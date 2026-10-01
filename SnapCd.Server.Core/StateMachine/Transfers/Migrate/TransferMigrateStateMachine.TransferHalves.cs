// SPDX-License-Identifier: LicenseRef-Snap-CD-Source-Available-1.1
// Copyright (c) 2026 Karl Schriek / Schrieksoft.
// No license is granted to use this file, in whole or in part, (a) as training, fine-tuning, retrieval, or
// embedding data for any machine-learning model, or (b) as input to any machine-learning model, agent, or automated
// system for the purpose of producing a derivative work or reimplementation that is not otherwise permitted by the
// Snap CD Source-Available License (including any Competing Product as defined therein). Contact info@snapcd.io
// for terms covering either use.


using System.Text.Json;
using SnapCd.Server.Core.StateMachine.StateMigrations.Finalization;
using SnapCd.Server.Core.Events.Jobs.Module;
using SnapCd.Server.Core.StateMachine.StateMigrations.Activities;
using SnapCd.Server.Core.Events.System;
using SnapCd.Server.Core.Services.Crud.Transfers;
using SnapCd.Contracts.RunnerRequests.Transfers;
using MassTransit;
using Microsoft.Extensions.Logging;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using SnapCd.Server.Core.Database;
using SnapCd.Server.Core.Entities.Sagas;
using SnapCd.Server.Core.Enums;
using SnapCd.Server.Core.Events.Steps;
using SnapCd.Server.Core.Events.Steps.StateMigrations;
using SnapCd.Server.Core.Events.Steps.Transfer;
using SnapCd.Server.Core.Services.Crud;
using SnapCd.Server.Core.Services.Crud.StateMigrations;
using SnapCd.Server.Core.StateMachine.Jobs.Utils;

using SnapCd.Server.Core.StateMachine.Transfers.Migrate.Activities;

namespace SnapCd.Server.Core.StateMachine.Transfers.Migrate;

public partial class TransferMigrateStateMachine
{
    /// <summary>
    /// The demonolith steps, run one after the other without being told: map pins this Module's
    /// state, prove asks whether it plans clean with the moved resources in place.
    ///
    /// The Module reports once, when it has finished everything it was asked to do.
    /// </summary>
    private void Configure_TransferHalves()
    {
        // A prompt that arrives once this half has moved on is stale, not an error: the state that
        // waits for the fragment has already asked the store and been released by the answer.
        foreach (var state in new[]
                 {
                     TransferSelectRunnerInstancePending, TransferGetModulePending,
                     TransferInitPending, TransferValidatePending, AnalyseTransferRefactorMapPending,
                     TransferMigrateMapPending, TransferMigrateProvePending,
                     TransferMigrateRunPending, TransferMigrateVerifyPending,
                     TransferGetModuleWaitingForRunner, TransferInitWaitingForRunner,
                     TransferValidateWaitingForRunner, AnalyseTransferRefactorMapWaitingForRunner,
                     TransferMigrateMapWaitingForRunner, TransferMigrateProveWaitingForRunner,
                     TransferMigrateRunWaitingForRunner, TransferMigrateVerifyWaitingForRunner,
                     WaitingForApproval, WaitingForConsent, Completed, Failed
                 })
            During(state,
                Ignore(FragmentAvailableEvent),
                Ignore(OutputsAvailableEvent));

        // The half that waits for the fragment is not the one that waits for the outputs, so each
        // ignores the gate it never enters.
        During(WaitingForFragment, Ignore(OutputsAvailableEvent));
        During(WaitingForOutputs, Ignore(FragmentAvailableEvent));

        // What the map says decides which half moves first. The source cuts the fragment at its own
        // map step; the receiver's map cannot run until that fragment is in its working directory.
        During(AnalyseTransferRefactorMapPending,
            When(AnalyseTransferRefactorMapCompleted)
                .ThenAsync(context => RecordCompleted(context, "AnalyseTransferRefactorMap", StateMigrationStepStatus.Succeeded))
                .Then(context =>
                {
                    context.Saga.IsSource = context.Message.Role == TransferRoleKind.Source;
                    context.Saga.NeedsOutputsJson = JsonSerializer.Serialize(context.Message.NeedsOutputs);
                })
                .IfElse(
                    context => context.Message.Role == TransferRoleKind.Unknown,
                    unreadable => unreadable
                        .ThenAsync(context => RecordCompleted(
                            context, "AnalyseTransferRefactorMap", StateMigrationStepStatus.Faulted,
                            context.Message.Problem))
                        .Then(context => _logger.LogInformation(
                            "Transfer: Module {ModuleId} could not read the transfer map: {Problem}",
                            context.Saga.ModuleId, context.Message.Problem))
                        .ThenJobFailed().TransitionTo(Failed).Finalize(),
                    known => known.IfElse(
                        context => context.Message.Role == TransferRoleKind.Source,
                        source => SendOrWaitForRunner<AnalyseTransferRefactorMapCompleted, TransferMigrateMapRequested>(
                            source, "TransferMigrateMap",
                            TransferMigrateMapPending, TransferMigrateMapWaitingForRunner),
                        receiver => receiver
                            .Then(context =>
                            {
                                context.Saga.WaitingSince = DateTime.UtcNow;
                                _logger.LogInformation(
                                    "Transfer: Module {ModuleId} is waiting for the other module's fragment",
                                    context.Saga.ModuleId);
                            })
                            .TransitionTo(WaitingForFragment))),

            When(AnalyseTransferRefactorMapCancelled)
                .ThenAsync(context => RecordCompleted(context, "AnalyseTransferRefactorMap", StateMigrationStepStatus.Cancelled))
                .ThenJobCancelled().TransitionTo(Failed).Finalize(),
            When(AnalyseTransferRefactorMapFaulted)
                .ThenAsync(context => RecordCompleted(context, "AnalyseTransferRefactorMap", StateMigrationStepStatus.Faulted))
                .ThenJobFailed().TransitionTo(Failed).Finalize(),
            When(HeartbeatScheduled.Received).ThenHeartbeatScheduled(HeartbeatRequested),
            When(HeartbeatRequested.Completed).ThenHeartbeatCompleted(HeartbeatScheduled),
            When(HeartbeatRequested.Completed2)
                .ThenAsync(context => RecordCompleted(context, "AnalyseTransferRefactorMap", StateMigrationStepStatus.Faulted,
                    "The runner stopped responding."))
                .Then(LostRunner("AnalyseTransferRefactorMap")).ThenJobFailed().TransitionTo(Failed).Finalize()
        );

        WaitForRunner<AnalyseTransferRefactorMapCompleted, TransferMigrateMapRequested>(
            TransferMigrateMapWaitingForRunner, "TransferMigrateMap", TransferMigrateMapPending);

        // The fragment has landed, so the receiver's map can run. The request carries the files
        // because the receiver's runner has never seen the source's working directory.
        During(WaitingForFragment,
            // What is waited for is the fragment being in the store. The message is a prompt to
            // look again, never the proof, because it can arrive before this half has parked or
            // after it has moved on.
            When(WaitingForFragment.Enter)
                .Then(context => context.Saga.WaitingSince = DateTime.UtcNow)
                .Activity(x => x.OfType<CheckArtefactPresentActivity>()),

            When(FragmentAvailableEvent)
                .IfAsync(
                    context => FragmentIsStored(context),
                    present => ReleaseWithFragment<TransferFragmentAvailable>(present)),

            // Nothing is on a runner while it waits, so there is nothing to kill or wait out.
            When(CancelRequested)
                .Then(context => _logger.LogInformation(
                    "Transfer: cancelled while awaiting the other module's fragment for Module {ModuleId}",
                    context.Saga.ModuleId))
                .Activity(x => x.OfType<CancelStateMigrationJobActivity<TransferMigrateSaga, CancelStateMigrationJobRequested>>())
                .TransitionTo(Failed)
                .Finalize(),

            Ignore(HeartbeatScheduled.Received),
            Ignore(HeartbeatRequested.Completed),
            Ignore(HeartbeatRequested.Completed2));

        // The values this half's plan reads are stored, so it can prove. The request carries them
        // because demonolith threads them from a file in this root's own working directory.
        During(WaitingForOutputs,
            // As with the fragment: the store is what is waited for, and the message only says it
            // is worth looking again.
            When(WaitingForOutputs.Enter)
                .Then(context => context.Saga.WaitingSince = DateTime.UtcNow)
                .Activity(x => x.OfType<CheckArtefactPresentActivity>()),

            When(OutputsAvailableEvent)
                .IfAsync(
                    context => OutputsAreStored(context),
                    present => SendOrWaitForRunner<TransferOutputsAvailable, TransferMigrateProveRequested>(
                        present.Then(context =>
                        {
                            context.Saga.WaitingSince = null;
                            _logger.LogInformation(
                                "Transfer: Module {ModuleId} has the values it reads and may prove",
                                context.Saga.ModuleId);
                        }),
                        "TransferMigrateProve", TransferMigrateProvePending, TransferMigrateProveWaitingForRunner)),

            // Nothing is on a runner while it waits, so there is nothing to kill or wait out.
            When(CancelRequested)
                .Then(context => _logger.LogInformation(
                    "Transfer: cancelled while awaiting the other module's values for Module {ModuleId}",
                    context.Saga.ModuleId))
                .Activity(x => x.OfType<CancelStateMigrationJobActivity<TransferMigrateSaga, CancelStateMigrationJobRequested>>())
                .TransitionTo(Failed)
                .Finalize(),

            Ignore(HeartbeatScheduled.Received),
            Ignore(HeartbeatRequested.Completed),
            Ignore(HeartbeatRequested.Completed2));

        During(TransferMigrateMapPending,
            When(TransferMigrateMapCompleted)
                .ThenAsync(context => RecordCompleted(context, "TransferMigrateMap", StateMigrationStepStatus.Succeeded))
                .ThenAsync(StoreFragment)
                .IfElse(
                    // A half that reads nothing from the other proves straight away.
                    context => string.IsNullOrWhiteSpace(context.Saga.NeedsOutputsJson)
                               || context.Saga.NeedsOutputsJson == "[]",
                    ready => SendOrWaitForRunner<TransferMigrateMapCompleted, TransferMigrateProveRequested>(
                        ready, "TransferMigrateProve",
                        TransferMigrateProvePending, TransferMigrateProveWaitingForRunner),
                    waiting => waiting
                        .Then(context =>
                        {
                            context.Saga.WaitingSince = DateTime.UtcNow;
                            _logger.LogInformation(
                                "Transfer: Module {ModuleId} is waiting on the other module's outputs",
                                context.Saga.ModuleId);
                        })
                        .TransitionTo(WaitingForOutputs)),

            When(TransferMigrateMapCancelled)
                .ThenAsync(context => RecordCompleted(context, "TransferMigrateMap", StateMigrationStepStatus.Cancelled))
                .ThenJobCancelled().TransitionTo(Failed).Finalize(),
            When(TransferMigrateMapFaulted)
                .ThenAsync(context => RecordCompleted(context, "TransferMigrateMap", StateMigrationStepStatus.Faulted))
                .ThenJobFailed().TransitionTo(Failed).Finalize(),
            When(HeartbeatScheduled.Received).ThenHeartbeatScheduled(HeartbeatRequested),
            When(HeartbeatRequested.Completed).ThenHeartbeatCompleted(HeartbeatScheduled),
            When(HeartbeatRequested.Completed2)
                .ThenAsync(context => RecordCompleted(context, "TransferMigrateMap", StateMigrationStepStatus.Faulted,
                    "The runner stopped responding."))
                .Then(LostRunner("TransferMigrateMap")).ThenJobFailed().TransitionTo(Failed).Finalize()
        );

        During(TransferMigrateProvePending,
            // Exit 2 is demonolith answering no: it ran, so it is a verdict rather than a fault.
            When(TransferMigrateProveCompleted)
                .ThenAsync(context => RecordCompleted(context, "TransferMigrateProve",
                    context.Message.ExitCode == 0 ? StateMigrationStepStatus.Succeeded : StateMigrationStepStatus.Refused))
                .ThenAsync(StoreOutputs)
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
                            "Transfer: Module {ModuleId} refused the move: {Verdict}",
                            context.Saga.ModuleId, context.Message.Verdict))
                        .ThenJobFailed().TransitionTo(Failed).Finalize()),

            When(TransferMigrateProveCancelled)
                .ThenAsync(context => RecordCompleted(context, "TransferMigrateProve", StateMigrationStepStatus.Cancelled))
                .ThenJobCancelled().TransitionTo(Failed).Finalize(),
            When(TransferMigrateProveFaulted)
                .ThenAsync(context => RecordCompleted(context, "TransferMigrateProve", StateMigrationStepStatus.Faulted))
                .ThenJobFailed().TransitionTo(Failed).Finalize(),
            When(HeartbeatScheduled.Received).ThenHeartbeatScheduled(HeartbeatRequested),
            When(HeartbeatRequested.Completed).ThenHeartbeatCompleted(HeartbeatScheduled),
            When(HeartbeatRequested.Completed2)
                .ThenAsync(context => RecordCompleted(context, "TransferMigrateProve", StateMigrationStepStatus.Faulted,
                    "The runner stopped responding."))
                .Then(LostRunner("TransferMigrateProve")).ThenJobFailed().TransitionTo(Failed).Finalize()
        );

        // The write. From here the Module's state has changed, so a failure is a failed transfer
        // rather than something to undo.
        During(TransferMigrateRunPending,
            SendOrWaitForRunner<TransferMigrateRunCompleted, TransferMigrateVerifyRequested>(
                When(TransferMigrateRunCompleted)
                    .ThenAsync(context => RecordCompleted(context, "TransferMigrateRun", StateMigrationStepStatus.Succeeded))
                    .ThenAsync(RecordAddresses),
                "TransferMigrateVerify", TransferMigrateVerifyPending, TransferMigrateVerifyWaitingForRunner),

            When(TransferMigrateRunCancelled)
                .ThenAsync(context => RecordCompleted(context, "TransferMigrateRun", StateMigrationStepStatus.Cancelled))
                .ThenJobCancelled().TransitionTo(Failed).Finalize(),
            When(TransferMigrateRunFaulted)
                .ThenAsync(context => RecordCompleted(context, "TransferMigrateRun", StateMigrationStepStatus.Faulted))
                .ThenJobFailed().TransitionTo(Failed).Finalize(),
            When(HeartbeatScheduled.Received).ThenHeartbeatScheduled(HeartbeatRequested),
            When(HeartbeatRequested.Completed).ThenHeartbeatCompleted(HeartbeatScheduled),
            When(HeartbeatRequested.Completed2)
                .ThenAsync(context => RecordCompleted(context, "TransferMigrateRun", StateMigrationStepStatus.Faulted,
                    "The runner stopped responding."))
                .Then(LostRunner("TransferMigrateRun")).ThenJobFailed().TransitionTo(Failed).Finalize()
        );

        During(TransferMigrateVerifyPending,
            When(TransferMigrateVerifyCompleted)
                .ThenAsync(context => RecordCompleted(context, "TransferMigrateVerify", StateMigrationStepStatus.Succeeded))
                .Then(context => _logger.LogInformation(
                    "Transfer: Module {ModuleId} landed",
                    context.Saga.ModuleId))
                .ThenJobCompleted().TransitionTo(Completed).Finalize(),

            When(TransferMigrateVerifyCancelled)
                .ThenAsync(context => RecordCompleted(context, "TransferMigrateVerify", StateMigrationStepStatus.Cancelled))
                .ThenJobCancelled().TransitionTo(Failed).Finalize(),
            When(TransferMigrateVerifyFaulted)
                .ThenAsync(context => RecordCompleted(context, "TransferMigrateVerify", StateMigrationStepStatus.Faulted))
                .ThenJobFailed().TransitionTo(Failed).Finalize(),
            When(HeartbeatScheduled.Received).ThenHeartbeatScheduled(HeartbeatRequested),
            When(HeartbeatRequested.Completed).ThenHeartbeatCompleted(HeartbeatScheduled),
            When(HeartbeatRequested.Completed2)
                .ThenAsync(context => RecordCompleted(context, "TransferMigrateVerify", StateMigrationStepStatus.Faulted,
                    "The runner stopped responding."))
                .Then(LostRunner("TransferMigrateVerify")).ThenJobFailed().TransitionTo(Failed).Finalize()
        );
        WaitForRunner<TransferMigrateRunCompleted, TransferMigrateVerifyRequested>(
            TransferMigrateVerifyWaitingForRunner, "TransferMigrateVerify", TransferMigrateVerifyPending);
    }

    /// <summary>
    /// This Module's write. demonolith reads the map and the receiver's receipt from the checkout
    /// itself, so the request carries neither.
    /// </summary>
    private static TransferMigrateRunRequested RunRequest(TransferMigrateSaga saga) =>
        Request<TransferMigrateRunRequested>(saga);

    /// <summary>
    /// Whether the fragment is in the store. Asked of the store each time rather than kept on the
    /// saga, so a prompt that arrives early or twice cannot be answered from a stale copy.
    /// </summary>
    private static async Task<bool> FragmentIsStored<TMessage>(
        BehaviorContext<TransferMigrateSaga, TMessage> context)
        where TMessage : class =>
        context.Saga.TransferId is { } transferId
        && await PipeExtensions.GetPayload<IServiceProvider>(context)
            .GetRequiredService<TransferArtefactService>()
            .HasSourceFragment(transferId, context.Saga.OrganizationId);

    /// <summary>Whether the values this half reads are in the store.</summary>
    private static async Task<bool> OutputsAreStored<TMessage>(
        BehaviorContext<TransferMigrateSaga, TMessage> context)
        where TMessage : class =>
        context.Saga.TransferId is { } transferId
        && await PipeExtensions.GetPayload<IServiceProvider>(context)
            .GetRequiredService<TransferArtefactService>()
            .HasReceiverOutputs(transferId, context.Saga.OrganizationId);

    /// <summary>The fragment is there, so this half's map can run with it.</summary>
    private EventActivityBinder<TransferMigrateSaga, TMessage> ReleaseWithFragment<TMessage>(
        EventActivityBinder<TransferMigrateSaga, TMessage> binder)
        where TMessage : class =>
        SendOrWaitForRunner<TMessage, TransferMigrateMapRequested>(
            binder.Then(context =>
            {
                context.Saga.WaitingSince = null;
                _logger.LogInformation(
                    "Transfer: Module {ModuleId} has the fragment and may go ahead",
                    context.Saga.ModuleId);
            }),
            "TransferMigrateMap", TransferMigrateMapPending, TransferMigrateMapWaitingForRunner);

    /// <summary>
    /// Keeps the fragment the source just cut, and tells the waiting receiver it is there. The two
    /// halves run on their own runners and never see each other's working directory.
    /// </summary>
    private static async Task StoreFragment(
        BehaviorContext<TransferMigrateSaga, TransferMigrateMapCompleted> context)
    {
        if (context.Message.SourceFragment is not { } fragment
            || context.Message.SourceFragmentMeta is not { } meta
            || context.Saga.TransferId is not { } transferId) return;

        var services = PipeExtensions.GetPayload<IServiceProvider>(context);

        await services.GetRequiredService<TransferArtefactService>()
            .StoreSourceFragment(transferId, context.Saga.OrganizationId, fragment, meta);

        await context.Publish(new TransferFragmentAvailable
        {
            TransferId = transferId,
            OrganizationId = context.Saga.OrganizationId
        });
    }

    /// <summary>
    /// Keeps the output values this half's prove produced, for the half whose plan reads them.
    /// </summary>
    private static async Task StoreOutputs(
        BehaviorContext<TransferMigrateSaga, TransferMigrateProveCompleted> context)
    {
        if (context.Message.Outputs is not { } outputs
            || context.Saga.TransferId is not { } transferId) return;

        var services = PipeExtensions.GetPayload<IServiceProvider>(context);

        await services.GetRequiredService<TransferArtefactService>()
            .StoreReceiverOutputs(transferId, context.Saga.OrganizationId, outputs);

        await context.Publish(new TransferOutputsAvailable
        {
            TransferId = transferId,
            OrganizationId = context.Saga.OrganizationId,
            ProducedByModuleId = context.Saga.ModuleId
        });
    }

    private Action<BehaviorContext<TransferMigrateSaga, HeartbeatFailed>> LostRunner(string task) =>
        context =>
        {
            _logger.LogWarning(
                "Transfer: Module {ModuleId} lost its runner at {Task}",
                context.Saga.ModuleId, task);

        };

    /// <summary>
    /// Records which addresses this Module's write moved, the same way every state-touching job
    /// does. The runner reports the direction from demonolith's own map, so nothing here has to
    /// know which side of the move this Module is on.
    /// </summary>
    private static async Task RecordAddresses(
        BehaviorContext<TransferMigrateSaga, TransferMigrateRunCompleted> context)
    {
        var addresses = context.Message.TransferredAddresses;
        if (addresses.Count == 0) return;

        var results = addresses
            .Select(a => new AddressResult { Address = a, Outcome = AddressOutcome.Succeeded })
            .ToList();

        await PipeExtensions.GetPayload<IServiceProvider>(context)
            .GetRequiredService<StateMigrationAddressService>()
            .Record(
                context.Saga.CorrelationId, context.Saga.OrganizationId, context.Saga.ModuleId,
                context.Message.GaveUp ? AddressOperation.TransferOut : AddressOperation.TransferIn,
                results);
    }


}
