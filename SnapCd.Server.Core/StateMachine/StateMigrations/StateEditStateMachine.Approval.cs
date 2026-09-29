// SPDX-License-Identifier: LicenseRef-Snap-CD-Source-Available-1.1
// Copyright (c) 2026 Karl Schriek / Schrieksoft.
// No license is granted to use this file, in whole or in part, (a) as training, fine-tuning, retrieval, or
// embedding data for any machine-learning model, or (b) as input to any machine-learning model, agent, or automated
// system for the purpose of producing a derivative work or reimplementation that is not otherwise permitted by the
// Snap CD Source-Available License (including any Competing Product as defined therein). Contact info@snapcd.io
// for terms covering either use.

using MassTransit;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using SnapCd.Server.Core.Entities.Sagas;
using SnapCd.Server.Core.Enums;
using SnapCd.Server.Core.Events.Jobs.Module;
using SnapCd.Server.Core.Events.Steps;
using SnapCd.Server.Core.Events.Steps.StateMigrations;
using SnapCd.Server.Core.Events.System;
using SnapCd.Server.Core.StateMachine.StateMigrations.Activities;
using SnapCd.Server.Core.StateMachine.StateMigrations.Finalization;
using SnapCd.Server.Core.StateMachine.StateMigrations.Activities;

namespace SnapCd.Server.Core.StateMachine.StateMigrations;

public abstract partial class StateEditStateMachine<
    TSaga, TJobRequested, TApproved,
    TSelectRunnerInstanceRequested, TGetModuleRequested, TInitRequested,
    TSelectRunnerInstanceCompleted, TSelectRunnerInstanceCancelled, TSelectRunnerInstanceFaulted,
    TGetModuleCompleted, TGetModuleCancelled, TGetModuleFaulted,
    TInitCompleted, TInitCancelled, TInitFaulted,
    TPreCheckRequested, TPreCheckCompleted, TPreCheckCancelled, TPreCheckFaulted,
    TEditRequested, TEditCompleted, TEditCancelled, TEditFaulted>
{
    public Event<ApprovalReevaluationRequestedEvent> ApprovalModifiedEvent { get; } = null!;

    public Event<TApproved> ApprovedEvent { get; } = null!;

    public Schedule<TSaga, ApprovalTimeoutReceived> ApprovalTimeoutScheduled { get; } = null!;

    public State WaitingForApproval { get; } = null!;

    /// <summary>
    /// The gate on the one irreversible step. Everything before it reads: the checkout, the
    /// backend, the pre-check saying what the edit would do. The edit itself writes, and a state
    /// edit cannot be undone by running it again, so it answers to the same threshold a split and a
    /// transfer do. Listing afterwards is a read and needs no answer of its own.
    /// </summary>
    private void Configure_Approval()
    {
        Event(() => ApprovalModifiedEvent, x => x.CorrelateById(y => y.Message.ModuleJobId));
        Event(() => ApprovedEvent, x => x.CorrelateById(y => y.Message.ModuleJobId));

        // The edit is asked for by a second consume, once this one has committed the transition.
        // Dispatching from the approving chain lets the reply arrive in the state it is leaving.
        During(EditPending,
            When(ApprovedEvent)
                .Activity(x => x.OfType<
                    SendStateEditStepToRunnerActivity<TSaga, TApproved, TEditRequested>>())
                .ThenAsync(context => RecordDispatched(context, EditName)));

        Schedule(() => ApprovalTimeoutScheduled, saga => saga.ApprovalTimeoutScheduleTokenId,
            config => { config.Received = e => e.CorrelateById(context => context.Message.CorrelationId); });

        During(WaitingForApproval,
            DealWithApprovalStatus(When(ApprovalModifiedEvent)),

            When(ApprovalTimeoutScheduled.Received)
                .Then(context => _logger.LogInformation(
                    "{Verb} on Module {ModuleId} was not answered in time",
                    Verb, context.Saga.ModuleId))
                .Activity(x => x.OfType<CancelStateMigrationJobActivity<TSaga, ApprovalTimeoutReceived>>())
                .TransitionTo(Failed)
                .Finalize(),

            // Nothing is on a runner while it waits, so there is nothing to kill or wait out.
            When(CancelRequested)
                .Then(context => _logger.LogInformation(
                    "{Verb} on Module {ModuleId} was cancelled while awaiting approval",
                    Verb, context.Saga.ModuleId))
                .Unschedule(ApprovalTimeoutScheduled)
                .Activity(x => x.OfType<CancelStateMigrationJobActivity<TSaga, CancelStateMigrationJobRequested>>())
                .TransitionTo(Failed)
                .Finalize(),

            Ignore(HeartbeatScheduled.Received),
            Ignore(HeartbeatRequested.Completed),
            Ignore(HeartbeatRequested.Completed2)
        );
    }

    /// <summary>
    /// Approved edits; declined ends the job; neither yet leaves it waiting. The same binder runs
    /// on entry and on every later change, so an already-satisfied threshold never waits.
    /// </summary>
    private EventActivityBinder<TSaga, TMessage> DealWithApprovalStatus<TMessage>(
        EventActivityBinder<TSaga, TMessage> binder, bool transition = false)
        where TMessage : class
    {
        return binder
            .Activity(x => x.OfType<StateEditNeedsApprovalActivity<TSaga, TMessage>>())
            .IfElse(
                x => x.Saga.IsApproved,
                approved => approved
                    .Then(context =>
                    {
                        context.Saga.WaitingSince = null;
                        _logger.LogInformation(
                            "{Verb} on Module {ModuleId} was approved",
                            Verb, context.Saga.ModuleId);
                    })
                    .Unschedule(ApprovalTimeoutScheduled)
                    .Activity(x => x.OfType<NotWaitingForApprovalStateMigrationActivity<TSaga, TMessage>>())
                    // Scheduled before the approval is published, not after: anything between the
                    // publish and the end of the chain is time for the answer to arrive early.
                    .Schedule(HeartbeatScheduled,
                        context => new HeartbeatScheduled
                        {
                            CorrelationId = context.Saga.CorrelationId,
                            OrganizationId = context.Saga.OrganizationId
                        })
                    .Publish(context => new TApproved
                    {
                        ModuleJobId = context.Saga.CorrelationId,
                        OrganizationId = context.Saga.OrganizationId
                    })
                    .TransitionTo(EditPending),
                notApproved => notApproved
                    .IfElse(
                        x => x.Saga.IsDeclined,
                        declined => declined
                            .Then(context => _logger.LogInformation(
                                "{Verb} on Module {ModuleId} was declined; nothing was changed",
                                Verb, context.Saga.ModuleId))
                            .Unschedule(ApprovalTimeoutScheduled)
                            .Activity(x => x.OfType<CancelStateMigrationJobActivity<TSaga, TMessage>>())
                            .TransitionTo(Failed)
                            .Finalize(),
                        stillWaiting => stillWaiting
                            .If(
                                _ => transition,
                                waiting => waiting
                                    .Activity(x => x.OfType<WaitingForApprovalStateMigrationActivity<TSaga, TMessage>>())
                                    .Then(context =>
                                    {
                                        context.Saga.WaitingSince = DateTime.UtcNow;
                                        _logger.LogInformation(
                                            "{Verb} on Module {ModuleId} is waiting to be approved",
                                            Verb, context.Saga.ModuleId);
                                    })
                                    // An unset timeout is no timeout: scheduling zero would cancel
                                    // the job as it asks.
                                    .If(x => x.Saga.ApprovalTimeoutMinutes > 0, x => x
                                        .Schedule(ApprovalTimeoutScheduled,
                                            context => new ApprovalTimeoutReceived
                                            {
                                                CorrelationId = context.Saga.CorrelationId,
                                                OrganizationId = context.Saga.OrganizationId
                                            },
                                            x2 => TimeSpan.FromMinutes(x2.Saga.ApprovalTimeoutMinutes ?? 0)))
                                    .TransitionTo(WaitingForApproval))));
    }
}
