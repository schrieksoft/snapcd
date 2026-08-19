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
    public Event<MigrateMapCompleted> MigrateMapCompleted { get; } = null!;
    public Event<MigrateMapCancelled> MigrateMapCancelled { get; } = null!;
    public Event<MigrateMapFaulted> MigrateMapFaulted { get; } = null!;

    public State MigrateMapPending { get; } = null!;
    public State MigrateMapWaitingForRunner { get; } = null!;

    private void Configure_MigrateMap()
    {
        Event(() => MigrateMapCompleted, x => x.CorrelateById(y => y.Message.CorrelationId));
        Event(() => MigrateMapCancelled, x => x.CorrelateById(y => y.Message.CorrelationId));
        Event(() => MigrateMapFaulted, x => x.CorrelateById(y => y.Message.CorrelationId));

        CreateStep<MigrateMapCompleted, MigrateMapCancelled, MigrateMapFaulted, MigrateProveRequested>(
            MigrateMapWaitingForRunner,
            MigrateMapPending,
            MigrateMapCompleted,
            MigrateMapCancelled,
            MigrateMapFaulted,
            MigrateProveWaitingForRunner,
            MigrateProvePending
        );
    }
}
