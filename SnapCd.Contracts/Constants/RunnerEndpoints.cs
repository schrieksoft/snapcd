// SPDX-License-Identifier: LicenseRef-Snap-CD-Source-Available-1.1
// Copyright (c) 2026 Karl Schriek / Schrieksoft.
// No license is granted to use this file, in whole or in part, (a) as training, fine-tuning, retrieval, or
// embedding data for any machine-learning model, or (b) as input to any machine-learning model, agent, or automated
// system for the purpose of producing a derivative work or reimplementation that is not otherwise permitted by the
// Snap CD Source-Available License (including any Competing Product as defined therein). Contact info@snapcd.io
// for terms covering either use.

namespace SnapCd.Contracts.Constants;

public static class RunnerEndpoints
{

    // The runner runs one checkout and one init whatever asked for it, but each family is dispatched
    // to its own endpoint so the runner knows which endpoint to answer on.
    public const string ApplyGetDefinitiveRevision = "ApplyGetDefinitiveRevision";
    public const string ApplyPolicyValidate = "ApplyPolicyValidate";
    public const string ApplyOutput = "ApplyOutput";
    public const string DestroyGetDefinitiveRevision = "DestroyGetDefinitiveRevision";
    public const string DestroyPolicyValidate = "DestroyPolicyValidate";
    public const string DestroyOutput = "DestroyOutput";
    public const string ApplyGetModule = "ApplyGetModule";
    public const string ApplyInit = "ApplyInit";
    public const string ApplyValidate = "ApplyValidate";
    public const string ApplyVariables = "ApplyVariables";
    public const string DestroyGetModule = "DestroyGetModule";
    public const string DestroyInit = "DestroyInit";
    public const string DestroyValidate = "DestroyValidate";
    public const string DestroyVariables = "DestroyVariables";
    public const string SplitValidate = "SplitValidate";
    public const string SplitPlan = "SplitPlan";
    public const string TransferValidate = "TransferValidate";
    public const string TransferPlan = "TransferPlan";
    public const string SplitGetModule = "SplitGetModule";
    public const string SplitInit = "SplitInit";
    public const string TransferGetModule = "TransferGetModule";
    public const string TransferInit = "TransferInit";
    public const string StateListFilteredGetModule = "StateListFilteredGetModule";
    public const string StateListFilteredInit = "StateListFilteredInit";
    public const string MoveGetModule = "MoveGetModule";
    public const string MoveInit = "MoveInit";
    public const string ImportGetModule = "ImportGetModule";
    public const string ImportInit = "ImportInit";
    public const string RemoveGetModule = "RemoveGetModule";
    public const string RemoveInit = "RemoveInit";
    public const string Variables = "Input";
    public const string ApplyPlan = "ApplyPlan";
    public const string DestroyPlan = "DestroyPlan";
    public const string SplitPlanEmptyVerify = "SplitPlanEmptyVerify";
    public const string SplitRefactorValidate = "SplitRefactorValidate";
    public const string SplitRefactorDiff = "SplitRefactorDiff";
    public const string TransferMigrateMap = "TransferMigrateMap";
    public const string TransferMigrateProve = "TransferMigrateProve";
    public const string TransferMigrateRun = "TransferMigrateRun";
    public const string TransferMigrateVerify = "TransferMigrateVerify";
    public const string TransferOutputs = "TransferOutputs";
    public const string StateListFiltered = "StateListFiltered";
    public const string StateMove = "StateMove";

    /// <summary>
    /// What the edit would do, asked before anyone is asked to approve it. A move and a remove ask
    /// the engine itself; an import has no such mode, so its check is that the address is free.
    /// </summary>
    public const string MoveDryRun = "MoveDryRun";

    public const string RemoveDryRun = "RemoveDryRun";

    public const string ImportPreCheck = "ImportPreCheck";
    public const string StateImport = "StateImport";
    public const string StateRemove = "StateRemove";

    public const string SplitMigrateMap = "SplitMigrateMap";
    public const string SplitMigrateProve = "SplitMigrateProve";
    public const string SplitMigrateRun = "SplitMigrateRun";
    public const string SplitMigrateVerify = "SplitMigrateVerify";
    public const string ApplyFromPlan = "ApplyFromPlan";
    public const string DestroyFromPlan = "DestroyFromPlan";
    public const string SourceRefresh = "SourceRefresh";
    public const string CancelKill = "CancelKill";
    public const string CancelGraceful = "CancelGraceful";
    public const string Ping = "Ping";
}