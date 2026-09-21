// SPDX-License-Identifier: LicenseRef-Snap-CD-Source-Available-1.1
// Copyright (c) 2026 Karl Schriek / Schrieksoft.
// No license is granted to use this file, in whole or in part, (a) as training, fine-tuning, retrieval, or
// embedding data for any machine-learning model, or (b) as input to any machine-learning model, agent, or automated
// system for the purpose of producing a derivative work or reimplementation that is not otherwise permitted by the
// Snap CD Source-Available License (including any Competing Product as defined therein). Contact info@snapcd.io
// for terms covering either use.

using MassTransit;
using SnapCd.Server.Core.Events.Steps;
using SnapCd.Server.Core.Events.Steps.SplitMigrate;

namespace SnapCd.Server.Core.StateMachine.SplitMigrate;

public partial class SplitMigrateStateMachine
{
    public Event<SplitPlanCompleted> SplitPlanCompleted { get; } = null!;
    public Event<SplitPlanCancelled> SplitPlanCancelled { get; } = null!;
    public Event<SplitPlanFaulted> SplitPlanFaulted { get; } = null!;

    public State PlanPending { get; } = null!;
    public State PlanWaitingForRunner { get; } = null!;

    /// <summary>
    /// An ordinary step. The runner fails it when the plan is not empty: a monolith with pending
    /// changes cannot be split, because the carve would be proved against a baseline that was
    /// never real.
    /// </summary>
    private void Configure_Plan()
    {
        Event(() => SplitPlanCompleted, x => x.CorrelateById(y => y.Message.CorrelationId));
        Event(() => SplitPlanCancelled, x => x.CorrelateById(y => y.Message.CorrelationId));
        Event(() => SplitPlanFaulted, x => x.CorrelateById(y => y.Message.CorrelationId));

        CreateStep<SplitPlanCompleted, SplitPlanCancelled, SplitPlanFaulted, PlanEmptyVerifyRequested>(
            PlanWaitingForRunner,
            PlanPending,
            SplitPlanCompleted,
            SplitPlanCancelled,
            SplitPlanFaulted,
            PlanEmptyVerifyWaitingForRunner,
            PlanEmptyVerifyPending
        );
    }
}
