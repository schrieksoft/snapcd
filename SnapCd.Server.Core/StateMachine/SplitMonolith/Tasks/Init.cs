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
    public Event<SplitInitCompleted> SplitInitCompleted { get; } = null!;
    public Event<SplitInitCancelled> SplitInitCancelled { get; } = null!;
    public Event<SplitInitFaulted> SplitInitFaulted { get; } = null!;

    public State InitPending { get; } = null!;
    public State InitWaitingForRunner { get; } = null!;

    private void Configure_Init()
    {
        Event(() => SplitInitCompleted, x => x.CorrelateById(y => y.Message.CorrelationId));
        Event(() => SplitInitCancelled, x => x.CorrelateById(y => y.Message.CorrelationId));
        Event(() => SplitInitFaulted, x => x.CorrelateById(y => y.Message.CorrelationId));

        CreateStep<SplitInitCompleted, SplitInitCancelled, SplitInitFaulted, SplitValidateRequested>(
            InitWaitingForRunner,
            InitPending,
            SplitInitCompleted,
            SplitInitCancelled,
            SplitInitFaulted,
            ValidateWaitingForRunner,
            ValidatePending
        );
    }
}
