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
using SnapCd.Server.Core.Entities.Sagas;
using SnapCd.Server.Core.Enums;
using SnapCd.Server.Core.Events.Steps;
using SnapCd.Server.Core.Events.Steps.Transfer;
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
            When(MigrateMapCompleted)
                .ThenAsync(context => RecordCompleted(context, "MigrateMap", ManualJobStepStatus.Succeeded))
                .Then(context =>
                {
                    context.Saga.ProducedFragmentState = context.Message.FragmentState;
                    context.Saga.ProducedFragmentMeta = context.Message.FragmentMeta;
                })
                .Publish(context => ProveRequest(context.Saga))
                .ThenAsync(context => RecordDispatched(context, "MigrateProve"))
                .TransitionTo(MigrateProvePending),

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
                    context.Saga.OutputsJson = JsonSerializer.Serialize(context.Message.Outputs);
                })
                .IfElse(
                    context => context.Message.ExitCode == 0,
                    // The write is the one irreversible step, so it waits for approval. An already
                    // satisfied threshold goes straight through rather than parking.
                    clean => DealWithApprovalStatus(clean, transition: true),
                    // Nothing is written against a red prove.
                    refused => refused
                        .Then(context => _logger.LogInformation(
                            "Transfer {TransferId}: {Role} refused the move: {Verdict}",
                            context.Saga.TransferId, context.Saga.Role, context.Message.Verdict))
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
                .Then(context => _logger.LogInformation(
                    "Transfer {TransferId}: {Role} Module {ModuleId} landed",
                    context.Saga.TransferId, context.Saga.Role, context.Saga.ModuleId))
                .ThenJobCompleted().TransitionTo(Completed).Finalize(),

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
                "Transfer {TransferId}: {Role} lost its runner at {Task}",
                context.Saga.TransferId, context.Saga.Role, task);

        };
}
