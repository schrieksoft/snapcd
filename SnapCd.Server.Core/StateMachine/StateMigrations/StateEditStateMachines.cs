// SPDX-License-Identifier: LicenseRef-Snap-CD-Source-Available-1.1
// Copyright (c) 2026 Karl Schriek / Schrieksoft.
// No license is granted to use this file, in whole or in part, (a) as training, fine-tuning, retrieval, or
// embedding data for any machine-learning model, or (b) as input to any machine-learning model, agent, or automated
// system for the purpose of producing a derivative work or reimplementation that is not otherwise permitted by the
// Snap CD Source-Available License (including any Competing Product as defined therein). Contact info@snapcd.io
// for terms covering either use.

using Microsoft.Extensions.Logging;
using SnapCd.Server.Core.Entities.Sagas;
using SnapCd.Server.Core.Enums;
using SnapCd.Server.Core.Events.Jobs.Module;
using SnapCd.Server.Core.Events.Steps.ManualJobs;
using SnapCd.Server.Core.Events.Steps.StateMigrations;
using SnapCd.Server.Core.Events.System;
using SnapCd.Server.Core.Services.Crud.StateMigrations;

namespace SnapCd.Server.Core.StateMachine.StateMigrations;

/// <summary>
/// Moves addresses to where they should be. A dry run says what would move before anyone approves
/// it.
/// </summary>
public class MoveStateMachine(ILogger<MoveStateMachine> logger)
    : StateEditStateMachine<
        MoveSaga, MoveJobRequested, MoveApproved,
        MoveSelectRunnerInstanceRequested, MoveGetModuleRequested, MoveInitRequested,
        MoveSelectRunnerInstanceCompleted, MoveSelectRunnerInstanceCancelled, MoveSelectRunnerInstanceFaulted,
        MoveGetModuleCompleted, MoveGetModuleCancelled, MoveGetModuleFaulted,
        MoveInitCompleted, MoveInitCancelled, MoveInitFaulted,
        MoveDryRunRequested, MoveDryRunCompleted, MoveDryRunFaulted,
        MoveRequested, MoveCompleted, MoveFaulted>(logger)
{
    protected override string Verb => "Move";

    protected override AddressOperation RowOperation => AddressOperation.MoveFrom;

    protected override string PreCheckName => "MoveDryRun";

    protected override string EditName => "Move";

    /// <summary>
    /// A move has two ends and both are checked: the old address should be gone and the new one
    /// should be there, and a move that only half happened is visible in neither end alone.
    /// </summary>
    protected override List<string> AddressesToVerify(List<AddressResult> managed) =>
        managed
            .SelectMany(r => string.IsNullOrWhiteSpace(r.Target)
                ? new[] { r.Address }
                : [r.Address, r.Target])
            .Distinct()
            .ToList();

    /// <summary>A move touches two addresses, and either should be findable by its own name.</summary>
    protected override Task RecordExtraRows(
        MoveSaga saga, List<AddressResult> results, ManualJobAddressService addresses) =>
        addresses.Record(
            saga.CorrelationId, saga.OrganizationId, saga.ModuleId, AddressOperation.MoveTo,
            results
                .Where(r => r.Target != null)
                .Select(r => new AddressResult
                {
                    Address = r.Target!, Target = r.Address, Outcome = r.Outcome
                })
                .ToList());
}

/// <summary>
/// Brings resources that already exist under management. There is no dry run for an import, so the
/// check before approval is that the addresses are free.
/// </summary>
public class ImportStateMachine(ILogger<ImportStateMachine> logger)
    : StateEditStateMachine<
        ImportSaga, ImportJobRequested, ImportApproved,
        ImportSelectRunnerInstanceRequested, ImportGetModuleRequested, ImportInitRequested,
        ImportSelectRunnerInstanceCompleted, ImportSelectRunnerInstanceCancelled, ImportSelectRunnerInstanceFaulted,
        ImportGetModuleCompleted, ImportGetModuleCancelled, ImportGetModuleFaulted,
        ImportInitCompleted, ImportInitCancelled, ImportInitFaulted,
        ImportPreCheckRequested, ImportPreCheckCompleted, ImportPreCheckFaulted,
        ImportRequested, ImportCompleted, ImportFaulted>(logger)
{
    protected override string Verb => "Import";

    protected override AddressOperation RowOperation => AddressOperation.Import;

    protected override string PreCheckName => "ImportPreCheck";

    protected override string EditName => "Import";
}

/// <summary>
/// Takes addresses out of state, leaving the infrastructure alone. A dry run says what would go
/// before anyone approves it.
/// </summary>
public class RemoveStateMachine(ILogger<RemoveStateMachine> logger)
    : StateEditStateMachine<
        RemoveSaga, RemoveJobRequested, RemoveApproved,
        RemoveSelectRunnerInstanceRequested, RemoveGetModuleRequested, RemoveInitRequested,
        RemoveSelectRunnerInstanceCompleted, RemoveSelectRunnerInstanceCancelled, RemoveSelectRunnerInstanceFaulted,
        RemoveGetModuleCompleted, RemoveGetModuleCancelled, RemoveGetModuleFaulted,
        RemoveInitCompleted, RemoveInitCancelled, RemoveInitFaulted,
        RemoveDryRunRequested, RemoveDryRunCompleted, RemoveDryRunFaulted,
        RemoveRequested, RemoveCompleted, RemoveFaulted>(logger)
{
    protected override string Verb => "Remove";

    protected override AddressOperation RowOperation => AddressOperation.Remove;

    protected override string PreCheckName => "RemoveDryRun";

    protected override string EditName => "Remove";
}
