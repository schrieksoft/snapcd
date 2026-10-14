// SPDX-License-Identifier: LicenseRef-Snap-CD-Source-Available-1.1
// Copyright (c) 2026 Karl Schriek / Schrieksoft.
// No license is granted to use this file, in whole or in part, (a) as training, fine-tuning, retrieval, or
// embedding data for any machine-learning model, or (b) as input to any machine-learning model, agent, or automated
// system for the purpose of producing a derivative work or reimplementation that is not otherwise permitted by the
// Snap CD Source-Available License (including any Competing Product as defined therein). Contact info@snapcd.io
// for terms covering either use.


using MassTransit;
using SnapCd.Server.Core.Entities.Sagas;
using SnapCd.Server.Core.Events.Jobs.Module;
using SnapCd.Server.Core.Events.Runners;
using SnapCd.Server.Core.Events.Steps;
using SnapCd.Server.Core.Events.Steps.RippleProbe;
using SnapCd.Server.Core.StateMachine.Jobs.Utils;

namespace SnapCd.Server.Core.StateMachine.RippleProbes;

public partial class RippleProbeStateMachine
{
    public Event<RippleProbeProbeCompleted> RippleProbeProbeCompleted { get; } = null!;
    public Event<RippleProbeProbeCancelled> RippleProbeProbeCancelled { get; } = null!;
    public Event<RippleProbeProbeFaulted> RippleProbeProbeFaulted { get; } = null!;

    public State RippleProbeProbePending { get; } = null!;
    public State RippleProbeProbeWaitingForRunner { get; } = null!;

    /// <summary>
    /// The last step of the chain, so its completion ends the job rather than dispatching the next
    /// request. It carries the signals the probe produced, which the finalization activity stores.
    /// </summary>
    private void Configure_RippleProbeProbe()
    {
        Event(() => RippleProbeProbeCompleted, x => x.CorrelateById(y => y.Message.CorrelationId));
        Event(() => RippleProbeProbeCancelled, x => x.CorrelateById(y => y.Message.CorrelationId));
        Event(() => RippleProbeProbeFaulted, x => x.CorrelateById(y => y.Message.CorrelationId));

        During(RippleProbeProbeWaitingForRunner,
            When(RippleProbeProbeWaitingForRunner.Enter)
                .Activity(x => x.OfType<Jobs.Activites.CheckRunnerConnectionActivity<RippleProbeSaga, RippleProbeProbeCompleted>>()),
            When(CancelRippleProbeJobRequested)
                .IfCancelKill<RippleProbeSaga, RippleProbeCancelled, CancelRippleProbeJobRequested, RippleProbeCancelKillRequested, DummyRippleProbeCancelKillCompleted>(_logger, CancelKillRequested, CancellingImmediateKill, Cancelled)
                .IfCancelAfterCurrent<RippleProbeSaga, CancelRippleProbeJobRequested>(_logger, CancellingAfterCurrent),
            Ignore(RunnerReconnectedEvent),
            Ignore(HeartbeatScheduled.Received),
            Ignore(HeartbeatRequested.Completed),
            Ignore(HeartbeatRequested.Completed2)
        );

        During(RippleProbeProbePending,
            When(RippleProbeProbeCompleted)
                .Then(context => { context.Saga.SignalsJson = context.Message.SignalsJson; })
                .ThenRippleProbeCompleted(Completed),
            When(HeartbeatScheduled.Received)
                .ThenHeartbeatScheduled(HeartbeatRequested),
            When(HeartbeatRequested.Completed)
                .ThenHeartbeatCompleted(HeartbeatScheduled),
            When(HeartbeatRequested.Completed2)
                .ThenRippleProbeTimedOut(Failed),
            When(CancelRippleProbeJobRequested)
                .IfCancelKill<RippleProbeSaga, RippleProbeCancelled, CancelRippleProbeJobRequested, RippleProbeCancelKillRequested, DummyRippleProbeCancelKillCompleted>(_logger, CancelKillRequested, CancellingImmediateKill, Cancelled)
                .IfCancelAfterCurrent<RippleProbeSaga, CancelRippleProbeJobRequested>(_logger, CancellingAfterCurrent),
            When(RippleProbeProbeCancelled)
                .ThenRippleProbeCancelled(Cancelled),
            When(RippleProbeProbeFaulted)
                .ThenRippleProbeFaulted(Failed, _logger),
            Ignore(RunnerReconnectedEvent)
        );
    }
}
