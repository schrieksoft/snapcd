// SPDX-License-Identifier: LicenseRef-Snap-CD-Source-Available-1.1
// Copyright (c) 2026 Karl Schriek / Schrieksoft.
// No license is granted to use this file, in whole or in part, (a) as training, fine-tuning, retrieval, or
// embedding data for any machine-learning model, or (b) as input to any machine-learning model, agent, or automated
// system for the purpose of producing a derivative work or reimplementation that is not otherwise permitted by the
// Snap CD Source-Available License (including any Competing Product as defined therein). Contact info@snapcd.io
// for terms covering either use.

using MassTransit;
using SnapCd.Server.Core.Entities.Sagas.Base;
using SnapCd.Server.Core.Events.Jobs.Base;
using SnapCd.Server.Core.Events.Runners;
using SnapCd.Server.Core.Events.Steps;
using SnapCd.Server.Core.Events.Steps.Base;

using SnapCd.Server.Core.StateMachine.Jobs.Activites;
using SnapCd.Server.Core.StateMachine.Jobs.Activites.Finalization;
using SnapCd.Server.Core.StateMachine.Jobs.Utils;
namespace SnapCd.Server.Core.StateMachine.Jobs;

public partial class JobStateMachine<
    TSaga,
    TRequest,
    TResponseFailed,
    TResponseCompleted,
    TResponseCancelled,
    TPlanRequested,
    TPlanCompleted,
    TPlanCancelled,
    TApplyFromPlanRequested,
    TApplyFromPlanCompleted,
    TApplyFromPlanCancelled>
    where TSaga : JobSagaBase
    where TRequest : ModuleJobEventBase
    where TResponseFailed : ModuleJobEventCompletedBase, new()
    where TResponseCompleted : ModuleJobEventCompletedBase, new()
    where TResponseCancelled : ModuleJobEventCompletedBase, new()
    where TPlanRequested : StepRequestBase, new()
    where TPlanCompleted : PlanCompletedBase
    where TPlanCancelled : StepResponseBase
    where TApplyFromPlanRequested : StepRequestBase, new()
    where TApplyFromPlanCompleted : ApplyResponseBase
    where TApplyFromPlanCancelled : StepResponseBase
{
    private void CreateStep<TCompleted, TCancelled, TFaulted, TNextEvent>(
        State waitingState,
        State duringState,
        Event<TCompleted> completedEvent,
        Event<TCancelled> cancelledEvent,
        Event<TFaulted> faultedEvent,
        State nextWaitingState,
        State nextState
    )
        where TNextEvent : StepRequestBase, new()
        where TCompleted : StepResponseBase
        where TCancelled : StepResponseBase
        where TFaulted : StepResponseBase
    {
        // Handle waiting state when runner is disconnected
        During(waitingState,
            // Execute self-healing checks when entering this state
            When(waitingState.Enter)
                .Activity(x => x.OfType<CheckRunnerConnectionActivity<TSaga, TCompleted>>()),
            When(RunnerReconnectedEvent)
                .Then(context =>
                {
                    _logger.LogInformation(
                        "{StepName}: Runner reconnected for job {CorrelationId}, retrying send",
                        typeof(TCompleted).Name.Replace("Completed", ""), context.Saga.CorrelationId);
                })
                // Retry sending TNextEvent
                .Activity(x => x.OfType<SendToRunnerActivity<TSaga, RunnerReconnectedEvent, TNextEvent>>())
                .IfElse(
                    context => context.Saga.PreviousStateBeforeWaiting != null,
                    // Still disconnected (race condition) - stay in waiting
                    whenTrue => whenTrue
                        .Then(context =>
                        {
                            _logger.LogWarning(
                                "{StepName}: Runner disconnected again for job {CorrelationId}, staying in waiting state",
                                typeof(TCompleted).Name.Replace("Completed", ""), context.Saga.CorrelationId);
                        }),
                    // Successfully sent - transition to pending
                    whenFalse => whenFalse
                        .Then(context =>
                        {
                            context.Saga.WaitingSince = null;
                            _logger.LogInformation(
                                "{StepName}: Successfully sent for job {CorrelationId}, transitioning to pending",
                                typeof(TCompleted).Name.Replace("Completed", ""), context.Saga.CorrelationId);
                        })
                        .Schedule(HeartbeatScheduled,
                            context => new HeartbeatScheduled { CorrelationId = context.Saga.CorrelationId, OrganizationId = context.Saga.OrganizationId })
                        .TransitionTo(nextState)
                ),
            When(CancelModuleRequested)
                .IfCancelKill<TSaga, TResponseCancelled>(_logger, CancelKillRequested, CancellingImmediateKill, Cancelled)
                .IfCancelGraceful<TSaga, TResponseCancelled>(_logger, CancelGracefulRequested, CancellingImmediateGraceful, Cancelled)
                .IfCancelAfterCurrent(_logger, CancellingAfterCurrent),
            Ignore(HeartbeatScheduled.Received),
            Ignore(HeartbeatRequested.Completed),
            Ignore(HeartbeatRequested.Completed2)
        );

        During(duringState,
            When(completedEvent)
                // Use SendToRunnerActivity to target specific server instance
                .Activity(x => x.OfType<SendToRunnerActivity<TSaga, TCompleted, TNextEvent>>())
                .IfElse(
                    context => context.Saga.PreviousStateBeforeWaiting != null,
                    // Runner is disconnected - transition to waiting state
                    whenTrue => whenTrue
                        .Then(context =>
                        {
                            context.Saga.WaitingSince = DateTime.UtcNow;
                            _logger.LogWarning(
                                "{StepName}: Runner disconnected for job {CorrelationId}, entering waiting state",
                                typeof(TCompleted).Name.Replace("Completed", ""), context.Saga.CorrelationId);
                        })
                        .TransitionTo(nextWaitingState),
                    // Runner is connected - continue normally
                    whenFalse => whenFalse
                        .Schedule(HeartbeatScheduled,
                            context => new HeartbeatScheduled { CorrelationId = context.Saga.CorrelationId, OrganizationId = context.Saga.OrganizationId })
                        .TransitionTo(nextState)
                ),
            When(HeartbeatScheduled.Received)
                .ThenHeartbeatScheduled(HeartbeatRequested),
            When(HeartbeatRequested.Completed)
                .ThenHeartbeatCompleted(HeartbeatScheduled),
            When(HeartbeatRequested.Completed2)
                .ThenJobTimedOut<TSaga, TResponseFailed>(Failed),
            When(CancelModuleRequested)
                .IfCancelKill<TSaga, TResponseCancelled>(_logger, CancelKillRequested, CancellingImmediateKill, Cancelled)
                .IfCancelGraceful<TSaga, TResponseCancelled>(_logger, CancelGracefulRequested, CancellingImmediateGraceful, Cancelled)
                .IfCancelAfterCurrent(_logger, CancellingAfterCurrent),
            When(cancelledEvent)
                .ThenCancelled<TSaga, TResponseCancelled, TCancelled>(Cancelled),
            When(faultedEvent)
                .ThenFaulted<TSaga, TResponseFailed, TFaulted>(Failed, _logger),
            Ignore(RunnerReconnectedEvent)
        );
    }
}