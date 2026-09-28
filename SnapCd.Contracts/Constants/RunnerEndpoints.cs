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
    public const string Init = "Init";
    public const string GetModule = "GetModule";
    public const string Validate = "Validate";
    public const string PolicyValidate = "PolicyValidate";
    public const string Variables = "Input";
    public const string GetDefinitiveRevision = "GetDefinitiveRevision";
    public const string Plan = "Plan";
    public const string PlanDestroy = "PlanDestroy";
    public const string PlanEmptyVerify = "PlanEmptyVerify";
    public const string RefactorValidate = "RefactorValidate";
    public const string RefactorDiff = "RefactorDiff";
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
    public const string TransferRefactorDiff = "TransferRefactorDiff";

    public const string MigrateMap = "MigrateMap";
    public const string MigrateProve = "MigrateProve";
    public const string MigrateRun = "MigrateRun";
    public const string MigrateVerify = "MigrateVerify";
    public const string ApplyFromPlan = "ApplyFromPlan";
    public const string DestroyFromPlan = "DestroyFromPlan";
    public const string Output = "Output";
    public const string SourceRefresh = "SourceRefresh";
    public const string CancelKill = "CancelKill";
    public const string CancelGraceful = "CancelGraceful";
    public const string Ping = "Ping";
}