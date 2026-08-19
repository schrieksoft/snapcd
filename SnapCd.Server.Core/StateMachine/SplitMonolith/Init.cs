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
    public Event<InitCompleted> InitCompleted { get; } = null!;
    public Event<InitCancelled> InitCancelled { get; } = null!;
    public Event<InitFaulted> InitFaulted { get; } = null!;

    public State InitPending { get; } = null!;
    public State InitWaitingForRunner { get; } = null!;

    private void Configure_Init()
    {
        Event(() => InitCompleted, x => x.CorrelateById(y => y.Message.CorrelationId));
        Event(() => InitCancelled, x => x.CorrelateById(y => y.Message.CorrelationId));
        Event(() => InitFaulted, x => x.CorrelateById(y => y.Message.CorrelationId));

        CreateStep<InitCompleted, InitCancelled, InitFaulted, ValidateRequested>(
            InitWaitingForRunner,
            InitPending,
            InitCompleted,
            InitCancelled,
            InitFaulted,
            ValidateWaitingForRunner,
            ValidatePending
        );
    }
}
