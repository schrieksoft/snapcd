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
using SnapCd.Server.Core.Events.Jobs.Module;
using SnapCd.Server.Core.Events.Runners;
using SnapCd.Server.Core.Events.Steps;
using SnapCd.Server.Core.Events.Steps.Base;
using SnapCd.Server.Core.Events.System;
using SnapCd.Server.Core.StateMachine.Jobs.Activites;
using SnapCd.Server.Core.StateMachine.SplitMonolith.Activites;
using SnapCd.Server.Core.StateMachine.Jobs.Utils;

namespace SnapCd.Server.Core.StateMachine.SplitMonolith;

public partial class SplitMonolithStateMachine
{
    /// <summary>
    /// Wires one step's waiting and pending states: liveness on entry, retry on reconnect,
    /// heartbeat scheduling with a timeout, cancellation in every mode, then the next step.
    /// Mirrors JobStateMachine.CreateStep; duplicated so the two pipelines cannot drift into
    /// each other.
    /// </summary>
    private void CreateStep<TCompleted, TCancelled, TFaulted, TNextEvent>(
        State waitingState,
        State duringState,
        Event<TCompleted> completedEvent,
        Event<TCancelled> cancelledEvent,
        Event<TFaulted> faultedEvent,
        State nextWaitingState,
        State nextState,
        Action<BehaviorContext<SplitMonolithSaga, TCompleted>>? onCompleted = null
    )
        where TNextEvent : StepRequestBase, new()
        where TCompleted : StepResponseBase
        where TCancelled : StepResponseBase
        where TFaulted : StepResponseBase
    {
        During(waitingState,
            When(waitingState.Enter)
                .Activity(x => x.OfType<CheckRunnerConnectionActivity<SplitMonolithSaga, TCompleted>>()),
            When(RunnerReconnectedEvent)
                .Then(context =>
                {
                    _logger.LogInformation(
                        "{StepName}: Runner reconnected for job {CorrelationId}, retrying send",
                        typeof(TCompleted).Name.Replace("Completed", ""), context.Saga.CorrelationId);
                })
                .Activity(x => x.OfType<SendSplitStepToRunnerActivity<RunnerReconnectedEvent, TNextEvent>>())
                .IfElse(
                    context => context.Saga.PreviousStateBeforeWaiting != null,
                    whenTrue => whenTrue
                        .Then(context =>
                        {
                            _logger.LogWarning(
                                "{StepName}: Runner disconnected again for job {CorrelationId}, staying in waiting state",
                                typeof(TCompleted).Name.Replace("Completed", ""), context.Saga.CorrelationId);
                        }),
                    whenFalse => whenFalse
                        .Then(context =>
                        {
                            context.Saga.WaitingSince = null;
                            _logger.LogDebug(
                                "{StepName}: Successfully sent for job {CorrelationId}, transitioning to pending",
                                typeof(TCompleted).Name.Replace("Completed", ""), context.Saga.CorrelationId);
                        })
                        .Schedule(HeartbeatScheduled,
                            context => new HeartbeatScheduled { CorrelationId = context.Saga.CorrelationId, OrganizationId = context.Saga.OrganizationId })
                        .TransitionTo(nextState)
                ),
            When(CancelModuleRequested)
                .IfCancelKill<SplitMonolithSaga, SplitMonolithCancelled>(_logger, CancelKillRequested, CancellingImmediateKill, Cancelled)
                .IfCancelGraceful<SplitMonolithSaga, SplitMonolithCancelled>(_logger, CancelGracefulRequested, CancellingImmediateGraceful, Cancelled)
                .IfCancelAfterCurrent(_logger, CancellingAfterCurrent),
            Ignore(HeartbeatScheduled.Received),
            Ignore(HeartbeatRequested.Completed),
            Ignore(HeartbeatRequested.Completed2)
        );

        During(duringState,
            When(completedEvent)
                // Lets a step record what it reported before the chain moves on.
                .Then(context => onCompleted?.Invoke(context))
                .Activity(x => x.OfType<SendSplitStepToRunnerActivity<TCompleted, TNextEvent>>())
                .IfElse(
                    context => context.Saga.PreviousStateBeforeWaiting != null,
                    whenTrue => whenTrue
                        .Then(context =>
                        {
                            context.Saga.WaitingSince = DateTime.UtcNow;
                            _logger.LogWarning(
                                "{StepName}: Runner disconnected for job {CorrelationId}, entering waiting state",
                                typeof(TCompleted).Name.Replace("Completed", ""), context.Saga.CorrelationId);
                        })
                        .TransitionTo(nextWaitingState),
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
                .ThenSplitTimedOut(Failed),
            When(CancelModuleRequested)
                .IfCancelKill<SplitMonolithSaga, SplitMonolithCancelled>(_logger, CancelKillRequested, CancellingImmediateKill, Cancelled)
                .IfCancelGraceful<SplitMonolithSaga, SplitMonolithCancelled>(_logger, CancelGracefulRequested, CancellingImmediateGraceful, Cancelled)
                .IfCancelAfterCurrent(_logger, CancellingAfterCurrent),
            When(cancelledEvent)
                .ThenSplitCancelled(Cancelled),
            When(faultedEvent)
                .ThenSplitFaulted(Failed, _logger),
            Ignore(RunnerReconnectedEvent)
        );
    }
}
