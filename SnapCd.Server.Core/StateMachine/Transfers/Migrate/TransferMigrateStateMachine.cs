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
using SnapCd.Server.Core.StateMachine.StateMigrations.Activities;
using SnapCd.Server.Core.Entities.Sagas;
using SnapCd.Server.Core.Enums;
using SnapCd.Server.Core.Events.Runners;
using SnapCd.Server.Core.Events.Steps;
using SnapCd.Server.Core.Events.Steps.Transfer;
using SnapCd.Server.Core.Events.Jobs.Module;
using SnapCd.Server.Core.Services.ResolvedConfiguration.HelperClasses;

namespace SnapCd.Server.Core.StateMachine.Transfers.Migrate;

/// <summary>
/// One Module's state move, start to finish: check out, plan, pull and pin the state, prove, and -
/// once approved - write and verify. The saga is the job, so it ends when the job does.
///
/// A transfer is two of these, one per Module. Nothing coordinates them: the source's job is
/// started after the receiver's has completed, and demonolith refuses to strip the source without
/// the receiver's run receipt. Finishing a side that failed is a new transfer, not a retry here.
/// </summary>
public partial class TransferMigrateStateMachine : MassTransitStateMachine<TransferMigrateSaga>
{
    private readonly ILogger<TransferMigrateStateMachine> _logger;

    public Event<TransferMigrateRequested> MigrateRequested { get; } = null!;

    // Liveness: the runner can go away mid-job.
    public Event<RunnerReconnectedEvent> RunnerReconnectedEvent { get; } = null!;
    public Request<TransferMigrateSaga, HeartbeatRequested, HeartbeatCompleted, HeartbeatFailed> HeartbeatRequested { get; } = null!;
    public Schedule<TransferMigrateSaga, HeartbeatScheduled> HeartbeatScheduled { get; } = null!;

    // From the runner, each naming the Module it is for
    public Event<TransferSelectRunnerInstanceCompleted> SelectRunnerInstanceCompleted { get; } = null!;
    public Event<TransferSelectRunnerInstanceFaulted> SelectRunnerInstanceFaulted { get; } = null!;
    public Event<TransferGetModuleCompleted> GetModuleCompleted { get; } = null!;
    public Event<TransferGetModuleFaulted> GetModuleFaulted { get; } = null!;
    public Event<TransferInitCompleted> InitCompleted { get; } = null!;
    public Event<TransferInitFaulted> InitFaulted { get; } = null!;
    public Event<TransferValidateCompleted> ValidateCompleted { get; } = null!;
    public Event<TransferValidateFaulted> ValidateFaulted { get; } = null!;
    public Event<TransferPlanCompleted> ApplyPlanCompleted { get; } = null!;
    public Event<TransferPlanFaulted> ApplyPlanFaulted { get; } = null!;
    public Event<TransferMigrateMapCompleted> TransferMigrateMapCompleted { get; } = null!;
    public Event<TransferMigrateMapFaulted> TransferMigrateMapFaulted { get; } = null!;
    public Event<TransferMigrateProveCompleted> TransferMigrateProveCompleted { get; } = null!;
    public Event<TransferMigrateProveFaulted> TransferMigrateProveFaulted { get; } = null!;
    public Event<TransferMigrateRunCompleted> TransferMigrateRunCompleted { get; } = null!;
    public Event<TransferMigrateRunFaulted> TransferMigrateRunFaulted { get; } = null!;
    public Event<TransferMigrateVerifyCompleted> TransferMigrateVerifyCompleted { get; } = null!;
    public Event<TransferMigrateVerifyFaulted> TransferMigrateVerifyFaulted { get; } = null!;
    public Event<TransferOutputsCompleted> TransferOutputsCompleted { get; } = null!;
    public Event<TransferOutputsFaulted> TransferOutputsFaulted { get; } = null!;

    /// <summary>The state move landed. Terminal.</summary>
    public State Completed { get; } = null!;

    public State TransferSelectRunnerInstancePending { get; } = null!;
    public State TransferGetModulePending { get; } = null!;
    public State TransferInitPending { get; } = null!;
    public State TransferValidatePending { get; } = null!;
    public State TransferPlanPending { get; } = null!;

    // Each step's twin, entered when the runner is gone at send time. The step is re-sent on
    // reconnect rather than failed, because a runner restart is ordinary.
    public State TransferSelectRunnerInstanceWaitingForRunner { get; } = null!;
    public State TransferGetModuleWaitingForRunner { get; } = null!;
    public State TransferInitWaitingForRunner { get; } = null!;
    public State TransferValidateWaitingForRunner { get; } = null!;
    public State TransferPlanWaitingForRunner { get; } = null!;

    public State TransferMigrateMapPending { get; } = null!;
    public State TransferMigrateProvePending { get; } = null!;
    public State TransferMigrateRunPending { get; } = null!;
    public State TransferMigrateVerifyPending { get; } = null!;
    public State TransferOutputsPending { get; } = null!;

    /// <summary>The job ended without landing. Terminal; finishing this Module is a new job.</summary>
    public State Failed { get; } = null!;

    public TransferMigrateStateMachine(ILogger<TransferMigrateStateMachine> logger)
    {
        _logger = logger;

        InstanceState(x => x.CurrentState);
        SetCompletedWhenFinalized();

        Event(() => MigrateRequested, x => x.CorrelateById(y => y.Message.CorrelationId));

        // Correlated by the runner this job is pinned to, not by the job itself.
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

        // Every runner reply names its Module, so a transfer's two jobs are never confused.
        Event(() => SelectRunnerInstanceCompleted, x => x.CorrelateById(y => y.Message.CorrelationId));
        Event(() => SelectRunnerInstanceFaulted, x => x.CorrelateById(y => y.Message.CorrelationId));
        Event(() => GetModuleCompleted, x => x.CorrelateById(y => y.Message.CorrelationId));
        Event(() => GetModuleFaulted, x => x.CorrelateById(y => y.Message.CorrelationId));
        Event(() => InitCompleted, x => x.CorrelateById(y => y.Message.CorrelationId));
        Event(() => InitFaulted, x => x.CorrelateById(y => y.Message.CorrelationId));
        Event(() => ValidateCompleted, x => x.CorrelateById(y => y.Message.CorrelationId));
        Event(() => ValidateFaulted, x => x.CorrelateById(y => y.Message.CorrelationId));
        Event(() => ApplyPlanCompleted, x => x.CorrelateById(y => y.Message.CorrelationId));
        Event(() => ApplyPlanFaulted, x => x.CorrelateById(y => y.Message.CorrelationId));
        Event(() => TransferMigrateMapCompleted, x => x.CorrelateById(y => y.Message.CorrelationId));
        Event(() => TransferMigrateMapFaulted, x => x.CorrelateById(y => y.Message.CorrelationId));
        Event(() => TransferMigrateProveCompleted, x => x.CorrelateById(y => y.Message.CorrelationId));
        Event(() => TransferMigrateProveFaulted, x => x.CorrelateById(y => y.Message.CorrelationId));
        Event(() => TransferMigrateRunCompleted, x => x.CorrelateById(y => y.Message.CorrelationId));
        Event(() => TransferMigrateRunFaulted, x => x.CorrelateById(y => y.Message.CorrelationId));
        Event(() => TransferMigrateVerifyCompleted, x => x.CorrelateById(y => y.Message.CorrelationId));
        Event(() => TransferOutputsCompleted, x => x.CorrelateById(y => y.Message.CorrelationId));
        Event(() => TransferOutputsFaulted, x => x.CorrelateById(y => y.Message.CorrelationId));
        Event(() => TransferMigrateVerifyFaulted, x => x.CorrelateById(y => y.Message.CorrelationId));

        Configure_Approval();

        // One job, started once. The saga is the job: it is created by the request that starts it
        // and finalized when it ends, like any other manual job.
        Initially(
            When(MigrateRequested)
                .Then(context =>
                {
                    context.Saga.CorrelationId = context.Message.CorrelationId;
                    context.Saga.OrganizationId = context.Message.Declared.OrganizationId;
                    context.Saga.CounterpartyModuleId = context.Message.CounterpartyModuleId;
                    context.Saga.TransferId = context.Message.TransferId;
                    context.Saga.ModuleId = context.Message.Declared.ModuleId;
                    context.Saga.DeclaredJson = JsonSerializer.Serialize(context.Message.Declared);
                    context.Saga.RunnerId = context.Message.Declared.RunnerId;
                    context.Saga.RunnerName = context.Message.Declared.RunnerName;
                    context.Saga.RunnerInstanceName = context.Message.Declared.RunnerInstanceName;
                    context.Saga.ApprovalTimeoutMinutes = context.Message.Declared.ApprovalTimeoutMinutes;
                    context.Saga.RootDirectory = context.Message.RootDirectory;
                    context.Saga.ProveRef = context.Message.ProveRef;

                    _logger.LogInformation(
                        "Transfer: moving state for Module {ModuleId}",
                        context.Saga.ModuleId);
                })
                .IfElse(
                    context => context.Message.AwaitConsent,
                    waiting => waiting
                        .ThenAsync(context => RecordDispatched(context, "WaitForCounterpartyConsent"))
                        .Activity(x => x.OfType<WaitingForConsentActivity<TransferMigrateSaga, TransferMigrateRequested>>())
                        .Then(context => context.Saga.WaitingSince = DateTime.UtcNow)
                        .TransitionTo(WaitingForConsent),
                    ahead => ahead
                        .Publish(context => Request<TransferSelectRunnerInstanceRequested>(context.Saga))
                        .ThenAsync(context => RecordDispatched(context, "SelectRunnerInstance"))
                        .TransitionTo(TransferSelectRunnerInstancePending))
        );

        Configure_Preamble();
        Configure_Consent();
        Configure_Outputs();
        Configure_Slices();
        Configure_OutputsStep();
    }
}
