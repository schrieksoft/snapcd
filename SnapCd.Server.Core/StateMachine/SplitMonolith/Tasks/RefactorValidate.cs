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
    public Event<RefactorValidateCompleted> RefactorValidateCompleted { get; } = null!;
    public Event<RefactorValidateCancelled> RefactorValidateCancelled { get; } = null!;
    public Event<RefactorValidateFaulted> RefactorValidateFaulted { get; } = null!;

    public State RefactorValidatePending { get; } = null!;
    public State RefactorValidateWaitingForRunner { get; } = null!;

    private void Configure_RefactorValidate()
    {
        Event(() => RefactorValidateCompleted, x => x.CorrelateById(y => y.Message.CorrelationId));
        Event(() => RefactorValidateCancelled, x => x.CorrelateById(y => y.Message.CorrelationId));
        Event(() => RefactorValidateFaulted, x => x.CorrelateById(y => y.Message.CorrelationId));

        CreateStep<RefactorValidateCompleted, RefactorValidateCancelled, RefactorValidateFaulted, RefactorDiffRequested>(
            RefactorValidateWaitingForRunner,
            RefactorValidatePending,
            RefactorValidateCompleted,
            RefactorValidateCancelled,
            RefactorValidateFaulted,
            RefactorDiffWaitingForRunner,
            RefactorDiffPending
        );
    }
}
