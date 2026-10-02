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
    public Event<SplitMigrateMapCompleted> SplitMigrateMapCompleted { get; } = null!;
    public Event<SplitMigrateMapCancelled> SplitMigrateMapCancelled { get; } = null!;
    public Event<SplitMigrateMapFaulted> SplitMigrateMapFaulted { get; } = null!;

    public State SplitMigrateMapPending { get; } = null!;
    public State SplitMigrateMapWaitingForRunner { get; } = null!;

    private void Configure_SplitMigrateMap()
    {
        Event(() => SplitMigrateMapCompleted, x => x.CorrelateById(y => y.Message.CorrelationId));
        Event(() => SplitMigrateMapCancelled, x => x.CorrelateById(y => y.Message.CorrelationId));
        Event(() => SplitMigrateMapFaulted, x => x.CorrelateById(y => y.Message.CorrelationId));

        CreateStep<SplitMigrateMapCompleted, SplitMigrateMapCancelled, SplitMigrateMapFaulted, SplitMigrateProveRequested>(
            SplitMigrateMapWaitingForRunner,
            SplitMigrateMapPending,
            SplitMigrateMapCompleted,
            SplitMigrateMapCancelled,
            SplitMigrateMapFaulted,
            SplitMigrateProveWaitingForRunner,
            SplitMigrateProvePending,
            context =>
            {
                context.Saga.RefactorMapHash = context.Message.RefactorMapHash;
                context.Saga.CarvedModuleNames = context.Message.CarvedModuleNames.Count > 0
                    ? string.Join(", ", context.Message.CarvedModuleNames)
                    : null;
            }
        );
    }
}
