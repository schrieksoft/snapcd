// SPDX-License-Identifier: LicenseRef-Snap-CD-Source-Available-1.1
// Copyright (c) 2026 Karl Schriek / Schrieksoft.
// No license is granted to use this file, in whole or in part, (a) as training, fine-tuning, retrieval, or
// embedding data for any machine-learning model, or (b) as input to any machine-learning model, agent, or automated
// system for the purpose of producing a derivative work or reimplementation that is not otherwise permitted by the
// Snap CD Source-Available License (including any Competing Product as defined therein). Contact info@snapcd.io
// for terms covering either use.


using MassTransit;
using Microsoft.Extensions.Logging;
using SnapCd.Contracts.RunnerRequests;
using SnapCd.Server.Core.Entities.Sagas;
using SnapCd.Server.Core.Events.Jobs.Module;
using SnapCd.Server.Core.Events.Runners;
using SnapCd.Server.Core.Events.Steps;
using SnapCd.Server.Core.Events.Steps.SplitMonolith;
using SnapCd.Server.Core.Events.System;
using SnapCd.Server.Core.StateMachine.Jobs.Activites;
using SnapCd.Server.Core.StateMachine.Jobs.Utils;

namespace SnapCd.Server.Core.StateMachine.SplitMonolith;

public partial class SplitMonolithStateMachine
{
    public Event<PlanCompleted> PlanCompleted { get; } = null!;
    public Event<PlanCancelled> PlanCancelled { get; } = null!;
    public Event<PlanFaulted> PlanFaulted { get; } = null!;

    public State PlanPending { get; } = null!;
    public State PlanWaitingForRunner { get; } = null!;

    /// <summary>
    /// The clean-plan precondition. A monolith with pending changes cannot be split: the carve
    /// would be proved against a baseline that was never real. This is the same assertion
    /// demonolith's own proof makes, one level up, and it fails before any state is pulled.
    /// A non-empty plan may mean drift or an unapplied definition; the plan cannot tell them
    /// apart and it does not matter, because the remedy is the same.
    /// </summary>
    private void Configure_Plan()
    {
        Event(() => PlanCompleted, x => x.CorrelateById(y => y.Message.CorrelationId));
        Event(() => PlanCancelled, x => x.CorrelateById(y => y.Message.CorrelationId));
        Event(() => PlanFaulted, x => x.CorrelateById(y => y.Message.CorrelationId));

        During(PlanWaitingForRunner,
            When(PlanWaitingForRunner.Enter)
                .Activity(x => x.OfType<CheckRunnerConnectionActivity<SplitMonolithSaga, PlanCompleted>>()),
            When(RunnerReconnectedEvent)
                .Activity(x => x.OfType<SendToRunnerActivity<SplitMonolithSaga, RunnerReconnectedEvent, PlanRequested>>())
                .IfElse(
                    context => context.Saga.PreviousStateBeforeWaiting != null,
                    whenTrue => whenTrue,
                    whenFalse => whenFalse
                        .Then(context => { context.Saga.WaitingSince = null; })
                        .Schedule(HeartbeatScheduled,
                            context => new HeartbeatScheduled { CorrelationId = context.Saga.CorrelationId, OrganizationId = context.Saga.OrganizationId })
                        .TransitionTo(PlanPending)
                ),
            When(CancelModuleRequested)
                .IfCancelKill<SplitMonolithSaga, SplitMonolithCancelled>(_logger, CancelKillRequested, CancellingImmediateKill, Cancelled)
                .IfCancelGraceful<SplitMonolithSaga, SplitMonolithCancelled>(_logger, CancelGracefulRequested, CancellingImmediateGraceful, Cancelled)
                .IfCancelAfterCurrent(_logger, CancellingAfterCurrent),
            Ignore(HeartbeatScheduled.Received),
            Ignore(HeartbeatRequested.Completed),
            Ignore(HeartbeatRequested.Completed2)
        );

        During(PlanPending,
            When(PlanCompleted)
                .IfElse(
                    context => context.Message.TotalChangedCount > 0,
                    whenTrue => whenTrue
                        .Then(context =>
                        {
                            context.Saga.NegativeVerdict =
                                $"The module's plan is not clean ({context.Message.TotalChangedCount} pending change(s)). Apply the module before splitting it.";
                            _logger.LogInformation(
                                "SplitMonolith: plan not clean for job {CorrelationId}, refusing the split",
                                context.Saga.CorrelationId);
                        })
                        .ThenSplitFaulted(Failed, _logger),
                    whenFalse => whenFalse
                        .Activity(x => x.OfType<SendToRunnerActivity<SplitMonolithSaga, PlanCompleted, RefactorVerifyRequested>>())
                        .IfElse(
                            context => context.Saga.PreviousStateBeforeWaiting != null,
                            stillWaiting => stillWaiting
                                .Then(context => { context.Saga.WaitingSince = DateTime.UtcNow; })
                                .TransitionTo(RefactorVerifyWaitingForRunner),
                            sent => sent
                                .Schedule(HeartbeatScheduled,
                                    context => new HeartbeatScheduled { CorrelationId = context.Saga.CorrelationId, OrganizationId = context.Saga.OrganizationId })
                                .TransitionTo(RefactorVerifyPending)
                        )
                ),
            When(HeartbeatScheduled.Received)
                .ThenHeartbeatScheduled(HeartbeatRequested),
            When(HeartbeatRequested.Completed)
                .ThenHeartbeatCompleted(HeartbeatScheduled),
            When(HeartbeatRequested.Completed2)
                .ThenSplitTimedOut(Failed),
            When(CancelModuleRequested)
                .IfCancelKill<SplitMonolithSaga, SplitMonolithCancelled>(_logger, CancelKillRequested, CancellingImmediateKill, Cancelled)
                .IfCancelGraceful<SplitMonolithSaga, SplitMonolithCancelled>(_logger, CancelGracefulRequested, CancellingImmediateGraceful, Cancelled)
                .IfCancelAfterCurrent(_logger, CancellingAfterCurrent),
            When(PlanCancelled)
                .ThenSplitCancelled(Cancelled),
            When(PlanFaulted)
                .ThenSplitFaulted(Failed, _logger),
            Ignore(RunnerReconnectedEvent)
        );
    }
}
