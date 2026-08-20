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
    public Event<RefactorDiffCompleted> RefactorDiffCompleted { get; } = null!;
    public Event<RefactorDiffCancelled> RefactorDiffCancelled { get; } = null!;
    public Event<RefactorDiffFaulted> RefactorDiffFaulted { get; } = null!;

    public State RefactorDiffPending { get; } = null!;
    public State RefactorDiffWaitingForRunner { get; } = null!;

    private void Configure_RefactorDiff()
    {
        Event(() => RefactorDiffCompleted, x => x.CorrelateById(y => y.Message.CorrelationId));
        Event(() => RefactorDiffCancelled, x => x.CorrelateById(y => y.Message.CorrelationId));
        Event(() => RefactorDiffFaulted, x => x.CorrelateById(y => y.Message.CorrelationId));

        CreateStep<RefactorDiffCompleted, RefactorDiffCancelled, RefactorDiffFaulted, MigrateMapRequested>(
            RefactorDiffWaitingForRunner,
            RefactorDiffPending,
            RefactorDiffCompleted,
            RefactorDiffCancelled,
            RefactorDiffFaulted,
            MigrateMapWaitingForRunner,
            MigrateMapPending
        );
    }
}
