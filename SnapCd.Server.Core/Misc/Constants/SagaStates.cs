// SPDX-License-Identifier: LicenseRef-Snap-CD-Source-Available-1.1
// Copyright (c) 2026 Karl Schriek / Schrieksoft.
// No license is granted to use this file, in whole or in part, (a) as training, fine-tuning, retrieval, or
// embedding data for any machine-learning model, or (b) as input to any machine-learning model, agent, or automated
// system for the purpose of producing a derivative work or reimplementation that is not otherwise permitted by the
// Snap CD Source-Available License (including any Competing Product as defined therein). Contact info@snapcd.io
// for terms covering either use.


namespace SnapCd.Server.Core.Misc.Constants;

/// <summary>
/// The saga state names a runner callback is authorized against. MassTransit persists the state
/// by name, so these are the strings in the CurrentState column.
/// </summary>
public static class SagaStates
{
    public const string GetDefinitiveRevisionPending = nameof(GetDefinitiveRevisionPending);
    public const string GetModulePending = nameof(GetModulePending);
    public const string InitPending = nameof(InitPending);
    public const string ValidatePending = nameof(ValidatePending);
    public const string VariablesPending = nameof(VariablesPending);
    public const string PlanPending = nameof(PlanPending);
    public const string PolicyValidatePending = nameof(PolicyValidatePending);
    public const string ApplyFromPlanPending = nameof(ApplyFromPlanPending);
    public const string OutputPending = nameof(OutputPending);

    public const string TransferMigrateMapPending = nameof(TransferMigrateMapPending);
    public const string TransferMigrateProvePending = nameof(TransferMigrateProvePending);
    public const string TransferMigrateRunPending = nameof(TransferMigrateRunPending);
    public const string TransferMigrateVerifyPending = nameof(TransferMigrateVerifyPending);

    public const string SplitPlanEmptyVerifyPending = nameof(SplitPlanEmptyVerifyPending);
    public const string SplitRefactorValidatePending = nameof(SplitRefactorValidatePending);
    public const string SplitRefactorDiffPending = nameof(SplitRefactorDiffPending);
    public const string SplitMigrateMapPending = nameof(SplitMigrateMapPending);
    public const string SplitMigrateProvePending = nameof(SplitMigrateProvePending);
    public const string SplitMigrateRunPending = nameof(SplitMigrateRunPending);
    public const string SplitMigrateVerifyPending = nameof(SplitMigrateVerifyPending);
    public const string TransferOutputsPending = nameof(TransferOutputsPending);

    public const string SplitGetModulePending = nameof(SplitGetModulePending);
    public const string SplitInitPending = nameof(SplitInitPending);
    public const string SplitPlanPending = nameof(SplitPlanPending);
    public const string SplitValidatePending = nameof(SplitValidatePending);
    public const string LookupAddressesGetModulePending = nameof(LookupAddressesGetModulePending);
    public const string LookupAddressesInitPending = nameof(LookupAddressesInitPending);
    public const string TransferGetModulePending = nameof(TransferGetModulePending);
    public const string TransferInitPending = nameof(TransferInitPending);
    public const string TransferPlanPending = nameof(TransferPlanPending);
    public const string TransferValidatePending = nameof(TransferValidatePending);

    public const string CancellingImmediateKill = nameof(CancellingImmediateKill);
    public const string CancellingAfterCurrent = nameof(CancellingAfterCurrent);

    /// <summary>
    /// A step dispatched before a cancellation arrived still reports back, so the saga's state
    /// before it started cancelling is honoured as well as its current one.
    /// </summary>
    public static readonly HashSet<string> Cancelling =
    [
        CancellingImmediateKill,
        CancellingAfterCurrent
    ];
}
