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
    public Event<RippleProbeValidateCompleted> RippleProbeValidateCompleted { get; } = null!;
    public Event<RippleProbeValidateCancelled> RippleProbeValidateCancelled { get; } = null!;
    public Event<RippleProbeValidateFaulted> RippleProbeValidateFaulted { get; } = null!;

    public State RippleProbeValidatePending { get; } = null!;
    public State RippleProbeValidateWaitingForRunner { get; } = null!;

    /// <summary>
    /// The last step of the chain, so its completion ends the job rather than dispatching the
    /// next request. The Plan step takes that place once it exists.
    /// </summary>
    private void Configure_RippleProbeValidate()
    {
        Event(() => RippleProbeValidateCompleted, x => x.CorrelateById(y => y.Message.CorrelationId));
        Event(() => RippleProbeValidateCancelled, x => x.CorrelateById(y => y.Message.CorrelationId));
        Event(() => RippleProbeValidateFaulted, x => x.CorrelateById(y => y.Message.CorrelationId));

        During(RippleProbeValidateWaitingForRunner,
            When(RippleProbeValidateWaitingForRunner.Enter)
                .Activity(x => x.OfType<Jobs.Activites.CheckRunnerConnectionActivity<RippleProbeSaga, RippleProbeValidateCompleted>>()),
            When(CancelRippleProbeJobRequested)
                .IfCancelKill<RippleProbeSaga, RippleProbeCancelled, CancelRippleProbeJobRequested, RippleProbeCancelKillRequested, DummyRippleProbeCancelKillCompleted>(_logger, CancelKillRequested, CancellingImmediateKill, Cancelled)
                .IfCancelAfterCurrent<RippleProbeSaga, CancelRippleProbeJobRequested>(_logger, CancellingAfterCurrent),
            Ignore(RunnerReconnectedEvent),
            Ignore(HeartbeatScheduled.Received),
            Ignore(HeartbeatRequested.Completed),
            Ignore(HeartbeatRequested.Completed2)
        );

        During(RippleProbeValidatePending,
            When(RippleProbeValidateCompleted)
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
            When(RippleProbeValidateCancelled)
                .ThenRippleProbeCancelled(Cancelled),
            When(RippleProbeValidateFaulted)
                .ThenRippleProbeFaulted(Failed, _logger),
            Ignore(RunnerReconnectedEvent)
        );
    }
}
