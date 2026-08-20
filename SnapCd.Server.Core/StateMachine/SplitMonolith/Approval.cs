// SPDX-License-Identifier: LicenseRef-Snap-CD-Source-Available-1.1
// Copyright (c) 2026 Karl Schriek / Schrieksoft.
// No license is granted to use this file, in whole or in part, (a) as training, fine-tuning, retrieval, or
// embedding data for any machine-learning model, or (b) as input to any machine-learning model, agent, or automated
// system for the purpose of producing a derivative work or reimplementation that is not otherwise permitted by the
// Snap CD Source-Available License (including any Competing Product as defined therein). Contact info@snapcd.io
// for terms covering either use.


using MassTransit;
using Microsoft.Extensions.Logging;
using SnapCd.Contracts;
using SnapCd.Contracts.RunnerRequests;
using SnapCd.Server.Core.Entities.Sagas;
using SnapCd.Server.Core.Events.Jobs.Module;
using SnapCd.Server.Core.Events.Runners;
using SnapCd.Server.Core.Events.Steps;
using SnapCd.Server.Core.Events.Steps.SplitMonolith;
using SnapCd.Server.Core.Events.System;
using SnapCd.Server.Core.StateMachine.Jobs.Activites;
using SnapCd.Server.Core.StateMachine.Jobs.Utils;
using SnapCd.Server.Core.StateMachine.ManualJobs.Finalization;

namespace SnapCd.Server.Core.StateMachine.SplitMonolith;

public partial class SplitMonolithStateMachine
{
    public Event<ApprovalReevaluationRequestedEvent> ApprovalModifiedEvent { get; } = null!;
    public Schedule<SplitMonolithSaga, ApprovalTimeoutReceived> ApprovalTimeoutScheduled { get; } = null!;

    public State WaitingForApproval { get; } = null!;
    public State Declined { get; } = null!;

    /// <summary>
    /// The only gate, sitting on the job's single irreversible transition: everything before it is
    /// reversible, everything after pushes state.
    /// </summary>
    private void Configure_Approval()
    {
        Event(() => ApprovalModifiedEvent, x => x.CorrelateById(y => y.Message.ModuleJobId));

        Schedule(() => ApprovalTimeoutScheduled, saga => saga.ApprovalTimeoutScheduleTokenId,
            config => { config.Received = e => e.CorrelateById(context => context.Message.CorrelationId); });

        During(WaitingForApproval,
            DealWithApprovalStatus(When(ApprovalModifiedEvent), false),
            When(ApprovalTimeoutScheduled.Received)
                .Then(context =>
                {
                    _logger.LogInformation("SplitMonolith: approval timed out for job {JobId}", context.Saga.CorrelationId);
                })
                .Publish(context => new SplitMonolithCancelled
                {
                    ModuleId = context.Saga.ModuleId,
                    OrganizationId = context.Saga.OrganizationId,
                    ModuleJobId = context.Saga.CorrelationId,
                    CancellationReason = CancellationReason.ApprovalTimeout
                })
                .Activity(x => x.OfType<CancelManualModuleJobActivity<SplitMonolithSaga, ApprovalTimeoutReceived>>())
                .TransitionTo(Cancelled)
                .Finalize(),
            When(CancelModuleRequested)
                .IfCancelKill<SplitMonolithSaga, SplitMonolithCancelled>(_logger, CancelKillRequested, CancellingImmediateKill, Cancelled)
                .IfCancelGraceful<SplitMonolithSaga, SplitMonolithCancelled>(_logger, CancelGracefulRequested, CancellingImmediateGraceful, Cancelled)
                .IfCancelAfterCurrent(_logger, CancellingAfterCurrent),
            Ignore(RunnerReconnectedEvent),
            Ignore(HeartbeatScheduled.Received),
            Ignore(HeartbeatRequested.Completed),
            Ignore(HeartbeatRequested.Completed2)
        );

        During(Declined, Ignore(RunnerReconnectedEvent));
    }

    /// <summary>
    /// Approved sends MigrateRun; declined ends the job; neither yet parks it in WaitingForApproval.
    /// </summary>
    private EventActivityBinder<SplitMonolithSaga, TMessage> DealWithApprovalStatus<TMessage>(
        EventActivityBinder<SplitMonolithSaga, TMessage> binder, bool transition)
        where TMessage : class
    {
        return binder
            .Activity(y => y.OfType<SplitMonolithNeedsApprovalActivity<TMessage>>())
            .IfElse(
                y => y.Saga.IsApproved,
                approved => approved
                    .Then(context =>
                    {
                        context.Saga.WaitingSince = null;
                        _logger.LogInformation("SplitMonolith: approved, pushing state for job {JobId}", context.Saga.CorrelationId);
                    })
                    .Activity(z => z.OfType<SendToRunnerActivity<SplitMonolithSaga, TMessage, MigrateRunRequested>>())
                    .IfElse(
                        context => context.Saga.PreviousStateBeforeWaiting != null,
                        disconnected => disconnected
                            .Then(context => { context.Saga.WaitingSince = DateTime.UtcNow; })
                            .TransitionTo(MigrateRunWaitingForRunner),
                        connected => connected
                            .Schedule(HeartbeatScheduled,
                                context => new HeartbeatScheduled { CorrelationId = context.Saga.CorrelationId, OrganizationId = context.Saga.OrganizationId })
                            .TransitionTo(MigrateRunPending)
                    ),
                notApproved => notApproved
                    .IfElse(
                        y => y.Saga.IsDeclined,
                        declined => declined
                            .Then(context =>
                            {
                                _logger.LogInformation("SplitMonolith: declined for job {JobId}", context.Saga.CorrelationId);
                            })
                            .Publish(context => new SplitMonolithCancelled
                            {
                                ModuleId = context.Saga.ModuleId,
                                OrganizationId = context.Saga.OrganizationId,
                                ModuleJobId = context.Saga.CorrelationId,
                                CancellationReason = CancellationReason.ApprovalDeclined
                            })
                            .Activity(z => z.OfType<CancelManualModuleJobActivity<SplitMonolithSaga, TMessage>>())
                            .TransitionTo(Declined)
                            .Finalize(),
                        stillWaiting => stillWaiting
                            .If(
                                _ => transition,
                                z1 => z1
                                    .Then(context =>
                                    {
                                        context.Saga.WaitingSince = DateTime.UtcNow;
                                        _logger.LogInformation("SplitMonolith: awaiting approval for job {JobId}", context.Saga.CorrelationId);
                                    })
                                    .If(z2 => z2.Saga.ApprovalTimeoutMinutes > 0, z2 => z2
                                        .Schedule(ApprovalTimeoutScheduled,
                                            context => new ApprovalTimeoutReceived { CorrelationId = context.Saga.CorrelationId, OrganizationId = context.Saga.OrganizationId },
                                            z3 => TimeSpan.FromMinutes(z3.Saga.ApprovalTimeoutMinutes ?? 0)))
                                    .TransitionTo(WaitingForApproval)
                            )
                    )
            );
    }
}
