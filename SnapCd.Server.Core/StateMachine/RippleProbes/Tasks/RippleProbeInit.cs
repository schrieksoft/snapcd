// SPDX-License-Identifier: LicenseRef-Snap-CD-Source-Available-1.1
// Copyright (c) 2026 Karl Schriek / Schrieksoft.
// No license is granted to use this file, in whole or in part, (a) as training, fine-tuning, retrieval, or
// embedding data for any machine-learning model, or (b) as input to any machine-learning model, agent, or automated
// system for the purpose of producing a derivative work or reimplementation that is not otherwise permitted by the
// Snap CD Source-Available License (including any Competing Product as defined therein). Contact info@snapcd.io
// for terms covering either use.


using MassTransit;
using SnapCd.Server.Core.Events.Steps.RippleProbe;

namespace SnapCd.Server.Core.StateMachine.RippleProbes;

public partial class RippleProbeStateMachine
{
    public Event<RippleProbeInitCompleted> RippleProbeInitCompleted { get; } = null!;
    public Event<RippleProbeInitCancelled> RippleProbeInitCancelled { get; } = null!;
    public Event<RippleProbeInitFaulted> RippleProbeInitFaulted { get; } = null!;

    public State RippleProbeInitPending { get; } = null!;
    public State RippleProbeInitWaitingForRunner { get; } = null!;

    private void Configure_RippleProbeInit()
    {
        Event(() => RippleProbeInitCompleted, x => x.CorrelateById(y => y.Message.CorrelationId));
        Event(() => RippleProbeInitCancelled, x => x.CorrelateById(y => y.Message.CorrelationId));
        Event(() => RippleProbeInitFaulted, x => x.CorrelateById(y => y.Message.CorrelationId));

        CreateStep<RippleProbeInitCompleted, RippleProbeInitCancelled, RippleProbeInitFaulted, RippleProbeValidateRequested>(
            RippleProbeInitWaitingForRunner,
            RippleProbeInitPending,
            RippleProbeInitCompleted,
            RippleProbeInitCancelled,
            RippleProbeInitFaulted,
            RippleProbeValidateWaitingForRunner,
            RippleProbeValidatePending
        );
    }
}
