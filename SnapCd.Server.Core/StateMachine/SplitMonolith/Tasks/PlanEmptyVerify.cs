// SPDX-License-Identifier: LicenseRef-Snap-CD-Source-Available-1.1
// Copyright (c) 2026 Karl Schriek / Schrieksoft.
// No license is granted to use this file, in whole or in part, (a) as training, fine-tuning, retrieval, or
// embedding data for any machine-learning model, or (b) as input to any machine-learning model, agent, or automated
// system for the purpose of producing a derivative work or reimplementation that is not otherwise permitted by the
// Snap CD Source-Available License (including any Competing Product as defined therein). Contact info@snapcd.io
// for terms covering either use.

using MassTransit;
using SnapCd.Server.Core.Events.Steps.SplitMonolith;

namespace SnapCd.Server.Core.StateMachine.SplitMonolith;

public partial class SplitMonolithStateMachine
{
    public Event<PlanEmptyVerifyCompleted> PlanEmptyVerifyCompleted { get; } = null!;
    public Event<PlanEmptyVerifyCancelled> PlanEmptyVerifyCancelled { get; } = null!;
    public Event<PlanEmptyVerifyFaulted> PlanEmptyVerifyFaulted { get; } = null!;

    public State PlanEmptyVerifyPending { get; } = null!;
    public State PlanEmptyVerifyWaitingForRunner { get; } = null!;

    /// <summary>
    /// Asserts the plan just written is empty. The runner owns the verdict; a monolith with
    /// pending changes fails here, before any state is pulled or carved.
    /// </summary>
    private void Configure_PlanEmptyVerify()
    {
        Event(() => PlanEmptyVerifyCompleted, x => x.CorrelateById(y => y.Message.CorrelationId));
        Event(() => PlanEmptyVerifyCancelled, x => x.CorrelateById(y => y.Message.CorrelationId));
        Event(() => PlanEmptyVerifyFaulted, x => x.CorrelateById(y => y.Message.CorrelationId));

        CreateStep<PlanEmptyVerifyCompleted, PlanEmptyVerifyCancelled, PlanEmptyVerifyFaulted, RefactorValidateRequested>(
            PlanEmptyVerifyWaitingForRunner,
            PlanEmptyVerifyPending,
            PlanEmptyVerifyCompleted,
            PlanEmptyVerifyCancelled,
            PlanEmptyVerifyFaulted,
            RefactorValidateWaitingForRunner,
            RefactorValidatePending
        );
    }
}
