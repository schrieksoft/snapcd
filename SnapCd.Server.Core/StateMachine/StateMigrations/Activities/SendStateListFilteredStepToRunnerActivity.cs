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

/// <summary>A state list's steps, with the addresses it was asked about carried onto the list.</summary>
public class SendStateListFilteredStepToRunnerActivity<TMessage, TOutgoingMessage>(
    SnapCdDbContext dbContext,
    IMaintenanceModeService maintenanceMode,
    ILogger<SendToRunnerActivity<StateListFilteredSaga, TMessage, TOutgoingMessage>> logger)
    : SendManualStepToRunnerActivity<StateListFilteredSaga, TMessage, TOutgoingMessage>(
        dbContext, maintenanceMode, logger)
    where TMessage : class
    where TOutgoingMessage : StepRequestBase, new()
{
    protected override TOutgoingMessage CreateMessage(StateListFilteredSaga saga)
    {
        var request = base.CreateMessage(saga);

        if (request is StateListFilteredRequested list)
            list.Addresses = JsonSerializer.Deserialize<List<string>>(saga.AddressesJson) ?? [];

        return request;
    }
}
