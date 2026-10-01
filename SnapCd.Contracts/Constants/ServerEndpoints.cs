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
    public const string SplitPlanEmptyVerifyCompleted = "SplitPlanEmptyVerifyCompleted";
    public const string SplitPlanEmptyVerifyCancelled = "SplitPlanEmptyVerifyCancelled";
    public const string SplitPlanEmptyVerifyFaulted = "SplitPlanEmptyVerifyFaulted";
    public const string SplitRefactorValidateCompleted = "SplitRefactorValidateCompleted";
    public const string SplitRefactorValidateCancelled = "SplitRefactorValidateCancelled";
    public const string SplitRefactorValidateFaulted = "SplitRefactorValidateFaulted";
    public const string SplitRefactorDiffCompleted = "SplitRefactorDiffCompleted";
    public const string SplitRefactorDiffCancelled = "SplitRefactorDiffCancelled";
    public const string SplitRefactorDiffFaulted = "SplitRefactorDiffFaulted";
    // A transfer runs the same steps against one of two Modules, so its replies are separate
    // endpoints carrying the Module they are for.
    public const string AnalyseTransferRefactorMapCompleted = "AnalyseTransferRefactorMapCompleted";
    public const string AnalyseTransferRefactorMapCancelled = "AnalyseTransferRefactorMapCancelled";
    public const string AnalyseTransferRefactorMapFaulted = "AnalyseTransferRefactorMapFaulted";
    public const string TransferMigrateMapCompleted = "TransferMigrateMapCompleted";
    public const string TransferMigrateMapCancelled = "TransferMigrateMapCancelled";
    public const string TransferMigrateMapFaulted = "TransferMigrateMapFaulted";
    public const string TransferMigrateProveCompleted = "TransferMigrateProveCompleted";
    public const string TransferMigrateProveCancelled = "TransferMigrateProveCancelled";
    public const string TransferMigrateProveFaulted = "TransferMigrateProveFaulted";
    public const string TransferMigrateRunCompleted = "TransferMigrateRunCompleted";
    public const string TransferMigrateRunCancelled = "TransferMigrateRunCancelled";
    public const string TransferMigrateRunFaulted = "TransferMigrateRunFaulted";
    public const string TransferMigrateVerifyCompleted = "TransferMigrateVerifyCompleted";
    public const string TransferMigrateVerifyCancelled = "TransferMigrateVerifyCancelled";
    public const string TransferMigrateVerifyFaulted = "TransferMigrateVerifyFaulted";
    public const string LookupAddressesCompleted = "LookupAddressesCompleted";
    public const string LookupAddressesCancelled = "LookupAddressesCancelled";
    public const string LookupAddressesFaulted = "LookupAddressesFaulted";
    public const string MoveCompleted = "MoveCompleted";
    public const string MoveCancelled = "MoveCancelled";
    public const string MoveFaulted = "MoveFaulted";

    public const string ImportCompleted = "ImportCompleted";
    public const string ImportCancelled = "ImportCancelled";
    public const string ImportFaulted = "ImportFaulted";

    public const string RemoveCompleted = "RemoveCompleted";
    public const string RemoveCancelled = "RemoveCancelled";
    public const string RemoveFaulted = "RemoveFaulted";

    public const string MoveDryRunCompleted = "MoveDryRunCompleted";
    public const string MoveDryRunCancelled = "MoveDryRunCancelled";
    public const string MoveDryRunFaulted = "MoveDryRunFaulted";

    public const string RemoveDryRunCompleted = "RemoveDryRunCompleted";
    public const string RemoveDryRunCancelled = "RemoveDryRunCancelled";
    public const string RemoveDryRunFaulted = "RemoveDryRunFaulted";

    public const string ImportPreCheckCompleted = "ImportPreCheckCompleted";
    public const string ImportPreCheckCancelled = "ImportPreCheckCancelled";
    public const string ImportPreCheckFaulted = "ImportPreCheckFaulted";

    public const string SplitMigrateMapCompleted = "SplitMigrateMapCompleted";
    public const string SplitMigrateMapCancelled = "SplitMigrateMapCancelled";
    public const string SplitMigrateMapFaulted = "SplitMigrateMapFaulted";
    public const string SplitMigrateProveCompleted = "SplitMigrateProveCompleted";
    public const string SplitMigrateProveCancelled = "SplitMigrateProveCancelled";
    public const string SplitMigrateProveFaulted = "SplitMigrateProveFaulted";
    public const string SplitMigrateRunCompleted = "SplitMigrateRunCompleted";
    public const string SplitMigrateRunCancelled = "SplitMigrateRunCancelled";
    public const string SplitMigrateRunFaulted = "SplitMigrateRunFaulted";
    public const string SplitMigrateVerifyCompleted = "SplitMigrateVerifyCompleted";
    public const string SplitMigrateVerifyCancelled = "SplitMigrateVerifyCancelled";
    public const string SplitMigrateVerifyFaulted = "SplitMigrateVerifyFaulted";

    public const string ApplyPlanCompleted = "ApplyPlanCompleted";
    public const string ApplyPlanCancelled = "ApplyPlanCancelled";
    public const string ApplyPlanFaulted = "ApplyPlanFaulted";

    public const string DestroyPlanCompleted = "DestroyPlanCompleted";
    public const string DestroyPlanCancelled = "DestroyPlanCancelled";
    public const string DestroyPlanFaulted = "DestroyPlanFaulted";

    public const string ApplyFromPlanCompleted = "ApplyFromPlanCompleted";
    public const string ApplyFromPlanCancelled = "ApplyFromPlanCancelled";
    public const string ApplyFromPlanFaulted = "ApplyFromPlanFaulted";

    public const string DestroyFromPlanCompleted = "DestroyFromPlanCompleted";
    public const string DestroyFromPlanCancelled = "DestroyFromPlanCancelled";
    public const string DestroyFromPlanFaulted = "DestroyFromPlanFaulted";

    public const string SourceRefreshCompleted = "SourceRefreshCompleted";
    public const string SourceRefreshCompletedV2 = "SourceRefreshCompletedV2";
    public const string SourceRefreshFaulted = "SourceRefreshFaulted";

    public const string AddLogs = "AddLogs";

    public const string ReportRunningTask = "ReportRunningTask";
    
    public const string ApplyCancelKillCompleted = "ApplyCancelKillCompleted";
    public const string DestroyCancelKillCompleted = "DestroyCancelKillCompleted";
    public const string SplitCancelKillCompleted = "SplitCancelKillCompleted";
    public const string MoveCancelKillCompleted = "MoveCancelKillCompleted";
    public const string ImportCancelKillCompleted = "ImportCancelKillCompleted";
    public const string RemoveCancelKillCompleted = "RemoveCancelKillCompleted";
    

    // Every family reports a shared step on its own endpoint: the runner runs one task,
    // and the name it answers on says which job asked. Deployment keeps the bare names above.

    public const string SplitGetModuleCompleted = "SplitGetModuleCompleted";
    public const string SplitGetModuleCancelled = "SplitGetModuleCancelled";
    public const string SplitGetModuleFaulted = "SplitGetModuleFaulted";
    public const string SplitInitCompleted = "SplitInitCompleted";
    public const string SplitInitCancelled = "SplitInitCancelled";
    public const string SplitInitFaulted = "SplitInitFaulted";
    public const string SplitValidateCompleted = "SplitValidateCompleted";
    public const string SplitValidateCancelled = "SplitValidateCancelled";
    public const string SplitValidateFaulted = "SplitValidateFaulted";
    public const string SplitPlanCompleted = "SplitPlanCompleted";
    public const string SplitPlanCancelled = "SplitPlanCancelled";
    public const string SplitPlanFaulted = "SplitPlanFaulted";

    public const string TransferGetModuleCompleted = "TransferGetModuleCompleted";
    public const string TransferGetModuleCancelled = "TransferGetModuleCancelled";
    public const string TransferGetModuleFaulted = "TransferGetModuleFaulted";
    public const string TransferInitCompleted = "TransferInitCompleted";
    public const string TransferInitCancelled = "TransferInitCancelled";
    public const string TransferInitFaulted = "TransferInitFaulted";
    public const string TransferValidateCompleted = "TransferValidateCompleted";
    public const string TransferValidateCancelled = "TransferValidateCancelled";
    public const string TransferValidateFaulted = "TransferValidateFaulted";

    public const string LookupAddressesGetModuleCompleted = "LookupAddressesGetModuleCompleted";
    public const string LookupAddressesGetModuleCancelled = "LookupAddressesGetModuleCancelled";
    public const string LookupAddressesGetModuleFaulted = "LookupAddressesGetModuleFaulted";
    public const string LookupAddressesInitCompleted = "LookupAddressesInitCompleted";
    public const string LookupAddressesInitCancelled = "LookupAddressesInitCancelled";
    public const string LookupAddressesInitFaulted = "LookupAddressesInitFaulted";

    public const string MoveGetModuleCompleted = "MoveGetModuleCompleted";
    public const string MoveGetModuleCancelled = "MoveGetModuleCancelled";
    public const string MoveGetModuleFaulted = "MoveGetModuleFaulted";
    public const string MoveInitCompleted = "MoveInitCompleted";
    public const string MoveInitCancelled = "MoveInitCancelled";
    public const string MoveInitFaulted = "MoveInitFaulted";

    public const string ImportGetModuleCompleted = "ImportGetModuleCompleted";
    public const string ImportGetModuleCancelled = "ImportGetModuleCancelled";
    public const string ImportGetModuleFaulted = "ImportGetModuleFaulted";
    public const string ImportInitCompleted = "ImportInitCompleted";
    public const string ImportInitCancelled = "ImportInitCancelled";
    public const string ImportInitFaulted = "ImportInitFaulted";

    public const string RemoveGetModuleCompleted = "RemoveGetModuleCompleted";
    public const string RemoveGetModuleCancelled = "RemoveGetModuleCancelled";
    public const string RemoveGetModuleFaulted = "RemoveGetModuleFaulted";
    public const string RemoveInitCompleted = "RemoveInitCompleted";
    public const string RemoveInitCancelled = "RemoveInitCancelled";
    public const string RemoveInitFaulted = "RemoveInitFaulted";

    public const string ApplyGetModuleCompleted = "ApplyGetModuleCompleted";
    public const string ApplyGetModuleCancelled = "ApplyGetModuleCancelled";
    public const string ApplyGetModuleFaulted = "ApplyGetModuleFaulted";
    public const string ApplyInitCompleted = "ApplyInitCompleted";
    public const string ApplyInitCancelled = "ApplyInitCancelled";
    public const string ApplyInitFaulted = "ApplyInitFaulted";
    public const string ApplyValidateCompleted = "ApplyValidateCompleted";
    public const string ApplyValidateCancelled = "ApplyValidateCancelled";
    public const string ApplyValidateFaulted = "ApplyValidateFaulted";
    public const string ApplyVariablesCompleted = "ApplyVariablesCompleted";
    public const string ApplyVariablesCancelled = "ApplyVariablesCancelled";
    public const string ApplyVariablesFaulted = "ApplyVariablesFaulted";

    public const string DestroyGetModuleCompleted = "DestroyGetModuleCompleted";
    public const string DestroyGetModuleCancelled = "DestroyGetModuleCancelled";
    public const string DestroyGetModuleFaulted = "DestroyGetModuleFaulted";
    public const string DestroyInitCompleted = "DestroyInitCompleted";
    public const string DestroyInitCancelled = "DestroyInitCancelled";
    public const string DestroyInitFaulted = "DestroyInitFaulted";
    public const string DestroyValidateCompleted = "DestroyValidateCompleted";
    public const string DestroyValidateCancelled = "DestroyValidateCancelled";
    public const string DestroyValidateFaulted = "DestroyValidateFaulted";
    public const string DestroyVariablesCompleted = "DestroyVariablesCompleted";
    public const string DestroyVariablesCancelled = "DestroyVariablesCancelled";
    public const string DestroyVariablesFaulted = "DestroyVariablesFaulted";


    public const string ApplyGetDefinitiveRevisionCompleted = "ApplyGetDefinitiveRevisionCompleted";
    public const string ApplyGetDefinitiveRevisionCancelled = "ApplyGetDefinitiveRevisionCancelled";
    public const string ApplyGetDefinitiveRevisionFaulted = "ApplyGetDefinitiveRevisionFaulted";
    public const string ApplyPolicyValidateCompleted = "ApplyPolicyValidateCompleted";
    public const string ApplyPolicyValidateCancelled = "ApplyPolicyValidateCancelled";
    public const string ApplyPolicyValidateFaulted = "ApplyPolicyValidateFaulted";
    public const string ApplyOutputCompleted = "ApplyOutputCompleted";
    public const string ApplyOutputCancelled = "ApplyOutputCancelled";
    public const string ApplyOutputFaulted = "ApplyOutputFaulted";
    public const string DestroyGetDefinitiveRevisionCompleted = "DestroyGetDefinitiveRevisionCompleted";
    public const string DestroyGetDefinitiveRevisionCancelled = "DestroyGetDefinitiveRevisionCancelled";
    public const string DestroyGetDefinitiveRevisionFaulted = "DestroyGetDefinitiveRevisionFaulted";
    public const string DestroyPolicyValidateCompleted = "DestroyPolicyValidateCompleted";
    public const string DestroyPolicyValidateCancelled = "DestroyPolicyValidateCancelled";
    public const string DestroyPolicyValidateFaulted = "DestroyPolicyValidateFaulted";
    public const string DestroyOutputCompleted = "DestroyOutputCompleted";
    public const string DestroyOutputCancelled = "DestroyOutputCancelled";
    public const string DestroyOutputFaulted = "DestroyOutputFaulted";

}