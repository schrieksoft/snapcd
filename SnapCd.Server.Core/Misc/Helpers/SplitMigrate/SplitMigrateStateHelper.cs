// SPDX-License-Identifier: LicenseRef-Snap-CD-Source-Available-1.1
// Copyright (c) 2026 Karl Schriek / Schrieksoft.
// No license is granted to use this file, in whole or in part, (a) as training, fine-tuning, retrieval, or
// embedding data for any machine-learning model, or (b) as input to any machine-learning model, agent, or automated
// system for the purpose of producing a derivative work or reimplementation that is not otherwise permitted by the
// Snap CD Source-Available License (including any Competing Product as defined therein). Contact info@snapcd.io
// for terms covering either use.

using SnapCd.Server.Core.Enums;

namespace SnapCd.Server.Core.Misc.Helpers.SplitMigrate;

/// <summary>
/// Maps each SplitMigrate runner callback to the saga state it is only valid in. Parallel to
/// StateHelper, which maps the deployment pipeline's callbacks.
/// </summary>
public static class SplitMigrateStateHelper
{
    public static HashSet<SplitMigrateSagaState> GetCancellingStates() =>
    [
        SplitMigrateSagaState.CancellingImmediateKill,
        SplitMigrateSagaState.CancellingImmediateGraceful,
        SplitMigrateSagaState.CancellingAfterCurrent
    ];

    private static readonly Dictionary<SplitMigrateTaskEndpoint, SplitMigrateSagaState> MethodToState = new()
    {
        [SplitMigrateTaskEndpoint.SelectRunnerInstanceCompleted] = SplitMigrateSagaState.SelectRunnerInstancePending,
        [SplitMigrateTaskEndpoint.CancelKillCompleted] = SplitMigrateSagaState.CancellingImmediateKill,
        [SplitMigrateTaskEndpoint.CancelGracefulCompleted] = SplitMigrateSagaState.CancellingImmediateGraceful,
        [SplitMigrateTaskEndpoint.SelectRunnerInstanceCancelled] = SplitMigrateSagaState.SelectRunnerInstancePending,
        [SplitMigrateTaskEndpoint.SelectRunnerInstanceFaulted] = SplitMigrateSagaState.SelectRunnerInstancePending,

        [SplitMigrateTaskEndpoint.GetModuleCompleted] = SplitMigrateSagaState.GetModulePending,
        [SplitMigrateTaskEndpoint.GetModuleCancelled] = SplitMigrateSagaState.GetModulePending,
        [SplitMigrateTaskEndpoint.GetModuleFaulted] = SplitMigrateSagaState.GetModulePending,

        [SplitMigrateTaskEndpoint.InitCompleted] = SplitMigrateSagaState.InitPending,
        [SplitMigrateTaskEndpoint.InitCancelled] = SplitMigrateSagaState.InitPending,
        [SplitMigrateTaskEndpoint.InitFaulted] = SplitMigrateSagaState.InitPending,

        [SplitMigrateTaskEndpoint.ValidateCompleted] = SplitMigrateSagaState.ValidatePending,
        [SplitMigrateTaskEndpoint.ValidateCancelled] = SplitMigrateSagaState.ValidatePending,
        [SplitMigrateTaskEndpoint.ValidateFaulted] = SplitMigrateSagaState.ValidatePending,

        [SplitMigrateTaskEndpoint.PlanCompleted] = SplitMigrateSagaState.PlanPending,
        [SplitMigrateTaskEndpoint.PlanCancelled] = SplitMigrateSagaState.PlanPending,
        [SplitMigrateTaskEndpoint.PlanFaulted] = SplitMigrateSagaState.PlanPending,

        [SplitMigrateTaskEndpoint.PlanEmptyVerifyCompleted] = SplitMigrateSagaState.PlanEmptyVerifyPending,
        [SplitMigrateTaskEndpoint.PlanEmptyVerifyCancelled] = SplitMigrateSagaState.PlanEmptyVerifyPending,
        [SplitMigrateTaskEndpoint.PlanEmptyVerifyFaulted] = SplitMigrateSagaState.PlanEmptyVerifyPending,

        [SplitMigrateTaskEndpoint.RefactorValidateCompleted] = SplitMigrateSagaState.RefactorValidatePending,
        [SplitMigrateTaskEndpoint.RefactorValidateCancelled] = SplitMigrateSagaState.RefactorValidatePending,
        [SplitMigrateTaskEndpoint.RefactorValidateFaulted] = SplitMigrateSagaState.RefactorValidatePending,

        [SplitMigrateTaskEndpoint.RefactorDiffCompleted] = SplitMigrateSagaState.RefactorDiffPending,
        [SplitMigrateTaskEndpoint.RefactorDiffCancelled] = SplitMigrateSagaState.RefactorDiffPending,
        [SplitMigrateTaskEndpoint.RefactorDiffFaulted] = SplitMigrateSagaState.RefactorDiffPending,

        [SplitMigrateTaskEndpoint.MigrateMapCompleted] = SplitMigrateSagaState.MigrateMapPending,
        [SplitMigrateTaskEndpoint.MigrateMapCancelled] = SplitMigrateSagaState.MigrateMapPending,
        [SplitMigrateTaskEndpoint.MigrateMapFaulted] = SplitMigrateSagaState.MigrateMapPending,

        [SplitMigrateTaskEndpoint.MigrateProveCompleted] = SplitMigrateSagaState.MigrateProvePending,
        [SplitMigrateTaskEndpoint.MigrateProveCancelled] = SplitMigrateSagaState.MigrateProvePending,
        [SplitMigrateTaskEndpoint.MigrateProveFaulted] = SplitMigrateSagaState.MigrateProvePending,

        [SplitMigrateTaskEndpoint.MigrateRunCompleted] = SplitMigrateSagaState.MigrateRunPending,
        [SplitMigrateTaskEndpoint.MigrateRunCancelled] = SplitMigrateSagaState.MigrateRunPending,
        [SplitMigrateTaskEndpoint.MigrateRunFaulted] = SplitMigrateSagaState.MigrateRunPending,

        [SplitMigrateTaskEndpoint.MigrateVerifyCompleted] = SplitMigrateSagaState.MigrateVerifyPending,
        [SplitMigrateTaskEndpoint.MigrateVerifyCancelled] = SplitMigrateSagaState.MigrateVerifyPending,
        [SplitMigrateTaskEndpoint.MigrateVerifyFaulted] = SplitMigrateSagaState.MigrateVerifyPending
    };

    public static SplitMigrateSagaState Lookup(SplitMigrateTaskEndpoint endpoint)
    {
        if (!MethodToState.TryGetValue(endpoint, out var state))
            throw new ArgumentOutOfRangeException(
                nameof(endpoint), endpoint, "No saga state is mapped for this SplitMigrate callback.");

        return state;
    }
}
