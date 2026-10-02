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
    public Event<SplitMigrateRunCompleted> SplitMigrateRunCompleted { get; } = null!;
    public Event<SplitMigrateRunCancelled> SplitMigrateRunCancelled { get; } = null!;
    public Event<SplitMigrateRunFaulted> SplitMigrateRunFaulted { get; } = null!;

    public State SplitMigrateRunPending { get; } = null!;
    public State SplitMigrateRunWaitingForRunner { get; } = null!;

    private void Configure_SplitMigrateRun()
    {
        Event(() => SplitMigrateRunCompleted, x => x.CorrelateById(y => y.Message.CorrelationId));
        Event(() => SplitMigrateRunCancelled, x => x.CorrelateById(y => y.Message.CorrelationId));
        Event(() => SplitMigrateRunFaulted, x => x.CorrelateById(y => y.Message.CorrelationId));

        CreateStep<SplitMigrateRunCompleted, SplitMigrateRunCancelled, SplitMigrateRunFaulted, SplitMigrateVerifyRequested>(
            SplitMigrateRunWaitingForRunner,
            SplitMigrateRunPending,
            SplitMigrateRunCompleted,
            SplitMigrateRunCancelled,
            SplitMigrateRunFaulted,
            SplitMigrateVerifyWaitingForRunner,
            SplitMigrateVerifyPending
        );
    }
}
