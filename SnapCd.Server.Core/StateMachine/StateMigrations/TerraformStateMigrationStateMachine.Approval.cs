// SPDX-License-Identifier: LicenseRef-Snap-CD-Source-Available-1.1
// Copyright (c) 2026 Karl Schriek / Schrieksoft.
// No license is granted to use this file, in whole or in part, (a) as training, fine-tuning, retrieval, or
// embedding data for any machine-learning model, or (b) as input to any machine-learning model, agent, or automated
// system for the purpose of producing a derivative work or reimplementation that is not otherwise permitted by the
// Snap CD Source-Available License (including any Competing Product as defined therein). Contact info@snapcd.io
// for terms covering either use.

using SnapCd.Server.Core.StateMachine.Jobs.Activites;
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
using SnapCd.Server.Core.Events.Runners;

namespace SnapCd.Server.Core.StateMachine.StateMigrations;

public abstract partial class TerraformStateMigrationStateMachine<
    TSaga, TJobRequested, TApproved,
    TSelectRunnerInstanceRequested, TGetModuleRequested, TInitRequested,
    TSelectRunnerInstanceCompleted, TSelectRunnerInstanceCancelled, TSelectRunnerInstanceFaulted,
    TGetModuleCompleted, TGetModuleCancelled, TGetModuleFaulted,
    TInitCompleted, TInitCancelled, TInitFaulted,
    TPreCheckRequested, TPreCheckCompleted, TPreCheckCancelled, TPreCheckFaulted,
    TMigrateStateRequested, TMigrateStateCompleted, TMigrateStateCancelled, TMigrateStateFaulted,
    TCancelKillRequested, TDummyCancelKillCompleted, TCancelKillCompleted>
{
    public Event<ApprovalReevaluationRequestedEvent> ApprovalModifiedEvent { get; } = null!;

    public Event<TApproved> ApprovedEvent { get; } = null!;

    public Schedule<TSaga, ApprovalTimeoutReceived> ApprovalTimeoutScheduled { get; } = null!;

    public State WaitingForApproval { get; } = null!;

    /// <summary>
    /// The gate on the one irreversible step. Everything before it reads: the checkout, the
    /// backend, the pre-check saying what the state migration would do. The migration itself writes,
    /// and it cannot be undone by running it again, so it answers to the same threshold a split and
    /// a transfer do. Listing afterwards is a read and needs no answer of its own.
    /// </summary>
    private void Configure_Approval()
    {
        Event(() => ApprovalModifiedEvent, x => x.CorrelateById(y => y.Message.ModuleJobId));
        Event(() => ApprovedEvent, x => x.CorrelateById(y => y.Message.ModuleJobId));

        // The state migration is asked for by a second consume, once this one has committed the transition.
        // Dispatching from the approving chain lets the reply arrive in the state it is leaving.
        During(MigrateStatePending,
            When(ApprovedEvent)
                .Activity(x => x.OfType<
                    SendTerraformStateMigrationStepToRunnerActivity<TSaga, TApproved, TMigrateStateRequested>>())
                .IfElse(
                    context => context.Saga.PreviousStateBeforeWaiting != null,
                    noRunner => noRunner
                        .Activity(x => x.OfType<WaitingForRunnerActivity<TSaga, TApproved>>())
                        .Then(context =>
                        {
                            context.Saga.WaitingSince = DateTime.UtcNow;
                            _logger.LogInformation(
                                "{Verb} on Module {ModuleId} is approved and waiting for a runner",
                                Verb, context.Saga.ModuleId);
                        })
                        .TransitionTo(MigrateStateWaitingForRunner),
                    sent => sent
                        .ThenAsync(context => RecordDispatched(context, MigrateStateName))));

        // Approved, with nothing to send it to. The migration has not run, so the job waits for a
        // runner rather than failing: the approval it already has stays good.
        During(MigrateStateWaitingForRunner,
            When(MigrateStateWaitingForRunner.Enter)
                .Activity(x => x.OfType<CheckRunnerConnectionActivity<TSaga, TApproved>>()),

            When(RunnerReconnectedEvent)
                .Activity(x => x.OfType<
                    SendTerraformStateMigrationStepToRunnerActivity<TSaga, RunnerReconnectedEvent, TMigrateStateRequested>>())
                .IfElse(
                    context => context.Saga.PreviousStateBeforeWaiting != null,
                    stillGone => stillGone,
                    sent => sent
                        .Activity(x => x.OfType<NotWaitingForRunnerActivity<TSaga, RunnerReconnectedEvent>>())
                        .Then(context => context.Saga.WaitingSince = null)
                        .ThenAsync(context => RecordDispatched(context, MigrateStateName))
                        .TransitionTo(MigrateStatePending)),
            When(CancelRequested)
                .Then(context => _logger.LogInformation(
                    "{Verb} on Module {ModuleId} was cancelled while waiting for a runner to write with",
                    Verb, context.Saga.ModuleId))
                .Activity(x => x.OfType<CancelStateMigrationJobActivity<TSaga, CancelStateMigrationJobRequested>>())
                .TransitionTo(Failed).Finalize(),
            Ignore(HeartbeatScheduled.Received),
            Ignore(HeartbeatRequested.Completed),
            Ignore(HeartbeatRequested.Completed2));

        Schedule(() => ApprovalTimeoutScheduled, saga => saga.ApprovalTimeoutScheduleTokenId,
            config => { config.Received = e => e.CorrelateById(context => context.Message.CorrelationId); });

        During(WaitingForApproval,
            // The threshold may already be answered by the time this job parks, and nothing
            // re-raises the answer. Entry prompts itself so the one arm below does the reading.
            When(WaitingForApproval.Enter)
                .Publish(context => new ApprovalReevaluationRequestedEvent
                {
                    ModuleJobId = context.Saga.CorrelationId,
                    ModuleId = context.Saga.ModuleId
                }),

            DealWithApprovalStatus(When(ApprovalModifiedEvent)),

            // Dispatching the write publishes this to itself, so a delivery that overtakes the
            // transition out of this state is early rather than wrong.
            Ignore(ApprovedEvent),

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
    /// Approved state migrations; declined ends the job; neither yet leaves it waiting. The same binder runs
    /// on entry and on every later change, so an already-satisfied threshold never waits.
    /// </summary>
    private EventActivityBinder<TSaga, TMessage> DealWithApprovalStatus<TMessage>(
        EventActivityBinder<TSaga, TMessage> binder, bool transition = false)
        where TMessage : class
    {
        return binder
            .Activity(x => x.OfType<TerraformStateMigrationNeedsApprovalActivity<TSaga, TMessage>>())
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
                    .TransitionTo(MigrateStatePending),
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
