// SPDX-License-Identifier: LicenseRef-Snap-CD-Source-Available-1.1
// Copyright (c) 2026 Karl Schriek / Schrieksoft.
// No license is granted to use this file, in whole or in part, (a) as training, fine-tuning, retrieval, or
// embedding data for any machine-learning model, or (b) as input to any machine-learning model, agent, or automated
// system for the purpose of producing a derivative work or reimplementation that is not otherwise permitted by the
// Snap CD Source-Available License (including any Competing Product as defined therein). Contact info@snapcd.io
// for terms covering either use.

using MassTransit;
using Microsoft.Extensions.DependencyInjection;
using SnapCd.Server.Core.Entities.Sagas;
using SnapCd.Server.Core.Enums;
using SnapCd.Server.Core.Events.Jobs.Module;
using SnapCd.Server.Core.Events.Steps.StateMigrations;
using SnapCd.Server.Core.Events.System;
using SnapCd.Server.Core.StateMachine.ManualJobs.Activities;
using SnapCd.Server.Core.StateMachine.ManualJobs.Finalization;
using SnapCd.Server.Core.StateMachine.StateMigrations.Activities;

namespace SnapCd.Server.Core.StateMachine.StateMigrations;

public partial class StateMoveStateMachine
{
    public Event<ApprovalReevaluationRequestedEvent> ApprovalModifiedEvent { get; } = null!;

    public Event<ManualJobResumeEvent> ResumeEvent { get; } = null!;

    public Schedule<StateMoveSaga, ApprovalTimeoutReceived> ApprovalTimeoutScheduled { get; } = null!;

    public State WaitingForApproval { get; } = null!;

    /// <summary>
    /// The gate on the one irreversible step. Everything before it reads: the checkout, the
    /// backend. The edit itself writes, and a state edit cannot be undone by running it again, so
    /// it answers to the same threshold a split and a transfer do. Listing afterwards is a read and
    /// needs no answer of its own.
    /// </summary>
    private void Configure_Approval()
    {
        Event(() => ApprovalModifiedEvent, x => x.CorrelateById(y => y.Message.ModuleJobId));
        Event(() => ResumeEvent, x => x.CorrelateById(y => y.Message.ModuleJobId));

        // The edit is asked for by a second consume, once this one has committed the transition.
        // Dispatching from the approving chain lets the reply arrive in the state it is leaving.
        During(MovePending,
            When(ResumeEvent)
                .Activity(x => x.OfType<
                    SendStateMoveStepToRunnerActivity<ManualJobResumeEvent, StateMoveRequested>>())
                .ThenAsync(context => RecordDispatched(context, TaskOf<StateMoveRequested>())));

        Schedule(() => ApprovalTimeoutScheduled, saga => saga.ApprovalTimeoutScheduleTokenId,
            config => { config.Received = e => e.CorrelateById(context => context.Message.CorrelationId); });

        During(WaitingForApproval,
            DealWithApprovalStatus(When(ApprovalModifiedEvent)),

            When(ApprovalTimeoutScheduled.Received)
                .Then(context => _logger.LogInformation(
                    "{Operation} on Module {ModuleId} was not answered in time",
                    context.Saga.Operation, context.Saga.ModuleId))
                .Activity(x => x.OfType<CancelManualModuleJobActivity<StateMoveSaga, ApprovalTimeoutReceived>>())
                .TransitionTo(Failed)
                .Finalize(),

            // Nothing is on a runner while it waits, so there is nothing to kill or wait out.
            When(CancelRequested)
                .Then(context => _logger.LogInformation(
                    "{Operation} on Module {ModuleId} was cancelled while awaiting approval",
                    context.Saga.Operation, context.Saga.ModuleId))
                .Unschedule(ApprovalTimeoutScheduled)
                .Activity(x => x.OfType<CancelManualModuleJobActivity<StateMoveSaga, CancelManualModuleJobRequested>>())
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
    private EventActivityBinder<StateMoveSaga, TMessage> DealWithApprovalStatus<TMessage>(
        EventActivityBinder<StateMoveSaga, TMessage> binder, bool transition = false)
        where TMessage : class
    {
        return binder
            .Activity(x => x.OfType<StateMoveNeedsApprovalActivity<TMessage>>())
            .IfElse(
                x => x.Saga.IsApproved,
                approved => approved
                    .Then(context =>
                    {
                        context.Saga.WaitingSince = null;
                        _logger.LogInformation(
                            "{Operation} on Module {ModuleId} was approved",
                            context.Saga.Operation, context.Saga.ModuleId);
                    })
                    .Unschedule(ApprovalTimeoutScheduled)
                    .Activity(x => x.OfType<NotWaitingForApprovalManualJobActivity<StateMoveSaga, TMessage>>())
                    .Publish(context => new ManualJobResumeEvent
                    {
                        ModuleJobId = context.Saga.CorrelationId,
                        OrganizationId = context.Saga.OrganizationId
                    })
                    .TransitionTo(MovePending),
                notApproved => notApproved
                    .IfElse(
                        x => x.Saga.IsDeclined,
                        declined => declined
                            .Then(context => _logger.LogInformation(
                                "{Operation} on Module {ModuleId} was declined; nothing was changed",
                                context.Saga.Operation, context.Saga.ModuleId))
                            .Unschedule(ApprovalTimeoutScheduled)
                            .Activity(x => x.OfType<CancelManualModuleJobActivity<StateMoveSaga, TMessage>>())
                            .TransitionTo(Failed)
                            .Finalize(),
                        stillWaiting => stillWaiting
                            .If(
                                _ => transition,
                                waiting => waiting
                                    .Activity(x => x.OfType<WaitingForApprovalManualJobActivity<StateMoveSaga, TMessage>>())
                                    .Then(context =>
                                    {
                                        context.Saga.WaitingSince = DateTime.UtcNow;
                                        _logger.LogInformation(
                                            "{Operation} on Module {ModuleId} is waiting to be approved",
                                            context.Saga.Operation, context.Saga.ModuleId);
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
