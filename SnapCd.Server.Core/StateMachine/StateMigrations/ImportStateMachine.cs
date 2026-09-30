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

namespace SnapCd.Server.Core.StateMachine.StateMigrations;

/// <summary>
/// Brings resources that already exist under management. There is no dry run for an import, so the
/// check before approval is that the addresses are free.
/// </summary>
public class ImportStateMachine(ILogger<ImportStateMachine> logger)
    : TerraformStateMigrationStateMachine<
        ImportSaga, ImportJobRequested, ImportApproved,
        ImportSelectRunnerInstanceRequested, ImportGetModuleRequested, ImportInitRequested,
        ImportSelectRunnerInstanceCompleted, ImportSelectRunnerInstanceCancelled, ImportSelectRunnerInstanceFaulted,
        ImportGetModuleCompleted, ImportGetModuleCancelled, ImportGetModuleFaulted,
        ImportInitCompleted, ImportInitCancelled, ImportInitFaulted,
        ImportPreCheckRequested, ImportPreCheckCompleted, ImportPreCheckCancelled, ImportPreCheckFaulted,
        ImportRequested, ImportCompleted, ImportCancelled, ImportFaulted>(logger)
{
    protected override string Verb => "Import";

    protected override AddressOperation RowOperation => AddressOperation.Import;

    protected override string PreCheckName => "ImportPreCheck";

    protected override string MigrateStateName => "Import";
}