// SPDX-License-Identifier: LicenseRef-Snap-CD-Source-Available-1.1
// Copyright (c) 2026 Karl Schriek / Schrieksoft.
// No license is granted to use this file, in whole or in part, (a) as training, fine-tuning, retrieval, or
// embedding data for any machine-learning model, or (b) as input to any machine-learning model, agent, or automated
// system for the purpose of producing a derivative work or reimplementation that is not otherwise permitted by the
// Snap CD Source-Available License (including any Competing Product as defined therein). Contact info@snapcd.io
// for terms covering either use.

using SnapCd.Server.Core.Database;
using SnapCd.Server.Core.Services.Crud.Transfers;
using SnapCd.Server.Core.Entities.Sagas;
using SnapCd.Server.Core.Events.Steps.Base;
using SnapCd.Server.Core.Events.Steps.StateMigrations;
using SnapCd.Server.Core.Events.Steps.Transfer;
using SnapCd.Server.Core.Services.MaintenanceMode;
using SnapCd.Server.Core.StateMachine.Jobs.Activites;
using SnapCd.Server.Core.StateMachine.StateMigrations.Activities;

namespace SnapCd.Server.Core.StateMachine.Transfers.Migrate.Activities;

/// <summary>
/// A transfer's steps, with the root this Module works in and the ref the transfer runs against.
/// </summary>
public class SendTransferStepToRunnerActivity<TMessage, TOutgoingMessage>(
    SnapCdDbContext dbContext,
    IMaintenanceModeService maintenanceMode,
    TransferArtefactService artefacts,
    ILogger<SendToRunnerActivity<TransferMigrateSaga, TMessage, TOutgoingMessage>> logger)
    : SendStateMigrationStepToRunnerActivity<TransferMigrateSaga, TMessage, TOutgoingMessage>(
        dbContext, maintenanceMode, logger)
    where TMessage : class
    where TOutgoingMessage : StepRequestBase, new()
{
    /// <summary>
    /// The files demonolith reads from the root's own working directory. The two halves run on
    /// their own runners and never share a disk, so what one produced travels with the other's
    /// request; it is read here rather than held on the saga, so raw state stays in the store.
    /// </summary>
    protected override async Task Enrich(TOutgoingMessage message, TransferMigrateSaga saga)
    {
        if (saga.TransferId is not { } transferId) return;

        switch (message)
        {
            case TransferMigrateMapRequested map when !saga.IsSource:
            {
                var (fragment, meta) = await artefacts.ReadSourceFragment(
                    transferId, saga.OrganizationId);

                map.SourceFragment = fragment;
                map.SourceFragmentMeta = meta;
                break;
            }

            case TransferMigrateProveRequested prove:
                prove.ReceiverOutputs = await artefacts.ReadReceiverOutputs(
                    transferId, saga.OrganizationId);
                break;
        }
    }

    protected override TOutgoingMessage CreateMessage(TransferMigrateSaga saga)
    {
        var request = base.CreateMessage(saga);

        if (request is StateMigrationStepRequestBase stateMigrationStep)
            stateMigrationStep.RootDirectory = saga.RootDirectory;

        // Only the checkout takes a ref, and it takes the transfer's rather than the Module's own.
        if (request is TransferGetModuleRequested getModule)
            getModule.SourceRevisionOverride = saga.ProveRef;

        return request;
    }
}
