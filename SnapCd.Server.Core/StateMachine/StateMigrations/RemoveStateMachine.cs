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
using SnapCd.Server.Core.Events.Steps;

namespace SnapCd.Server.Core.StateMachine.StateMigrations;

/// <summary>
/// Takes addresses out of state, leaving the infrastructure alone. A dry run says what would go
/// before anyone approves it.
/// </summary>
public class RemoveStateMachine(ILogger<RemoveStateMachine> logger)
    : TerraformStateMigrationStateMachine<
        RemoveSaga, RemoveJobRequested, RemoveApproved,
        RemoveSelectRunnerInstanceRequested, RemoveGetModuleRequested, RemoveInitRequested,
        RemoveSelectRunnerInstanceCompleted, RemoveSelectRunnerInstanceCancelled, RemoveSelectRunnerInstanceFaulted,
        RemoveGetModuleCompleted, RemoveGetModuleCancelled, RemoveGetModuleFaulted,
        RemoveInitCompleted, RemoveInitCancelled, RemoveInitFaulted,
        RemoveDryRunRequested, RemoveDryRunCompleted, RemoveDryRunCancelled, RemoveDryRunFaulted,
        RemoveRequested, RemoveCompleted, RemoveCancelled, RemoveFaulted,
        RemoveCancelKillRequested, DummyRemoveCancelKillCompleted, RemoveCancelKillCompleted>(logger)
{
    protected override string Verb => "Remove";

    protected override AddressOperation RowOperation => AddressOperation.Remove;

    protected override string PreCheckName => "RemoveDryRun";

    protected override string MigrateStateName => "Remove";
}
