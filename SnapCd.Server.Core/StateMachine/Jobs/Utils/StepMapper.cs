// SPDX-License-Identifier: LicenseRef-Snap-CD-Source-Available-1.1
// Copyright (c) 2026 Karl Schriek / Schrieksoft.
// No license is granted to use this file, in whole or in part, (a) as training, fine-tuning, retrieval, or
// embedding data for any machine-learning model, or (b) as input to any machine-learning model, agent, or automated
// system for the purpose of producing a derivative work or reimplementation that is not otherwise permitted by the
// Snap CD Source-Available License (including any Competing Product as defined therein). Contact info@snapcd.io
// for terms covering either use.

using SnapCd.Server.Core.Entities.Definition;

namespace SnapCd.Server.Core.StateMachine.Jobs.Utils;

/// <summary>
/// Maps a job saga's current state to the ServerSideStep it is in, for paths that have no fault message to read the step from.
/// </summary>
public static class StepMapper
{
    /// <summary>
    /// Determines the ServerSideStep a job saga is in from its current state name, e.g. "ApplyFromPlanPending".
    /// </summary>
    public static ServerSideStep? DetermineStepFromState(string? state) => state == null ? null : DetermineStep(state);

    // Longer step names are matched before the shorter names they contain ("SplitPlan" before "Plan", "PolicyValidate" before "Validate").
    private static ServerSideStep? DetermineStep(string name)
    {
        return name switch
        {
            _ when name.Contains("SplitPlanEmptyVerify") => ServerSideStep.SplitPlanEmptyVerify,
            _ when name.Contains("SplitRefactorValidate") => ServerSideStep.SplitRefactorValidate,
            _ when name.Contains("SplitRefactorDiff") => ServerSideStep.SplitRefactorDiff,
            _ when name.Contains("SplitMigrateMap") => ServerSideStep.SplitMigrateMap,
            _ when name.Contains("SplitMigrateProve") => ServerSideStep.SplitMigrateProve,
            _ when name.Contains("SplitMigrateRun") => ServerSideStep.SplitMigrateRun,
            _ when name.Contains("SplitMigrateVerify") => ServerSideStep.SplitMigrateVerify,
            _ when name.Contains("SplitPlan") => ServerSideStep.SplitPlan,
            _ when name.Contains("AnalyseTransferRefactorMap") => ServerSideStep.AnalyseTransferRefactorMap,
            _ when name.Contains("TransferMigrateMap") => ServerSideStep.TransferMigrateMap,
            _ when name.Contains("TransferMigrateProve") => ServerSideStep.TransferMigrateProve,
            _ when name.Contains("TransferMigrateRun") => ServerSideStep.TransferMigrateRun,
            _ when name.Contains("TransferMigrateVerify") => ServerSideStep.TransferMigrateVerify,
            _ when name.Contains("MoveDryRun") => ServerSideStep.MoveDryRun,
            _ when name.Contains("RemoveDryRun") => ServerSideStep.RemoveDryRun,
            _ when name.Contains("ImportPreCheck") => ServerSideStep.ImportPreCheck,
            _ when name.Contains("LookupAddresses") => ServerSideStep.LookupAddresses,
            _ when name.Contains("DestroyProbe") => ServerSideStep.DestroyProbe,
            _ when name.Contains("ApplyProbe") => ServerSideStep.ApplyProbe,
            _ when name.Contains("PolicyValidate") => ServerSideStep.PolicyValidate,
            _ when name.Contains("SelectRunnerInstance") => ServerSideStep.SelectRunnerInstance,
            _ when name.Contains("GetDefinitiveRevision") => ServerSideStep.GetDefinitiveRevision,
            _ when name.Contains("GetModule") => ServerSideStep.GetModule,
            _ when name.Contains("Init") => ServerSideStep.Init,
            _ when name.Contains("Validate") => ServerSideStep.Validate,
            _ when name.Contains("Variables") => ServerSideStep.Variables,
            _ when name.Contains("ApplyFromPlan") => ServerSideStep.ApplyFromPlan,
            _ when name.Contains("DestroyFromPlan") => ServerSideStep.DestroyFromPlan,
            _ when name.Contains("PlanDestroy") => ServerSideStep.Plan,
            _ when name.Contains("Plan") => ServerSideStep.Plan,
            _ when name.Contains("Output") => ServerSideStep.Output,
            _ => null
        };
    }
}
