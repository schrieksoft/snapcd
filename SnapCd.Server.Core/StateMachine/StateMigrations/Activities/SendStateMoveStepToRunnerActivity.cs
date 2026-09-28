// SPDX-License-Identifier: LicenseRef-Snap-CD-Source-Available-1.1
// Copyright (c) 2026 Karl Schriek / Schrieksoft.
// No license is granted to use this file, in whole or in part, (a) as training, fine-tuning, retrieval, or
// embedding data for any machine-learning model, or (b) as input to any machine-learning model, agent, or automated
// system for the purpose of producing a derivative work or reimplementation that is not otherwise permitted by the
// Snap CD Source-Available License (including any Competing Product as defined therein). Contact info@snapcd.io
// for terms covering either use.

using System.Text.Json;
using SnapCd.Server.Core.Database;
using SnapCd.Server.Core.Entities.Sagas;
using SnapCd.Server.Core.Events.Steps.Base;
using SnapCd.Server.Core.Events.Steps.StateMigrations;
using SnapCd.Server.Core.Services.MaintenanceMode;
using SnapCd.Server.Core.StateMachine.Jobs.Activites;
using SnapCd.Server.Core.StateMachine.ManualJobs.Activities;

namespace SnapCd.Server.Core.StateMachine.StateMigrations.Activities;

/// <summary>
/// A state edit's steps, with the edit's own payload carried onto the requests that take it.
/// </summary>
public class SendStateMoveStepToRunnerActivity<TMessage, TOutgoingMessage>(
    SnapCdDbContext dbContext,
    IMaintenanceModeService maintenanceMode,
    ILogger<SendToRunnerActivity<StateMoveSaga, TMessage, TOutgoingMessage>> logger)
    : SendManualStepToRunnerActivity<StateMoveSaga, TMessage, TOutgoingMessage>(
        dbContext, maintenanceMode, logger)
    where TMessage : class
    where TOutgoingMessage : StepRequestBase, new()
{
    protected override TOutgoingMessage CreateMessage(StateMoveSaga saga)
    {
        var request = base.CreateMessage(saga);

        if (request is StateMoveRequested move)
        {
            move.Operation = saga.Operation;
            move.Instructions =
                JsonSerializer.Deserialize<List<AddressInstruction>>(saga.InstructionsJson) ?? [];
        }

        // The list afterwards asks only about the addresses the edit managed.
        if (request is StateListFilteredRequested list)
            list.Addresses = JsonSerializer.Deserialize<List<string>>(saga.SucceededJson ?? "[]") ?? [];

        return request;
    }
}
