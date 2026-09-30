// SPDX-License-Identifier: LicenseRef-Snap-CD-Source-Available-1.1
// Copyright (c) 2026 Karl Schriek / Schrieksoft.
// No license is granted to use this file, in whole or in part, (a) as training, fine-tuning, retrieval, or
// embedding data for any machine-learning model, or (b) as input to any machine-learning model, agent, or automated
// system for the purpose of producing a derivative work or reimplementation that is not otherwise permitted by the
// Snap CD Source-Available License (including any Competing Product as defined therein). Contact info@snapcd.io
// for terms covering either use.

using SnapCd.Server.Core.Entities.Sagas;
using SnapCd.Server.Core.Enums;
using SnapCd.Server.Core.Events.Jobs.Module;
using SnapCd.Server.Core.Events.Steps.StateMigrations;
using SnapCd.Server.Core.Events.System;
using SnapCd.Server.Core.Services.Crud.StateMigrations;

namespace SnapCd.Server.Core.StateMachine.StateMigrations;

/// <summary>
/// Moves addresses to where they should be. A dry run says what would move before anyone approves
/// it.
/// </summary>
public class MoveStateMachine(ILogger<MoveStateMachine> logger)
    : TerraformStateMigrationStateMachine<
        MoveSaga, MoveJobRequested, MoveApproved,
        MoveSelectRunnerInstanceRequested, MoveGetModuleRequested, MoveInitRequested,
        MoveSelectRunnerInstanceCompleted, MoveSelectRunnerInstanceCancelled, MoveSelectRunnerInstanceFaulted,
        MoveGetModuleCompleted, MoveGetModuleCancelled, MoveGetModuleFaulted,
        MoveInitCompleted, MoveInitCancelled, MoveInitFaulted,
        MoveDryRunRequested, MoveDryRunCompleted, MoveDryRunCancelled, MoveDryRunFaulted,
        MoveRequested, MoveCompleted, MoveCancelled, MoveFaulted>(logger)
{
    protected override string Verb => "Move";

    protected override AddressOperation RowOperation => AddressOperation.MoveFrom;

    protected override string PreCheckName => "MoveDryRun";

    protected override string MigrateStateName => "Move";

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
        MoveSaga saga, List<AddressResult> results, StateMigrationAddressService addresses) =>
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