// SPDX-License-Identifier: LicenseRef-Snap-CD-Source-Available-1.1
// Copyright (c) 2026 Karl Schriek / Schrieksoft.
// No license is granted to use this file, in whole or in part, (a) as training, fine-tuning, retrieval, or
// embedding data for any machine-learning model, or (b) as input to any machine-learning model, agent, or automated
// system for the purpose of producing a derivative work or reimplementation that is not otherwise permitted by the
// Snap CD Source-Available License (including any Competing Product as defined therein). Contact info@snapcd.io
// for terms covering either use.

using SnapCd.Server.Core.Database;
using SnapCd.Server.Core.Entities.Sagas.Base;
using SnapCd.Server.Core.Events.Steps.Base;
using SnapCd.Server.Core.Events.Steps.StateMigrations;
using SnapCd.Server.Core.Services.MaintenanceMode;
using SnapCd.Server.Core.StateMachine.Jobs.Activites;

namespace SnapCd.Server.Core.StateMachine.StateMigrations.Activities;

/// <summary>
/// Sends a manual job's step to the server instance that owns the runner's connection, the way an
/// ordinary job's steps are sent. A runner-facing consumer is addressed by queue name and its
/// endpoint carries no topic subscription, so a published step reaches it on the SQL transport and
/// is dropped on Azure Service Bus.
/// </summary>
public class SendStateMigrationStepToRunnerActivity<TSaga, TMessage, TOutgoingMessage>(
    SnapCdDbContext dbContext,
    IMaintenanceModeService maintenanceMode,
    ILogger<SendToRunnerActivity<TSaga, TMessage, TOutgoingMessage>> logger)
    : SendToRunnerActivity<TSaga, TMessage, TOutgoingMessage>(dbContext, maintenanceMode, logger)
    where TSaga : StateMigrationSagaBase
    where TMessage : class
    where TOutgoingMessage : StepRequestBase, new()
{
    protected override TOutgoingMessage CreateMessage(TSaga saga)
    {
        var request = base.CreateMessage(saga);

        // Which Module the step is for: a manual job names it, an ordinary one has only its own.
        if (request is StateMigrationStepRequestBase stateMigrationStep)
            stateMigrationStep.ModuleId = saga.ModuleId;

        return request;
    }
}
