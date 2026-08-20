// SPDX-License-Identifier: LicenseRef-Snap-CD-Source-Available-1.1
// Copyright (c) 2026 Karl Schriek / Schrieksoft.
// No license is granted to use this file, in whole or in part, (a) as training, fine-tuning, retrieval, or
// embedding data for any machine-learning model, or (b) as input to any machine-learning model, agent, or automated
// system for the purpose of producing a derivative work or reimplementation that is not otherwise permitted by the
// Snap CD Source-Available License (including any Competing Product as defined therein). Contact info@snapcd.io
// for terms covering either use.

namespace SnapCd.Contracts.Constants;

public static class ServerEndpoints
{
    public const string GetDefinitiveRevisionCompleted = "GetDefinitiveRevisionCompleted";
    public const string GetDefinitiveRevisionCancelled = "GetDefinitiveRevisionCancelled";
    public const string GetDefinitiveRevisionFaulted = "GetDefinitiveRevisionFaulted";

    public const string GetModuleCompleted = "GetModuleCompleted";
    public const string GetModuleCancelled = "GetModuleCancelled";
    public const string GetModuleFaulted = "GetModuleFaulted";
    
    public const string InitCompleted = "InitCompleted";
    public const string InitCancelled = "InitCancelled";
    public const string InitFaulted = "InitFaulted";
    
    public const string ValidateCompleted = "ValidateCompleted";
    public const string ValidateCancelled = "ValidateCancelled";
    public const string ValidateFaulted = "ValidateFaulted";
    public const string PlanEmptyVerifyCompleted = "PlanEmptyVerifyCompleted";
    public const string PlanEmptyVerifyCancelled = "PlanEmptyVerifyCancelled";
    public const string PlanEmptyVerifyFaulted = "PlanEmptyVerifyFaulted";
    public const string RefactorVerifyCompleted = "RefactorVerifyCompleted";
    public const string RefactorVerifyCancelled = "RefactorVerifyCancelled";
    public const string RefactorVerifyFaulted = "RefactorVerifyFaulted";
    public const string MigrateMapCompleted = "MigrateMapCompleted";
    public const string MigrateMapCancelled = "MigrateMapCancelled";
    public const string MigrateMapFaulted = "MigrateMapFaulted";
    public const string MigrateProveCompleted = "MigrateProveCompleted";
    public const string MigrateProveCancelled = "MigrateProveCancelled";
    public const string MigrateProveFaulted = "MigrateProveFaulted";
    public const string MigrateRunCompleted = "MigrateRunCompleted";
    public const string MigrateRunCancelled = "MigrateRunCancelled";
    public const string MigrateRunFaulted = "MigrateRunFaulted";
    public const string MigrateVerifyCompleted = "MigrateVerifyCompleted";
    public const string MigrateVerifyCancelled = "MigrateVerifyCancelled";
    public const string MigrateVerifyFaulted = "MigrateVerifyFaulted";
    public const string PolicyValidateCompleted = "PolicyValidateCompleted";
    public const string PolicyValidateCancelled = "PolicyValidateCancelled";
    public const string PolicyValidateFaulted = "PolicyValidateFaulted";
    
    public const string VariablesCompleted = "VariablesCompleted";
    public const string VariablesCancelled = "VariablesCancelled";
    public const string VariablesFaulted = "VariablesFaulted";

    public const string PlanCompleted = "PlanCompleted";
    public const string PlanCancelled = "PlanCancelled";
    public const string PlanFaulted = "PlanFaulted";

    public const string PlanDestroyCompleted = "PlanDestroyCompleted";
    public const string PlanDestroyCancelled = "PlanDestroyCancelled";
    public const string PlanDestroyFaulted = "PlanDestroyFaulted";

    public const string ApplyFromPlanCompleted = "ApplyFromPlanCompleted";
    public const string ApplyFromPlanCancelled = "ApplyFromPlanCancelled";
    public const string ApplyFromPlanFaulted = "ApplyFromPlanFaulted";

    public const string DestroyFromPlanCompleted = "DestroyFromPlanCompleted";
    public const string DestroyFromPlanCancelled = "DestroyFromPlanCancelled";
    public const string DestroyFromPlanFaulted = "DestroyFromPlanFaulted";

    public const string OutputCompleted = "OutputCompleted";
    public const string OutputCancelled = "OutputCancelled";
    public const string OutputFaulted = "OutputFaulted";

    public const string SourceRefreshCompleted = "SourceRefreshCompleted";
    public const string SourceRefreshCompletedV2 = "SourceRefreshCompletedV2";
    public const string SourceRefreshFaulted = "SourceRefreshFaulted";

    public const string AddLogs = "AddLogs";

    public const string ReportRunningTask = "ReportRunningTask";
    
    public const string CancelKillCompleted = "CancelKillCompleted";
    public const string CancelGracefulCompleted = "CancelGracefulCompleted";
    
}