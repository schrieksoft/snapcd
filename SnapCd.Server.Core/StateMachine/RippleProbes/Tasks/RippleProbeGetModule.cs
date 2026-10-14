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
    public Event<RippleProbeGetModuleCompleted> RippleProbeGetModuleCompleted { get; } = null!;
    public Event<RippleProbeGetModuleCancelled> RippleProbeGetModuleCancelled { get; } = null!;
    public Event<RippleProbeGetModuleFaulted> RippleProbeGetModuleFaulted { get; } = null!;

    public State RippleProbeGetModulePending { get; } = null!;
    public State RippleProbeGetModuleWaitingForRunner { get; } = null!;

    private void Configure_RippleProbeGetModule()
    {
        Event(() => RippleProbeGetModuleCompleted, x => x.CorrelateById(y => y.Message.CorrelationId));
        Event(() => RippleProbeGetModuleCancelled, x => x.CorrelateById(y => y.Message.CorrelationId));
        Event(() => RippleProbeGetModuleFaulted, x => x.CorrelateById(y => y.Message.CorrelationId));

        CreateStep<RippleProbeGetModuleCompleted, RippleProbeGetModuleCancelled, RippleProbeGetModuleFaulted, RippleProbeInitRequested>(
            RippleProbeGetModuleWaitingForRunner,
            RippleProbeGetModulePending,
            RippleProbeGetModuleCompleted,
            RippleProbeGetModuleCancelled,
            RippleProbeGetModuleFaulted,
            RippleProbeInitWaitingForRunner,
            RippleProbeInitPending
        );
    }
}
