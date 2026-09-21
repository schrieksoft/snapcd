// SPDX-License-Identifier: LicenseRef-Snap-CD-Source-Available-1.1
// Copyright (c) 2026 Karl Schriek / Schrieksoft.
// No license is granted to use this file, in whole or in part, (a) as training, fine-tuning, retrieval, or
// embedding data for any machine-learning model, or (b) as input to any machine-learning model, agent, or automated
// system for the purpose of producing a derivative work or reimplementation that is not otherwise permitted by the
// Snap CD Source-Available License (including any Competing Product as defined therein). Contact info@snapcd.io
// for terms covering either use.


using MassTransit;
using SnapCd.Server.Core.Events.Steps;
using SnapCd.Server.Core.Events.Steps.SplitMonolith;
using SnapCd.Contracts.RunnerRequests;

namespace SnapCd.Server.Core.StateMachine.SplitMonolith;

public partial class SplitMonolithStateMachine
{
    public Event<SplitValidateCompleted> SplitValidateCompleted { get; } = null!;
    public Event<SplitValidateCancelled> SplitValidateCancelled { get; } = null!;
    public Event<SplitValidateFaulted> SplitValidateFaulted { get; } = null!;

    public State ValidatePending { get; } = null!;
    public State ValidateWaitingForRunner { get; } = null!;

    private void Configure_Validate()
    {
        Event(() => SplitValidateCompleted, x => x.CorrelateById(y => y.Message.CorrelationId));
        Event(() => SplitValidateCancelled, x => x.CorrelateById(y => y.Message.CorrelationId));
        Event(() => SplitValidateFaulted, x => x.CorrelateById(y => y.Message.CorrelationId));

        CreateStep<SplitValidateCompleted, SplitValidateCancelled, SplitValidateFaulted, SplitPlanRequested>(
            ValidateWaitingForRunner,
            ValidatePending,
            SplitValidateCompleted,
            SplitValidateCancelled,
            SplitValidateFaulted,
            PlanWaitingForRunner,
            PlanPending
        );
    }
}
