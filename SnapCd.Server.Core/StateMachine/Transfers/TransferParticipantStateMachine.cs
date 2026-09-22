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
using SnapCd.Server.Core.Events.Runners;
using SnapCd.Server.Core.Events.Steps;
using SnapCd.Server.Core.Events.Steps.Transfer;
using SnapCd.Server.Core.Events.Transfers;
using SnapCd.Server.Core.Services.ResolvedConfiguration.HelperClasses;

namespace SnapCd.Server.Core.StateMachine.Transfers;

/// <summary>
/// One Module's sequence within a Transfer. It exists for as long as the Transfer does, so a retry
/// is this saga resuming rather than a new job, and so the hold it takes has an owner that outlives
/// any single round.
///
/// It decides nothing about the Transfer: the coordinator tells it to prepare, and it reports how
/// that went. Which participant proves first, who holds the fragment and when a push happens are
/// all the coordinator's.
/// </summary>
public partial class TransferParticipantStateMachine : MassTransitStateMachine<TransferParticipantSaga>
{
    private readonly ILogger<TransferParticipantStateMachine> _logger;

    // From the coordinator
    public Event<TransferParticipantRegistered> Registered { get; } = null!;
    public Event<TransferParticipantPrepareRequested> PrepareRequested { get; } = null!;
    public Event<TransferParticipantStopRequested> StopRequested { get; } = null!;
    public Event<TransferParticipantMapRequested> MapRequested { get; } = null!;
    public Event<TransferParticipantProveRequested> ProveRequested { get; } = null!;

    // Liveness: a transfer dispatches to two runners, so either can go away mid-round.
    public Event<RunnerReconnectedEvent> RunnerReconnectedEvent { get; } = null!;
    public Request<TransferParticipantSaga, HeartbeatRequested, HeartbeatCompleted, HeartbeatFailed> HeartbeatRequested { get; } = null!;
    public Schedule<TransferParticipantSaga, HeartbeatScheduled> HeartbeatScheduled { get; } = null!;

    // From the runner, each naming the Module it is for
    public Event<TransferSelectRunnerInstanceCompleted> SelectRunnerInstanceCompleted { get; } = null!;
    public Event<TransferSelectRunnerInstanceFaulted> SelectRunnerInstanceFaulted { get; } = null!;
    public Event<TransferGetModuleCompleted> GetModuleCompleted { get; } = null!;
    public Event<TransferGetModuleFaulted> GetModuleFaulted { get; } = null!;
    public Event<TransferInitCompleted> InitCompleted { get; } = null!;
    public Event<TransferInitFaulted> InitFaulted { get; } = null!;
    public Event<TransferValidateCompleted> ValidateCompleted { get; } = null!;
    public Event<TransferValidateFaulted> ValidateFaulted { get; } = null!;
    public Event<TransferPlanCompleted> PlanCompleted { get; } = null!;
    public Event<TransferPlanFaulted> PlanFaulted { get; } = null!;
    public Event<TransferMigrateMapCompleted> MigrateMapCompleted { get; } = null!;
    public Event<TransferMigrateMapFaulted> MigrateMapFaulted { get; } = null!;
    public Event<TransferMigrateProveCompleted> MigrateProveCompleted { get; } = null!;
    public Event<TransferMigrateProveFaulted> MigrateProveFaulted { get; } = null!;

    /// <summary>Registered, with nothing asked of it yet. Between rounds it returns here.</summary>
    public State Idle { get; } = null!;

    public State SelectRunnerInstancePending { get; } = null!;
    public State GetModulePending { get; } = null!;
    public State InitPending { get; } = null!;
    public State ValidatePending { get; } = null!;
    public State PlanPending { get; } = null!;

    // Each step's twin, entered when the runner is gone at send time. The step is re-sent on
    // reconnect rather than failed, because a runner restart is ordinary.
    public State SelectRunnerInstanceWaitingForRunner { get; } = null!;
    public State GetModuleWaitingForRunner { get; } = null!;
    public State InitWaitingForRunner { get; } = null!;
    public State ValidateWaitingForRunner { get; } = null!;
    public State PlanWaitingForRunner { get; } = null!;

    public State MigrateMapPending { get; } = null!;
    public State MigrateProvePending { get; } = null!;

    /// <summary>Its sequence stopped and the coordinator has been told; a retry starts a new round.</summary>
    public State StoppedState { get; } = null!;

    public TransferParticipantStateMachine(ILogger<TransferParticipantStateMachine> logger)
    {
        _logger = logger;

        InstanceState(x => x.CurrentState);

        Event(() => Registered, x => x.CorrelateById(y => y.Message.CorrelationId));
        Event(() => PrepareRequested, x => x.CorrelateById(y => y.Message.CorrelationId));
        Event(() => StopRequested, x => x.CorrelateById(y => y.Message.CorrelationId));
        Event(() => MapRequested, x => x.CorrelateById(y => y.Message.CorrelationId));
        Event(() => ProveRequested, x => x.CorrelateById(y => y.Message.CorrelationId));

        // Correlated by the runner this participant is pinned to, not by the job.
        Event(() => RunnerReconnectedEvent, x => x
            .CorrelateBy((saga, context) =>
                saga.RunnerId == context.Message.RunnerId &&
                saga.RunnerInstanceName == context.Message.InstanceName &&
                saga.OrganizationId == context.Message.OrganizationId)
            .SelectId(context => Guid.NewGuid()));

        Request(() => HeartbeatRequested, x => x.HeartbeatRequestId, o => { o.Timeout = TimeSpan.FromSeconds(60); });
        Schedule(() => HeartbeatScheduled, saga => saga.HeartbeatScheduleTokenId, config =>
        {
            config.Delay = TimeSpan.FromSeconds(60);
            config.Received = e => e.CorrelateById(context => context.Message.CorrelationId);
        });

        // Every runner reply names its Module, which is what tells the two participants' replies
        // apart under one Transfer.
        Event(() => SelectRunnerInstanceCompleted, x => x.CorrelateById(y => y.Message.CorrelationId));
        Event(() => SelectRunnerInstanceFaulted, x => x.CorrelateById(y => y.Message.CorrelationId));
        Event(() => GetModuleCompleted, x => x.CorrelateById(y => y.Message.CorrelationId));
        Event(() => GetModuleFaulted, x => x.CorrelateById(y => y.Message.CorrelationId));
        Event(() => InitCompleted, x => x.CorrelateById(y => y.Message.CorrelationId));
        Event(() => InitFaulted, x => x.CorrelateById(y => y.Message.CorrelationId));
        Event(() => ValidateCompleted, x => x.CorrelateById(y => y.Message.CorrelationId));
        Event(() => ValidateFaulted, x => x.CorrelateById(y => y.Message.CorrelationId));
        Event(() => PlanCompleted, x => x.CorrelateById(y => y.Message.CorrelationId));
        Event(() => PlanFaulted, x => x.CorrelateById(y => y.Message.CorrelationId));
        Event(() => MigrateMapCompleted, x => x.CorrelateById(y => y.Message.CorrelationId));
        Event(() => MigrateMapFaulted, x => x.CorrelateById(y => y.Message.CorrelationId));
        Event(() => MigrateProveCompleted, x => x.CorrelateById(y => y.Message.CorrelationId));
        Event(() => MigrateProveFaulted, x => x.CorrelateById(y => y.Message.CorrelationId));

        Initially(
            When(Registered)
                .Then(context =>
                {
                    context.Saga.CorrelationId = context.Message.CorrelationId;
                    context.Saga.OrganizationId = context.Message.OrganizationId;
                    context.Saga.TransferId = context.Message.TransferId;
                    context.Saga.Role = context.Message.Role;
                    context.Saga.ModuleId = context.Message.Declared.ModuleId;
                    context.Saga.DeclaredJson = JsonSerializer.Serialize(context.Message.Declared);
                    context.Saga.RunnerId = context.Message.Declared.RunnerId;
                    context.Saga.RunnerName = context.Message.Declared.RunnerName;
                    context.Saga.RunnerInstanceName = context.Message.Declared.RunnerInstanceName;
                    context.Saga.RootDirectory = context.Message.RootDirectory;

                    _logger.LogInformation(
                        "Transfer {TransferId}: {Role} is Module {ModuleId}",
                        context.Message.TransferId, context.Message.Role, context.Saga.ModuleId);
                })
                .TransitionTo(Idle)
        );

        // A round begins from Idle, or from Stopped when the coordinator retries.
        During(Idle, StoppedState,
            When(PrepareRequested)
                .Then(context =>
                {
                    context.Saga.CurrentJobId = context.Message.JobId;
                    context.Saga.ProveRound = context.Message.ProveRound;
                    context.Saga.ProveRef = context.Message.ProveRef;
                })
                .Then(context => context.Publish(Request<TransferSelectRunnerInstanceRequested>(context.Saga)))
                .ThenAsync(context => RecordDispatched(context, "SelectRunnerInstance"))
                .TransitionTo(SelectRunnerInstancePending)
        );

        Configure_Preamble();
        Configure_Slices();
    }
}
