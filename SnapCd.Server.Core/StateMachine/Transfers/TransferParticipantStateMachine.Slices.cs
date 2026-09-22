// SPDX-License-Identifier: LicenseRef-Snap-CD-Source-Available-1.1
// Copyright (c) 2026 Karl Schriek / Schrieksoft.
// No license is granted to use this file, in whole or in part, (a) as training, fine-tuning, retrieval, or
// embedding data for any machine-learning model, or (b) as input to any machine-learning model, agent, or automated
// system for the purpose of producing a derivative work or reimplementation that is not otherwise permitted by the
// Snap CD Source-Available License (including any Competing Product as defined therein). Contact info@snapcd.io
// for terms covering either use.


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
    /// The demonolith slices. Unlike the preamble these are driven one at a time by the
    /// coordinator, because what a slice needs comes from the other participant: the receiver's map
    /// needs the source's fragment, and a consumer's proof needs its producer's outputs.
    ///
    /// The participant still owns the dispatch and the retry; it is only told when, and with what.
    /// </summary>
    private void Configure_Slices()
    {
        During(Idle, StoppedState,
            When(MapRequested)
                .Then(context =>
                {
                    var request = Request<TransferMigrateMapRequested>(context.Saga);
                    request.Map = context.Message.Map;
                    request.FragmentState = context.Message.FragmentState;
                    request.FragmentMeta = context.Message.FragmentMeta;
                    context.Publish(request);
                })
                .ThenAsync(context => RecordDispatched(context, "MigrateMap"))
                .TransitionTo(MigrateMapPending),

            When(ProveRequested)
                .Then(context => context.Saga.InputKey = context.Message.InputKey)
                .Then(context =>
                {
                    var request = Request<TransferMigrateProveRequested>(context.Saga);
                    request.Map = context.Message.Map;
                    request.Outputs = context.Message.Outputs;
                    context.Publish(request);
                })
                .ThenAsync(context => RecordDispatched(context, "MigrateProve"))
                .TransitionTo(MigrateProvePending)
        );

        During(MigrateMapPending,
            When(MigrateMapCompleted)
                .ThenAsync(context => RecordCompleted(context, "MigrateMap", ManualJobStepStatus.Succeeded))
                .Publish(context => new TransferParticipantMapped
                {
                    CorrelationId = context.Saga.CorrelationId,
                    OrganizationId = context.Saga.OrganizationId,
                    TransferId = context.Saga.TransferId,
                    ModuleId = context.Saga.ModuleId,
                    Role = context.Saga.Role,
                    ProveRound = context.Saga.ProveRound,
                    FragmentState = context.Message.FragmentState,
                    FragmentMeta = context.Message.FragmentMeta,
                    MapHash = context.Message.MapHash
                })
                .TransitionTo(Idle),
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
            // Exit 2 is the slice answering no. It ran, so it is a red verdict for the coordinator
            // to weigh rather than a fault to retry.
            When(MigrateProveCompleted)
                .ThenAsync(context => RecordCompleted(context, "MigrateProve",
                    context.Message.ExitCode == 0 ? ManualJobStepStatus.Succeeded : ManualJobStepStatus.Refused))
                .Publish(context => new TransferParticipantProved
                {
                    CorrelationId = context.Saga.CorrelationId,
                    OrganizationId = context.Saga.OrganizationId,
                    TransferId = context.Saga.TransferId,
                    ModuleId = context.Saga.ModuleId,
                    Role = context.Saga.Role,
                    ProveRound = context.Saga.ProveRound,
                    ExitCode = context.Message.ExitCode,
                    Outputs = context.Message.Outputs,
                    Verdict = context.Message.Verdict
                })
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

    private Action<BehaviorContext<TransferParticipantSaga, HeartbeatFailed>> LostRunner(string task) =>
        context =>
        {
            _logger.LogWarning(
                "Transfer {TransferId}: {Role} lost its runner at {Task}",
                context.Saga.TransferId, context.Saga.Role, task);

            context.Publish(StoppedAt(context.Saga, task, "The runner stopped responding."));
        };
}
