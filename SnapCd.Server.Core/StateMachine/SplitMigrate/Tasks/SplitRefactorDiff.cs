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
using SnapCd.Contracts.RunnerRequests;

namespace SnapCd.Server.Core.StateMachine.SplitMigrate;

public partial class SplitMigrateStateMachine
{
    public Event<SplitRefactorDiffCompleted> SplitRefactorDiffCompleted { get; } = null!;
    public Event<SplitRefactorDiffCancelled> SplitRefactorDiffCancelled { get; } = null!;
    public Event<SplitRefactorDiffFaulted> SplitRefactorDiffFaulted { get; } = null!;

    public State SplitRefactorDiffPending { get; } = null!;
    public State SplitRefactorDiffWaitingForRunner { get; } = null!;

    private void Configure_SplitRefactorDiff()
    {
        Event(() => SplitRefactorDiffCompleted, x => x.CorrelateById(y => y.Message.CorrelationId));
        Event(() => SplitRefactorDiffCancelled, x => x.CorrelateById(y => y.Message.CorrelationId));
        Event(() => SplitRefactorDiffFaulted, x => x.CorrelateById(y => y.Message.CorrelationId));

        CreateStep<SplitRefactorDiffCompleted, SplitRefactorDiffCancelled, SplitRefactorDiffFaulted, SplitMigrateMapRequested>(
            SplitRefactorDiffWaitingForRunner,
            SplitRefactorDiffPending,
            SplitRefactorDiffCompleted,
            SplitRefactorDiffCancelled,
            SplitRefactorDiffFaulted,
            SplitMigrateMapWaitingForRunner,
            SplitMigrateMapPending
        );
    }
}
