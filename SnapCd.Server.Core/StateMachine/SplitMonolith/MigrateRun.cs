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
    public Event<MigrateRunCompleted> MigrateRunCompleted { get; } = null!;
    public Event<MigrateRunCancelled> MigrateRunCancelled { get; } = null!;
    public Event<MigrateRunFaulted> MigrateRunFaulted { get; } = null!;

    public State MigrateRunPending { get; } = null!;
    public State MigrateRunWaitingForRunner { get; } = null!;

    private void Configure_MigrateRun()
    {
        Event(() => MigrateRunCompleted, x => x.CorrelateById(y => y.Message.CorrelationId));
        Event(() => MigrateRunCancelled, x => x.CorrelateById(y => y.Message.CorrelationId));
        Event(() => MigrateRunFaulted, x => x.CorrelateById(y => y.Message.CorrelationId));

        CreateStep<MigrateRunCompleted, MigrateRunCancelled, MigrateRunFaulted, MigrateVerifyRequested>(
            MigrateRunWaitingForRunner,
            MigrateRunPending,
            MigrateRunCompleted,
            MigrateRunCancelled,
            MigrateRunFaulted,
            MigrateVerifyWaitingForRunner,
            MigrateVerifyPending
        );
    }
}
