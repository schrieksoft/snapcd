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
    public Event<SplitGetModuleCompleted> SplitGetModuleCompleted { get; } = null!;
    public Event<SplitGetModuleCancelled> SplitGetModuleCancelled { get; } = null!;
    public Event<SplitGetModuleFaulted> SplitGetModuleFaulted { get; } = null!;

    public State GetModulePending { get; } = null!;
    public State GetModuleWaitingForRunner { get; } = null!;

    private void Configure_GetModule()
    {
        Event(() => SplitGetModuleCompleted, x => x.CorrelateById(y => y.Message.CorrelationId));
        Event(() => SplitGetModuleCancelled, x => x.CorrelateById(y => y.Message.CorrelationId));
        Event(() => SplitGetModuleFaulted, x => x.CorrelateById(y => y.Message.CorrelationId));

        CreateStep<SplitGetModuleCompleted, SplitGetModuleCancelled, SplitGetModuleFaulted, SplitInitRequested>(
            GetModuleWaitingForRunner,
            GetModulePending,
            SplitGetModuleCompleted,
            SplitGetModuleCancelled,
            SplitGetModuleFaulted,
            InitWaitingForRunner,
            InitPending
        );
    }
}
