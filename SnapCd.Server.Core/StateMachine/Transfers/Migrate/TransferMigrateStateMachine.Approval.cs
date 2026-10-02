// SPDX-License-Identifier: LicenseRef-Snap-CD-Source-Available-1.1
// Copyright (c) 2026 Karl Schriek / Schrieksoft.
// No license is granted to use this file, in whole or in part, (a) as training, fine-tuning, retrieval, or
// embedding data for any machine-learning model, or (b) as input to any machine-learning model, agent, or automated
// system for the purpose of producing a derivative work or reimplementation that is not otherwise permitted by the
// Snap CD Source-Available License (including any Competing Product as defined therein). Contact info@snapcd.io
// for terms covering either use.

using MassTransit;
using Microsoft.Extensions.Logging;
using SnapCd.Server.Core.Entities.Sagas;
using SnapCd.Server.Core.Enums;
using SnapCd.Server.Core.Events.Jobs.Module;
using SnapCd.Server.Core.Events.Steps;
using SnapCd.Server.Core.Events.Steps.Transfer;
using SnapCd.Server.Core.Events.System;
using SnapCd.Server.Core.StateMachine.StateMigrations.Activities;
using SnapCd.Server.Core.StateMachine.StateMigrations.Finalization;
using SnapCd.Server.Core.StateMachine.Transfers.Migrate.Activities;

namespace SnapCd.Server.Core.StateMachine.Transfers.Migrate;

public partial class TransferMigrateStateMachine
{
    public Event<ApprovalReevaluationRequestedEvent> ApprovalModifiedEvent { get; } = null!;

    public Event<TransferApproved> ApprovedEvent { get; } = null!;
    public Event<CancelStateMigrationJobRequested> CancelRequested { get; } = null!;
    public Schedule<TransferMigrateSaga, ApprovalTimeoutReceived> ApprovalTimeoutScheduled { get; } = null!;

    public State WaitingForApproval { get; } = null!;

    /// <summary>
    /// The gate on this Module's single irreversible transition. Everything before it is read-only:
    /// the plan, the map, the prove. Everything after writes state.
    /// </summary>
    private void Configure_Approval()
    {
        Event(() => ApprovalModifiedEvent, x => x.CorrelateById(y => y.Message.ModuleJobId));
        Event(() => ApprovedEvent, x => x.CorrelateById(y => y.Message.ModuleJobId));
        Event(() => CancelRequested, x => x.CorrelateById(y => y.Message.CorrelationId));

        Schedule(() => ApprovalTimeoutScheduled, saga => saga.ApprovalTimeoutScheduleTokenId,
            config => { config.Received = e => e.CorrelateById(context => context.Message.CorrelationId); });

        During(TransferMigrateRunPending,
            SendOrWaitAfterGate<TransferApproved, TransferMigrateRunRequested>(
                When(ApprovedEvent), "TransferMigrateRun", TransferMigrateRunWaitingForRunner));

        WaitForRunner<TransferApproved, TransferMigrateRunRequested>(
            TransferMigrateRunWaitingForRunner, "TransferMigrateRun", TransferMigrateRunPending);

        // An approval cast after this job stopped waiting is news about a job already running.
        DuringAny(Ignore(ApprovalModifiedEvent));

        During(WaitingForApproval,
            // The threshold may already be answered by the time this half parks, and nothing
            // re-raises the answer. Entry prompts itself so the one arm below does the reading.
            When(WaitingForApproval.Enter)
                .Publish(context => new ApprovalReevaluationRequestedEvent
                {
                    ModuleJobId = context.Saga.CorrelationId,
                    ModuleId = context.Saga.ModuleId
                }),

            DealWithApprovalStatus(When(ApprovalModifiedEvent)),

            // Dispatching the run publishes this to itself, so a delivery that overtakes the
            // transition out of this state is early rather than wrong.
            Ignore(ApprovedEvent),

            When(ApprovalTimeoutScheduled.Received)
                .Then(context => _logger.LogInformation(
                    "Transfer: approval timed out for Module {ModuleId}",
                    context.Saga.ModuleId))
                .Activity(x => x.OfType<CancelStateMigrationJobActivity<TransferMigrateSaga, ApprovalTimeoutReceived>>())
                .TransitionTo(Failed)
                .Finalize(),

            // Nothing is running on a runner here, so there is nothing to kill or wait out.
            When(CancelRequested)
                .Then(context => _logger.LogInformation(
                    "Transfer: cancelled while awaiting approval for Module {ModuleId}",
                    context.Saga.ModuleId))
                .Unschedule(ApprovalTimeoutScheduled)
                .Activity(x => x.OfType<CancelStateMigrationJobActivity<TransferMigrateSaga, CancelStateMigrationJobRequested>>())
                .TransitionTo(Failed)
                .Finalize(),

            Ignore(RunnerReconnectedEvent),
            Ignore(HeartbeatScheduled.Received),
            Ignore(HeartbeatRequested.Completed),
            Ignore(HeartbeatRequested.Completed2)
        );

        // Cancelling a running transfer ends it in every state, including while its state is being
        // written: a cancel kill stops the job wherever it is, and a side left part-written is
        // finished by its own transfer.
        foreach (var running in new[]
                 {
                     TransferSelectRunnerInstancePending, TransferGetModulePending, TransferInitPending, TransferValidatePending,
                     TransferGetModuleWaitingForRunner,
                     TransferInitWaitingForRunner, TransferValidateWaitingForRunner,
                     TransferMigrateMapPending, TransferMigrateProvePending,
                     TransferMigrateRunPending, TransferMigrateVerifyPending,
                     TransferMigrateMapWaitingForRunner, TransferMigrateProveWaitingForRunner,
                     TransferMigrateRunWaitingForRunner, TransferMigrateVerifyWaitingForRunner
                 })
            During(running,
                When(CancelRequested)
                    .Then(context => _logger.LogInformation(
                        "Transfer: cancelled for Module {ModuleId}",
                        context.Saga.ModuleId))
                    .Activity(x => x.OfType<CancelStateMigrationJobActivity<TransferMigrateSaga, CancelStateMigrationJobRequested>>())
                    .TransitionTo(Failed)
                    .Finalize()
            );
    }

    /// <summary>
    /// Approved writes; declined ends the job; neither yet leaves it waiting. The same binder runs
    /// on entry and on every later change, so an already-satisfied threshold never waits.
    /// </summary>
    private EventActivityBinder<TransferMigrateSaga, TMessage> DealWithApprovalStatus<TMessage>(
        EventActivityBinder<TransferMigrateSaga, TMessage> binder, bool transition = false)
        where TMessage : class
    {
        return binder
            .Activity(y => y.OfType<TransferMigrateNeedsApprovalActivity<TMessage>>())
            .IfElse(
                y => y.Saga.IsApproved,
                approved => approved
                    .Then(context =>
                    {
                        context.Saga.WaitingSince = null;
                        _logger.LogInformation(
                            "Transfer: approved, writing Module {ModuleId}",
                            context.Saga.ModuleId);
                    })
                    .Unschedule(ApprovalTimeoutScheduled)
                    .Activity(z => z.OfType<NotWaitingForApprovalStateMigrationActivity<TransferMigrateSaga, TMessage>>())
                    // Dispatched by a second consume, once this one has committed the transition.
                    .Schedule(HeartbeatScheduled,
                        context => new HeartbeatScheduled
                        {
                            CorrelationId = context.Saga.CorrelationId,
                            OrganizationId = context.Saga.OrganizationId
                        })
                    .Publish(context => new TransferApproved
                    {
                        ModuleJobId = context.Saga.CorrelationId,
                        OrganizationId = context.Saga.OrganizationId
                    })
                    .TransitionTo(TransferMigrateRunPending),
                notApproved => notApproved
                    .IfElse(
                        y => y.Saga.IsDeclined,
                        declined => declined
                            .Then(context => _logger.LogInformation(
                                "Transfer: declined for Module {ModuleId}; nothing written",
                                context.Saga.ModuleId))
                            .Unschedule(ApprovalTimeoutScheduled)
                            .Activity(z => z.OfType<CancelStateMigrationJobActivity<TransferMigrateSaga, TMessage>>())
                            .TransitionTo(Failed)
                            .Finalize(),
                        stillWaiting => stillWaiting
                            .If(
                                _ => transition,
                                z1 => z1
                                    .Activity(z2 => z2.OfType<WaitingForApprovalStateMigrationActivity<TransferMigrateSaga, TMessage>>())
                                    .Then(context =>
                                    {
                                        context.Saga.WaitingSince = DateTime.UtcNow;
                                        _logger.LogInformation(
                                            "Transfer: awaiting approval for Module {ModuleId}",
                                            context.Saga.ModuleId);
                                    })
                                    // Only where one is configured: an unset timeout is no
                                    // timeout, and scheduling zero cancels the job as it asks.
                                    .If(z2 => z2.Saga.ApprovalTimeoutMinutes > 0, z2 => z2
                                        .Schedule(ApprovalTimeoutScheduled,
                                            context => new ApprovalTimeoutReceived
                                            {
                                                CorrelationId = context.Saga.CorrelationId,
                                                OrganizationId = context.Saga.OrganizationId
                                            },
                                            z3 => TimeSpan.FromMinutes(z3.Saga.ApprovalTimeoutMinutes ?? 0)))
                                    .TransitionTo(WaitingForApproval))));
    }
}
