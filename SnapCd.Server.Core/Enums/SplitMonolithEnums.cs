// SPDX-License-Identifier: LicenseRef-Snap-CD-Source-Available-1.1
// Copyright (c) 2026 Karl Schriek / Schrieksoft.
// No license is granted to use this file, in whole or in part, (a) as training, fine-tuning, retrieval, or
// embedding data for any machine-learning model, or (b) as input to any machine-learning model, agent, or automated
// system for the purpose of producing a derivative work or reimplementation that is not otherwise permitted by the
// Snap CD Source-Available License (including any Competing Product as defined therein). Contact info@snapcd.io
// for terms covering either use.

namespace SnapCd.Server.Core.Enums;

/// <summary>
/// States a SplitMonolith saga can be in when a runner calls back. Separate from
/// ModuleJobSagaState because those name deployment steps a split never runs, and this names
/// steps a deployment never runs.
/// </summary>
public enum SplitMonolithSagaState
{
    SelectRunnerInstancePending,
    GetModulePending,
    GetModuleWaitingForRunner,
    InitPending,
    InitWaitingForRunner,
    ValidatePending,
    ValidateWaitingForRunner,
    PlanPending,
    PlanWaitingForRunner,
    PlanEmptyVerifyPending,
    PlanEmptyVerifyWaitingForRunner,
    RefactorValidatePending,
    RefactorValidateWaitingForRunner,
    RefactorDiffPending,
    RefactorDiffWaitingForRunner,
    MigrateMapPending,
    MigrateMapWaitingForRunner,
    MigrateProvePending,
    MigrateProveWaitingForRunner,
    WaitingForApproval,
    MigrateRunPending,
    MigrateRunWaitingForRunner,
    MigrateVerifyPending,
    MigrateVerifyWaitingForRunner,

    CancellingImmediateKill,
    CancellingImmediateGraceful,
    CancellingAfterCurrent,

    Completed,
    Failed,
    Cancelled,
    Declined
}

/// <summary>Runner callbacks a SplitMonolith job may make.</summary>
public enum SplitMonolithTaskEndpoint
{
    GetModuleCompleted,
    GetModuleCancelled,
    GetModuleFaulted,

    InitCompleted,
    InitCancelled,
    InitFaulted,

    ValidateCompleted,
    ValidateCancelled,
    ValidateFaulted,

    PlanCompleted,
    PlanCancelled,
    PlanFaulted,

    PlanEmptyVerifyCompleted,
    PlanEmptyVerifyCancelled,
    PlanEmptyVerifyFaulted,

    RefactorValidateCompleted,
    RefactorValidateCancelled,
    RefactorValidateFaulted,

    RefactorDiffCompleted,
    RefactorDiffCancelled,
    RefactorDiffFaulted,

    MigrateMapCompleted,
    MigrateMapCancelled,
    MigrateMapFaulted,

    MigrateProveCompleted,
    MigrateProveCancelled,
    MigrateProveFaulted,

    MigrateRunCompleted,
    MigrateRunCancelled,
    MigrateRunFaulted,

    MigrateVerifyCompleted,
    MigrateVerifyCancelled,
    MigrateVerifyFaulted,

    SelectRunnerInstanceCompleted,
    SelectRunnerInstanceCancelled,
    SelectRunnerInstanceFaulted
}
