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
using SnapCd.Server.Core.Events.Transfers;
using SnapCd.Server.Core.StateMachine.Jobs.Utils;

namespace SnapCd.Server.Core.StateMachine.Transfers;

public partial class TransferParticipantStateMachine
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
            // The source stops here when the receiver has to prove first: the receiver needs the
            // fragment this slice just wrote, and cannot wait for a full run to finish.
            When(MigrateMapCompleted, context => context.Saga.StopAfterMap)
                .ThenAsync(context => RecordCompleted(context, "MigrateMap", ManualJobStepStatus.Succeeded))
                .Then(context =>
                {
                    context.Saga.ProducedFragmentState = context.Message.FragmentState;
                    context.Saga.ProducedFragmentMeta = context.Message.FragmentMeta;
                    context.Saga.NeedsValuesFromJson = JsonSerializer.Serialize(context.Message.NeedsValuesFrom);
                })
                .Publish(Ran)
                .TransitionTo(Idle),

            When(MigrateMapCompleted)
                .ThenAsync(context => RecordCompleted(context, "MigrateMap", ManualJobStepStatus.Succeeded))
                .Then(context =>
                {
                    context.Saga.ProducedFragmentState = context.Message.FragmentState;
                    context.Saga.ProducedFragmentMeta = context.Message.FragmentMeta;
                    context.Saga.NeedsValuesFromJson = JsonSerializer.Serialize(context.Message.NeedsValuesFrom);
                })
                .Publish(context => ProveRequest(context.Saga))
                .ThenAsync(context => RecordDispatched(context, "MigrateProve"))
                .TransitionTo(MigrateProvePending),

            When(MigrateMapFaulted)
                .ThenAsync(context => RecordCompleted(context, "MigrateMap", ManualJobStepStatus.Faulted))
                .Then(context => context.Publish(Stopped(context.Saga, "MigrateMap", context.Message)))
                .TransitionTo(StoppedState),
            When(HeartbeatScheduled.Received).ThenHeartbeatScheduled(HeartbeatRequested),
            When(HeartbeatRequested.Completed).ThenHeartbeatCompleted(HeartbeatScheduled),
            When(HeartbeatRequested.Completed2).Then(LostRunner("MigrateMap")).TransitionTo(StoppedState),
            When(StopRequested).Then(LogStop("MigrateMap")).TransitionTo(StoppedState)
        );

        During(MigrateProvePending,
            // Exit 2 is the slice answering no. It ran, so it is a verdict the coordinator weighs
            // rather than a fault to retry, and it is still a completed run.
            When(MigrateProveCompleted)
                .ThenAsync(context => RecordCompleted(context, "MigrateProve",
                    context.Message.ExitCode == 0 ? ManualJobStepStatus.Succeeded : ManualJobStepStatus.Refused))
                .Then(context =>
                {
                    context.Saga.ProveExitCode = context.Message.ExitCode;
                    context.Saga.Verdict = context.Message.Verdict;
                    context.Saga.OutputsJson = JsonSerializer.Serialize(context.Message.Outputs);
                })
                .Publish(Ran)
                .TransitionTo(Idle),

            When(MigrateProveFaulted)
                .ThenAsync(context => RecordCompleted(context, "MigrateProve", ManualJobStepStatus.Faulted))
                .Then(context => context.Publish(Stopped(context.Saga, "MigrateProve", context.Message)))
                .TransitionTo(StoppedState),
            When(HeartbeatScheduled.Received).ThenHeartbeatScheduled(HeartbeatRequested),
            When(HeartbeatRequested.Completed).ThenHeartbeatCompleted(HeartbeatScheduled),
            When(HeartbeatRequested.Completed2).Then(LostRunner("MigrateProve")).TransitionTo(StoppedState),
            When(StopRequested).Then(LogStop("MigrateProve")).TransitionTo(StoppedState)
        );
    }

    /// <summary>
    /// The one report this Module makes, carrying everything the other one might need from it.
    /// </summary>
    private static TransferParticipantRan Ran<TMessage>(BehaviorContext<TransferParticipantSaga, TMessage> context)
        where TMessage : class =>
        new()
        {
            CorrelationId = context.Saga.CorrelationId,
            OrganizationId = context.Saga.OrganizationId,
            TransferId = context.Saga.TransferId,
            ModuleId = context.Saga.ModuleId,
            Role = context.Saga.Role,
            ProveRound = context.Saga.ProveRound,
            DefinitiveRevision = context.Saga.DefinitiveRevision,
            FragmentState = context.Saga.ProducedFragmentState,
            FragmentMeta = context.Saga.ProducedFragmentMeta,
            NeedsValuesFrom = context.Saga.NeedsValuesFromJson == null
                ? []
                : JsonSerializer.Deserialize<List<string>>(context.Saga.NeedsValuesFromJson)!,
            Outputs = context.Saga.OutputsJson == null
                ? new Dictionary<string, string>()
                : JsonSerializer.Deserialize<Dictionary<string, string>>(context.Saga.OutputsJson)!,
            StoppedAfterMap = context.Saga.StopAfterMap,
            ProveExitCode = context.Saga.ProveExitCode,
            Verdict = context.Saga.Verdict
        };

    private Action<BehaviorContext<TransferParticipantSaga, HeartbeatFailed>> LostRunner(string task) =>
        context =>
        {
            _logger.LogWarning(
                "Transfer {TransferId}: {Role} lost its runner at {Task}",
                context.Saga.TransferId, context.Saga.Role, task);

            context.Publish(StoppedAt(context.Saga, task, "The runner stopped responding."));
        };
}
