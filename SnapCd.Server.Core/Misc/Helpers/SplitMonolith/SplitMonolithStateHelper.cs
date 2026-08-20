// SPDX-License-Identifier: LicenseRef-Snap-CD-Source-Available-1.1
// Copyright (c) 2026 Karl Schriek / Schrieksoft.
// No license is granted to use this file, in whole or in part, (a) as training, fine-tuning, retrieval, or
// embedding data for any machine-learning model, or (b) as input to any machine-learning model, agent, or automated
// system for the purpose of producing a derivative work or reimplementation that is not otherwise permitted by the
// Snap CD Source-Available License (including any Competing Product as defined therein). Contact info@snapcd.io
// for terms covering either use.

using SnapCd.Server.Core.Enums;

namespace SnapCd.Server.Core.Misc.Helpers.SplitMonolith;

/// <summary>
/// Maps each SplitMonolith runner callback to the saga state it is only valid in. Parallel to
/// StateHelper, which maps the deployment pipeline's callbacks.
/// </summary>
public static class SplitMonolithStateHelper
{
    public static HashSet<SplitMonolithSagaState> GetCancellingStates() =>
    [
        SplitMonolithSagaState.CancellingImmediateKill,
        SplitMonolithSagaState.CancellingImmediateGraceful,
        SplitMonolithSagaState.CancellingAfterCurrent
    ];

    private static readonly Dictionary<SplitMonolithTaskEndpoint, SplitMonolithSagaState> MethodToState = new()
    {
        [SplitMonolithTaskEndpoint.SelectRunnerInstanceCompleted] = SplitMonolithSagaState.SelectRunnerInstancePending,
        [SplitMonolithTaskEndpoint.SelectRunnerInstanceCancelled] = SplitMonolithSagaState.SelectRunnerInstancePending,
        [SplitMonolithTaskEndpoint.SelectRunnerInstanceFaulted] = SplitMonolithSagaState.SelectRunnerInstancePending,

        [SplitMonolithTaskEndpoint.GetModuleCompleted] = SplitMonolithSagaState.GetModulePending,
        [SplitMonolithTaskEndpoint.GetModuleCancelled] = SplitMonolithSagaState.GetModulePending,
        [SplitMonolithTaskEndpoint.GetModuleFaulted] = SplitMonolithSagaState.GetModulePending,

        [SplitMonolithTaskEndpoint.InitCompleted] = SplitMonolithSagaState.InitPending,
        [SplitMonolithTaskEndpoint.InitCancelled] = SplitMonolithSagaState.InitPending,
        [SplitMonolithTaskEndpoint.InitFaulted] = SplitMonolithSagaState.InitPending,

        [SplitMonolithTaskEndpoint.ValidateCompleted] = SplitMonolithSagaState.ValidatePending,
        [SplitMonolithTaskEndpoint.ValidateCancelled] = SplitMonolithSagaState.ValidatePending,
        [SplitMonolithTaskEndpoint.ValidateFaulted] = SplitMonolithSagaState.ValidatePending,

        [SplitMonolithTaskEndpoint.PlanCompleted] = SplitMonolithSagaState.PlanPending,
        [SplitMonolithTaskEndpoint.PlanCancelled] = SplitMonolithSagaState.PlanPending,
        [SplitMonolithTaskEndpoint.PlanFaulted] = SplitMonolithSagaState.PlanPending,

        [SplitMonolithTaskEndpoint.PlanEmptyVerifyCompleted] = SplitMonolithSagaState.PlanEmptyVerifyPending,
        [SplitMonolithTaskEndpoint.PlanEmptyVerifyCancelled] = SplitMonolithSagaState.PlanEmptyVerifyPending,
        [SplitMonolithTaskEndpoint.PlanEmptyVerifyFaulted] = SplitMonolithSagaState.PlanEmptyVerifyPending,

        [SplitMonolithTaskEndpoint.RefactorVerifyCompleted] = SplitMonolithSagaState.RefactorVerifyPending,
        [SplitMonolithTaskEndpoint.RefactorVerifyCancelled] = SplitMonolithSagaState.RefactorVerifyPending,
        [SplitMonolithTaskEndpoint.RefactorVerifyFaulted] = SplitMonolithSagaState.RefactorVerifyPending,

        [SplitMonolithTaskEndpoint.MigrateMapCompleted] = SplitMonolithSagaState.MigrateMapPending,
        [SplitMonolithTaskEndpoint.MigrateMapCancelled] = SplitMonolithSagaState.MigrateMapPending,
        [SplitMonolithTaskEndpoint.MigrateMapFaulted] = SplitMonolithSagaState.MigrateMapPending,

        [SplitMonolithTaskEndpoint.MigrateProveCompleted] = SplitMonolithSagaState.MigrateProvePending,
        [SplitMonolithTaskEndpoint.MigrateProveCancelled] = SplitMonolithSagaState.MigrateProvePending,
        [SplitMonolithTaskEndpoint.MigrateProveFaulted] = SplitMonolithSagaState.MigrateProvePending,

        [SplitMonolithTaskEndpoint.MigrateRunCompleted] = SplitMonolithSagaState.MigrateRunPending,
        [SplitMonolithTaskEndpoint.MigrateRunCancelled] = SplitMonolithSagaState.MigrateRunPending,
        [SplitMonolithTaskEndpoint.MigrateRunFaulted] = SplitMonolithSagaState.MigrateRunPending,

        [SplitMonolithTaskEndpoint.MigrateVerifyCompleted] = SplitMonolithSagaState.MigrateVerifyPending,
        [SplitMonolithTaskEndpoint.MigrateVerifyCancelled] = SplitMonolithSagaState.MigrateVerifyPending,
        [SplitMonolithTaskEndpoint.MigrateVerifyFaulted] = SplitMonolithSagaState.MigrateVerifyPending
    };

    public static SplitMonolithSagaState Lookup(SplitMonolithTaskEndpoint endpoint)
    {
        if (!MethodToState.TryGetValue(endpoint, out var state))
            throw new ArgumentOutOfRangeException(
                nameof(endpoint), endpoint, "No saga state is mapped for this SplitMonolith callback.");

        return state;
    }
}
