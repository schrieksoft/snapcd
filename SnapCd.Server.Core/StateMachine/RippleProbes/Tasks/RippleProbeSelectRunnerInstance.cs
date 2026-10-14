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
using SnapCd.Server.Core.Events.Steps.RippleProbe;
using SnapCd.Server.Core.Events.System;
using SnapCd.Server.Core.StateMachine.Jobs.Utils;
using SnapCd.Server.Core.StateMachine.RippleProbes.Activites;

namespace SnapCd.Server.Core.StateMachine.RippleProbes;

public partial class RippleProbeStateMachine
{
    public Event<SelectRunnerInstanceCompleted> SelectRunnerInstanceCompleted { get; } = null!;
    public Event<SelectRunnerInstanceCancelled> SelectRunnerInstanceCancelled { get; } = null!;
    public Event<SelectRunnerInstanceFaulted> SelectRunnerInstanceFaulted { get; } = null!;

    public State RippleProbeSelectRunnerInstancePending { get; } = null!;

    /// <summary>
    /// Pins the runner instance for the whole job. Every later step dispatches to it, so the
    /// working directory the probe plans in persists across the pipeline.
    /// </summary>
    private void Configure_RippleProbeSelectRunnerInstance()
    {
        Event(() => SelectRunnerInstanceCompleted, x => x.CorrelateById(y => y.Message.CorrelationId));
        Event(() => SelectRunnerInstanceCancelled, x => x.CorrelateById(y => y.Message.CorrelationId));
        Event(() => SelectRunnerInstanceFaulted, x => x.CorrelateById(y => y.Message.CorrelationId));

        During(RippleProbeSelectRunnerInstancePending,
            When(CancelRippleProbeJobRequested)
                .IfCancelKill<RippleProbeSaga, RippleProbeCancelled, CancelRippleProbeJobRequested, RippleProbeCancelKillRequested, DummyRippleProbeCancelKillCompleted>(_logger, CancelKillRequested, CancellingImmediateKill, Cancelled)
                .IfCancelAfterCurrent<RippleProbeSaga, CancelRippleProbeJobRequested>(_logger, CancellingAfterCurrent),
            When(SelectRunnerInstanceCompleted)
                .Then(context => { context.Saga.RunnerInstanceName = context.Message.RunnerInstanceName; })
                .Activity(x => x.OfType<SendRippleProbeStepToRunnerActivity<SelectRunnerInstanceCompleted, RippleProbeGetModuleRequested>>())
                .IfElse(
                    context => context.Saga.PreviousStateBeforeWaiting != null,
                    whenTrue => whenTrue
                        .Then(context =>
                        {
                            context.Saga.WaitingSince = DateTime.UtcNow;
                            _logger.LogWarning(
                                "SelectRunnerInstance: Runner disconnected for job {CorrelationId}, entering waiting state",
                                context.Saga.CorrelationId);
                        })
                        .TransitionTo(RippleProbeGetModuleWaitingForRunner),
                    whenFalse => whenFalse
                        .Schedule(HeartbeatScheduled,
                            context => new HeartbeatScheduled { CorrelationId = context.Saga.CorrelationId, OrganizationId = context.Saga.OrganizationId })
                        .TransitionTo(RippleProbeGetModulePending)
                ),
            When(SelectRunnerInstanceCancelled)
                .ThenRippleProbeCancelled(Cancelled),
            When(SelectRunnerInstanceFaulted)
                .ThenRippleProbeFaulted(Failed, _logger),
            Ignore(RunnerReconnectedEvent),
            Ignore(HeartbeatScheduled.Received),
            Ignore(HeartbeatRequested.Completed),
            Ignore(HeartbeatRequested.Completed2)
        );
    }
}
