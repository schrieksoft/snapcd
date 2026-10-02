// SPDX-License-Identifier: LicenseRef-Snap-CD-Source-Available-1.1
// Copyright (c) 2026 Karl Schriek / Schrieksoft.
// No license is granted to use this file, in whole or in part, (a) as training, fine-tuning, retrieval, or
// embedding data for any machine-learning model, or (b) as input to any machine-learning model, agent, or automated
// system for the purpose of producing a derivative work or reimplementation that is not otherwise permitted by the
// Snap CD Source-Available License (including any Competing Product as defined therein). Contact info@snapcd.io
// for terms covering either use.

using MassTransit;
using SnapCd.Server.Core.Events.Steps.SplitMigrate;

namespace SnapCd.Server.Core.StateMachine.SplitMigrate;

public partial class SplitMigrateStateMachine
{
    public Event<SplitPlanEmptyVerifyCompleted> SplitPlanEmptyVerifyCompleted { get; } = null!;
    public Event<SplitPlanEmptyVerifyCancelled> SplitPlanEmptyVerifyCancelled { get; } = null!;
    public Event<SplitPlanEmptyVerifyFaulted> SplitPlanEmptyVerifyFaulted { get; } = null!;

    public State SplitPlanEmptyVerifyPending { get; } = null!;
    public State SplitPlanEmptyVerifyWaitingForRunner { get; } = null!;

    /// <summary>
    /// Asserts the plan just written is empty. The runner owns the verdict; a monolith with
    /// pending changes fails here, before any state is pulled or carved.
    /// </summary>
    private void Configure_SplitPlanEmptyVerify()
    {
        Event(() => SplitPlanEmptyVerifyCompleted, x => x.CorrelateById(y => y.Message.CorrelationId));
        Event(() => SplitPlanEmptyVerifyCancelled, x => x.CorrelateById(y => y.Message.CorrelationId));
        Event(() => SplitPlanEmptyVerifyFaulted, x => x.CorrelateById(y => y.Message.CorrelationId));

        CreateStep<SplitPlanEmptyVerifyCompleted, SplitPlanEmptyVerifyCancelled, SplitPlanEmptyVerifyFaulted, SplitRefactorValidateRequested>(
            SplitPlanEmptyVerifyWaitingForRunner,
            SplitPlanEmptyVerifyPending,
            SplitPlanEmptyVerifyCompleted,
            SplitPlanEmptyVerifyCancelled,
            SplitPlanEmptyVerifyFaulted,
            SplitRefactorValidateWaitingForRunner,
            SplitRefactorValidatePending
        );
    }
}
