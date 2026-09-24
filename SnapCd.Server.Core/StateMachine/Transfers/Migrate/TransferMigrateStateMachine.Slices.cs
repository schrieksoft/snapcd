// SPDX-License-Identifier: LicenseRef-Snap-CD-Source-Available-1.1
// Copyright (c) 2026 Karl Schriek / Schrieksoft.
// No license is granted to use this file, in whole or in part, (a) as training, fine-tuning, retrieval, or
// embedding data for any machine-learning model, or (b) as input to any machine-learning model, agent, or automated
// system for the purpose of producing a derivative work or reimplementation that is not otherwise permitted by the
// Snap CD Source-Available License (including any Competing Product as defined therein). Contact info@snapcd.io
// for terms covering either use.


using System.Text.Json;
using MassTransit;
using Microsoft.Extensions.Logging;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using SnapCd.Server.Core.Database;
using SnapCd.Server.Core.Entities.Sagas;
using SnapCd.Server.Core.Enums;
using SnapCd.Server.Core.Events.Steps;
using SnapCd.Server.Core.Events.Steps.Transfer;
using SnapCd.Server.Core.Events.System;
using SnapCd.Server.Core.Services.Crud;
using SnapCd.Server.Core.Services.Crud.Transfers;
using SnapCd.Server.Core.StateMachine.Jobs.Utils;

namespace SnapCd.Server.Core.StateMachine.Transfers.Migrate;

public partial class TransferMigrateStateMachine
{
    /// <summary>
    /// The demonolith slices, run one after the other without being told: map pins this Module's
    /// state, prove asks whether it plans clean with the moved resources in place.
    ///
    /// The Module reports once, when it has finished everything it was asked to do.
    /// </summary>
    private void Configure_Slices()
    {
        During(MigrateMapPending,
            DealWithOutputsStatus(
                When(MigrateMapCompleted)
                    .ThenAsync(context => RecordCompleted(context, "MigrateMap", ManualJobStepStatus.Succeeded))
                    .Then(context => context.Saga.NeedsOutputsJson =
                        JsonSerializer.Serialize(context.Message.NeedsOutputs)),
                transition: true),

            When(MigrateMapFaulted)
                .ThenAsync(context => RecordCompleted(context, "MigrateMap", ManualJobStepStatus.Faulted))
                .ThenJobFailed().TransitionTo(Failed).Finalize(),
            When(HeartbeatScheduled.Received).ThenHeartbeatScheduled(HeartbeatRequested),
            When(HeartbeatRequested.Completed).ThenHeartbeatCompleted(HeartbeatScheduled),
            When(HeartbeatRequested.Completed2)
                .ThenAsync(context => RecordCompleted(context, "MigrateMap", ManualJobStepStatus.Faulted,
                    "The runner stopped responding."))
                .Then(LostRunner("MigrateMap")).ThenJobFailed().TransitionTo(Failed).Finalize()
        );

        During(MigrateProvePending,
            // Exit 2 is demonolith answering no: it ran, so it is a verdict rather than a fault.
            When(MigrateProveCompleted)
                .ThenAsync(context => RecordCompleted(context, "MigrateProve",
                    context.Message.ExitCode == 0 ? ManualJobStepStatus.Succeeded : ManualJobStepStatus.Refused))
                .Then(context =>
                {
                    context.Saga.ProveExitCode = context.Message.ExitCode;
                    context.Saga.Verdict = context.Message.Verdict;
                })
                .IfElse(
                    context => context.Message.ExitCode == 0,
                    // The write is the one irreversible step, so it waits for approval. An already
                    // satisfied threshold goes straight through rather than parking.
                    clean => DealWithApprovalStatus(clean, transition: true),
                    // Nothing is written against a red prove.
                    refused => refused
                        .Then(context => _logger.LogInformation(
                            "Transfer {TransferId}: Module {ModuleId} refused the move: {Verdict}",
                            context.Saga.TransferId, context.Saga.ModuleId, context.Message.Verdict))
                        .ThenJobFailed().TransitionTo(Failed).Finalize()),

            When(MigrateProveFaulted)
                .ThenAsync(context => RecordCompleted(context, "MigrateProve", ManualJobStepStatus.Faulted))
                .ThenJobFailed().TransitionTo(Failed).Finalize(),
            When(HeartbeatScheduled.Received).ThenHeartbeatScheduled(HeartbeatRequested),
            When(HeartbeatRequested.Completed).ThenHeartbeatCompleted(HeartbeatScheduled),
            When(HeartbeatRequested.Completed2)
                .ThenAsync(context => RecordCompleted(context, "MigrateProve", ManualJobStepStatus.Faulted,
                    "The runner stopped responding."))
                .Then(LostRunner("MigrateProve")).ThenJobFailed().TransitionTo(Failed).Finalize()
        );

        // The write. From here the Module's state has changed, so a failure is a failed transfer
        // rather than something to undo.
        During(MigrateRunPending,
            When(MigrateRunCompleted)
                .ThenAsync(context => RecordCompleted(context, "MigrateRun", ManualJobStepStatus.Succeeded))
                .ThenAsync(OpenLedger)
                .Publish(context => Request<TransferMigrateVerifyRequested>(context.Saga))
                .ThenAsync(context => RecordDispatched(context, "MigrateVerify"))
                .TransitionTo(MigrateVerifyPending),

            When(MigrateRunFaulted)
                .ThenAsync(context => RecordCompleted(context, "MigrateRun", ManualJobStepStatus.Faulted))
                .ThenJobFailed().TransitionTo(Failed).Finalize(),
            When(HeartbeatScheduled.Received).ThenHeartbeatScheduled(HeartbeatRequested),
            When(HeartbeatRequested.Completed).ThenHeartbeatCompleted(HeartbeatScheduled),
            When(HeartbeatRequested.Completed2)
                .ThenAsync(context => RecordCompleted(context, "MigrateRun", ManualJobStepStatus.Faulted,
                    "The runner stopped responding."))
                .Then(LostRunner("MigrateRun")).ThenJobFailed().TransitionTo(Failed).Finalize()
        );

        During(MigrateVerifyPending,
            When(MigrateVerifyCompleted)
                .ThenAsync(context => RecordCompleted(context, "MigrateVerify", ManualJobStepStatus.Succeeded))
                .ThenAsync(AccountForAddresses)
                .Then(context => _logger.LogInformation(
                    "Transfer {TransferId}: Module {ModuleId} landed",
                    context.Saga.TransferId, context.Saga.ModuleId))
                .Publish(context => Request<TransferOutputsRequested>(context.Saga))
                .ThenAsync(context => RecordDispatched(context, "Outputs"))
                .TransitionTo(OutputsPending),

            When(MigrateVerifyFaulted)
                .ThenAsync(context => RecordCompleted(context, "MigrateVerify", ManualJobStepStatus.Faulted))
                .ThenJobFailed().TransitionTo(Failed).Finalize(),
            When(HeartbeatScheduled.Received).ThenHeartbeatScheduled(HeartbeatRequested),
            When(HeartbeatRequested.Completed).ThenHeartbeatCompleted(HeartbeatScheduled),
            When(HeartbeatRequested.Completed2)
                .ThenAsync(context => RecordCompleted(context, "MigrateVerify", ManualJobStepStatus.Faulted,
                    "The runner stopped responding."))
                .Then(LostRunner("MigrateVerify")).ThenJobFailed().TransitionTo(Failed).Finalize()
        );
    }

    /// <summary>
    /// This Module's write. demonolith reads the map and the receiver's receipt from the checkout
    /// itself, so the request carries neither.
    /// </summary>
    private static TransferMigrateRunRequested RunRequest(TransferMigrateSaga saga) =>
        Request<TransferMigrateRunRequested>(saga);

    private Action<BehaviorContext<TransferMigrateSaga, HeartbeatFailed>> LostRunner(string task) =>
        context =>
        {
            _logger.LogWarning(
                "Transfer {TransferId}: Module {ModuleId} lost its runner at {Task}",
                context.Saga.TransferId, context.Saga.ModuleId, task);

        };

    /// <summary>
    /// Records what this Module's write moved. Both Modules report the same addresses, so the rows
    /// are opened once and the second report finds them already there.
    /// </summary>
    private static async Task OpenLedger(
        BehaviorContext<TransferMigrateSaga, TransferMigrateRunCompleted> context)
    {
        var addresses = context.Message.TransferredAddresses;
        if (addresses.Count == 0) return;

        var services = PipeExtensions.GetPayload<IServiceProvider>(context);

        var dbContextFactory = services.GetRequiredService<IDbContextFactory<SnapCdDbContext>>();
        await using var dbContext = await dbContextFactory.CreateDbContextAsync();

        var transfer = await dbContext.Transfers.AsNoTracking().FirstOrDefaultAsync(t =>
            t.Id == context.Saga.TransferId && t.OrganizationId == context.Saga.OrganizationId);

        if (transfer is null) return;

        var thisModuleGivesUp = transfer.ModuleId == context.Saga.ModuleId;

        await services.GetRequiredService<TransferLedger>().Open(
            transfer.Id,
            transfer.OrganizationId,
            leftModuleId: thisModuleGivesUp ? transfer.ModuleId : transfer.CounterpartyModuleId,
            arrivedModuleId: thisModuleGivesUp ? transfer.CounterpartyModuleId : transfer.ModuleId,
            addresses);
    }

    /// <summary>
    /// Reports what this Module's state now holds, once verify has confirmed the write. The
    /// addresses come from the rows the write opened, so there is one record of them.
    /// </summary>
    private static async Task AccountForAddresses(
        BehaviorContext<TransferMigrateSaga, TransferMigrateVerifyCompleted> context)
    {
        var services = PipeExtensions.GetPayload<IServiceProvider>(context);

        var dbContextFactory = services.GetRequiredService<IDbContextFactory<SnapCdDbContext>>();
        await using var dbContext = await dbContextFactory.CreateDbContextAsync();

        var moduleId = context.Saga.ModuleId;

        // The rows this Module's write opened; verify has now confirmed they landed.
        var addresses = await dbContext.TransferObjects.AsNoTracking()
            .Where(o => o.TransferId == context.Saga.TransferId
                        && o.OrganizationId == context.Saga.OrganizationId
                        && (o.LeftModuleId == moduleId || o.ArrivedModuleId == moduleId))
            .Select(o => new { o.Address, GaveUp = o.LeftModuleId == moduleId })
            .ToListAsync();

        if (addresses.Count == 0) return;

        await context.Publish(new StateAddressesTouched
        {
            ModuleId = moduleId,
            OrganizationId = context.Saga.OrganizationId,
            JobId = context.Saga.CorrelationId,
            Present = addresses.Where(a => !a.GaveUp).Select(a => a.Address).ToList(),
            Absent = addresses.Where(a => a.GaveUp).Select(a => a.Address).ToList()
        });
    }

    /// <summary>
    /// The last step on every transfer job: read this Module's outputs and store them, so a Module
    /// waiting on a value this one produces can go ahead. A failure here does not undo the move,
    /// which has already landed, so it ends the job as completed.
    /// </summary>
    private void Configure_OutputsStep()
    {
        During(OutputsPending,
            When(OutputsCompleted)
                .ThenAsync(context => RecordCompleted(context, "Outputs", ManualJobStepStatus.Succeeded))
                .ThenAsync(StoreOutputs)
                .ThenJobCompleted().TransitionTo(Completed).Finalize(),

            When(OutputsFaulted)
                .ThenAsync(context => RecordCompleted(context, "Outputs", ManualJobStepStatus.Faulted))
                .Then(context => _logger.LogWarning(
                    "Transfer {TransferId}: Module {ModuleId} landed but its outputs could not be read",
                    context.Saga.TransferId, context.Saga.ModuleId))
                .ThenJobCompleted().TransitionTo(Completed).Finalize(),
            When(HeartbeatScheduled.Received).ThenHeartbeatScheduled(HeartbeatRequested),
            When(HeartbeatRequested.Completed).ThenHeartbeatCompleted(HeartbeatScheduled),
            When(HeartbeatRequested.Completed2)
                .ThenAsync(context => RecordCompleted(context, "Outputs", ManualJobStepStatus.Faulted,
                    "The runner stopped responding."))
                .Then(LostRunner("Outputs")).ThenJobCompleted().TransitionTo(Completed).Finalize()
        );
    }

    /// <summary>Stores what this Module's state now produces, against the Module rather than the job.</summary>
    private static async Task StoreOutputs(
        BehaviorContext<TransferMigrateSaga, TransferOutputsCompleted> context)
    {
        if (context.Message.OutputSet is not { } outputSet) return;

        await PipeExtensions.GetPayload<IServiceProvider>(context)
            .GetRequiredService<OutputSetService>()
            .CreateWithOutputsNonsecured(outputSet, context.Saga.ModuleId, context.Saga.OrganizationId);
    }
}
