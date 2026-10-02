// SPDX-License-Identifier: LicenseRef-Snap-CD-Source-Available-1.1
// Copyright (c) 2026 Karl Schriek / Schrieksoft.
// No license is granted to use this file, in whole or in part, (a) as training, fine-tuning, retrieval, or
// embedding data for any machine-learning model, or (b) as input to any machine-learning model, agent, or automated
// system for the purpose of producing a derivative work or reimplementation that is not otherwise permitted by the
// Snap CD Source-Available License (including any Competing Product as defined therein). Contact info@snapcd.io
// for terms covering either use.

using System.Text.Json;
using MassTransit;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using SnapCd.Server.Core.Entities.Sagas;
using SnapCd.Server.Core.Enums;
using SnapCd.Server.Core.Events.Jobs.Module;
using SnapCd.Server.Core.Events.Runners;
using SnapCd.Server.Core.StateMachine.Jobs.Activites;
using SnapCd.Server.Core.Events.Steps;
using SnapCd.Server.Core.Events.Steps.Base;
using SnapCd.Server.Core.Events.Steps.StateMigrations;
using SnapCd.Server.Core.Events.Steps.StateMigrations;
using SnapCd.Server.Core.Events.System;
using SnapCd.Server.Core.Services.Crud.StateMigrations;
using SnapCd.Server.Core.Services.Crud.Transfers;
using SnapCd.Server.Core.Services.ResolvedConfiguration.HelperClasses;
using SnapCd.Server.Core.StateMachine.Jobs.Utils;
using SnapCd.Server.Core.StateMachine.StateMigrations.Finalization;
using SnapCd.Server.Core.StateMachine.StateMigrations.Activities;

namespace SnapCd.Server.Core.StateMachine.StateMigrations;

/// <summary>
/// Migrates addresses in a Module's state, then asks the state what is there.
///
/// The state migration exiting cleanly is not proof the address arrived, so the list that follows is what the
/// ledger and the UI read. A batch that manages some and not others ends partially completed:
/// unfinished work with a remedy, not a failure.
///
/// A move, an import and a remove each close this over their own messages and their own saga, so a
/// reply can only ever reach the job that asked for it.
/// </summary>
public abstract partial class TerraformStateMigrationStateMachine<
    TSaga, TJobRequested, TApproved,
    TSelectRunnerInstanceRequested, TGetModuleRequested, TInitRequested,
    TSelectRunnerInstanceCompleted, TSelectRunnerInstanceCancelled, TSelectRunnerInstanceFaulted,
    TGetModuleCompleted, TGetModuleCancelled, TGetModuleFaulted,
    TInitCompleted, TInitCancelled, TInitFaulted,
    TPreCheckRequested, TPreCheckCompleted, TPreCheckCancelled, TPreCheckFaulted,
    TMigrateStateRequested, TMigrateStateCompleted, TMigrateStateCancelled, TMigrateStateFaulted,
    TCancelKillRequested, TDummyCancelKillCompleted, TCancelKillCompleted>
    : MassTransitStateMachine<TSaga>
    where TSaga : TerraformStateMigrationSagaBase, new()
    where TJobRequested : TerraformStateMigrationJobRequestedBase
    where TApproved : StateMigrationResumeEventBase, new()
    where TSelectRunnerInstanceRequested : StateMigrationStepRequestBase, new()
    where TGetModuleRequested : StateMigrationGetModuleRequestedBase, new()
    where TInitRequested : StateMigrationStepRequestBase, new()
    where TSelectRunnerInstanceCompleted : StateMigrationSelectRunnerInstanceCompletedBase
    where TSelectRunnerInstanceCancelled : StateMigrationStepResponseBase
    where TSelectRunnerInstanceFaulted : StateMigrationStepFaultedBase
    where TGetModuleCompleted : StateMigrationGetModuleCompletedBase
    where TGetModuleCancelled : StateMigrationStepResponseBase
    where TGetModuleFaulted : StateMigrationStepFaultedBase
    where TInitCompleted : StateMigrationStepResponseBase
    where TInitCancelled : StateMigrationStepResponseBase
    where TInitFaulted : StateMigrationStepFaultedBase
    where TPreCheckRequested : TerraformStateMigrationRequestBase, new()
    where TPreCheckCompleted : TerraformStateMigrationResponseBase
    where TPreCheckCancelled : StepResponseBase
    where TPreCheckFaulted : StepFaultedBase
    where TMigrateStateRequested : TerraformStateMigrationRequestBase, new()
    where TMigrateStateCompleted : TerraformStateMigrationResponseBase
    where TMigrateStateCancelled : StepResponseBase
    where TMigrateStateFaulted : StepFaultedBase
    where TCancelKillRequested : CancelKillRequestedBase, new()
    where TDummyCancelKillCompleted : class
    where TCancelKillCompleted : CancelKillCompletedBase
{
    private readonly ILogger _logger;

    /// <summary>The word the job is described by in anything an operator reads.</summary>
    protected abstract string Verb { get; }

    /// <summary>How an address this job touched is filed.</summary>
    protected abstract AddressOperation RowOperation { get; }

    /// <summary>What this family's pre-check is called in the steps an operator reads.</summary>
    protected abstract string PreCheckName { get; }

    /// <summary>What this family's migration is called in the steps an operator reads.</summary>
    protected abstract string MigrateStateName { get; }

    public Event<TJobRequested> JobRequested { get; } = null!;
    public Event<CancelStateMigrationJobRequested> CancelRequested { get; } = null!;

    public Event<TSelectRunnerInstanceCompleted> SelectRunnerInstanceCompleted { get; } = null!;
    public Event<TSelectRunnerInstanceCancelled> SelectRunnerInstanceCancelled { get; } = null!;
    public Event<TSelectRunnerInstanceFaulted> SelectRunnerInstanceFaulted { get; } = null!;
    public Event<TGetModuleCompleted> GetModuleCompleted { get; } = null!;
    public Event<TGetModuleCancelled> GetModuleCancelled { get; } = null!;
    public Event<TGetModuleFaulted> GetModuleFaulted { get; } = null!;
    public Event<TInitCompleted> InitCompleted { get; } = null!;
    public Event<TInitCancelled> InitCancelled { get; } = null!;
    public Event<TInitFaulted> InitFaulted { get; } = null!;
    public Event<TPreCheckCompleted> PreCheckCompleted { get; } = null!;
    public Event<TPreCheckCancelled> PreCheckCancelled { get; } = null!;
    public Event<TPreCheckFaulted> PreCheckFaulted { get; } = null!;
    public Event<TMigrateStateCompleted> MigrateStateCompleted { get; } = null!;
    public Event<TMigrateStateCancelled> MigrateStateCancelled { get; } = null!;
    public Event<TMigrateStateFaulted> MigrateStateFaulted { get; } = null!;
    public Event<LookupAddressesCompleted> ListCompleted { get; } = null!;
    public Event<LookupAddressesFaulted> ListFaulted { get; } = null!;

    public Event<RunnerReconnectedEvent> RunnerReconnectedEvent { get; } = null!;
    public Request<TSaga, HeartbeatRequested, HeartbeatCompleted, HeartbeatFailed> HeartbeatRequested { get; } = null!;
    public Schedule<TSaga, HeartbeatScheduled> HeartbeatScheduled { get; } = null!;

    public State SelectRunnerInstancePending { get; } = null!;
    public State GetModulePending { get; } = null!;
    public State InitPending { get; } = null!;
    public State PreCheckPending { get; } = null!;
    public State MigrateStatePending { get; } = null!;
    public State ListPending { get; } = null!;

    /// <summary>
    /// Entered when a step cannot be handed to a runner because none is connected: the request was
    /// never sent, so it is sent again once one reconnects. A runner that disappears while holding
    /// a step is a different thing, and the heartbeat ends the job for it.
    /// </summary>
    public State GetModuleWaitingForRunner { get; } = null!;
    public State InitWaitingForRunner { get; } = null!;
    public State PreCheckWaitingForRunner { get; } = null!;
    public State MigrateStateWaitingForRunner { get; } = null!;
    public State ListWaitingForRunner { get; } = null!;

    public State Completed { get; } = null!;
    public State Failed { get; } = null!;

    protected TerraformStateMigrationStateMachine(ILogger logger)
    {
        _logger = logger;

        InstanceState(x => x.CurrentState);
        SetCompletedWhenFinalized();

        Event(() => JobRequested, x => x.CorrelateById(y => y.Message.CorrelationId));
        Event(() => CancelRequested, x => x.CorrelateById(y => y.Message.CorrelationId));
        Event(() => SelectRunnerInstanceCompleted, x => x.CorrelateById(y => y.Message.CorrelationId));
        Event(() => SelectRunnerInstanceCancelled, x => x.CorrelateById(y => y.Message.CorrelationId));
        Event(() => SelectRunnerInstanceFaulted, x => x.CorrelateById(y => y.Message.CorrelationId));
        Event(() => GetModuleCompleted, x => x.CorrelateById(y => y.Message.CorrelationId));
        Event(() => GetModuleCancelled, x => x.CorrelateById(y => y.Message.CorrelationId));
        Event(() => GetModuleFaulted, x => x.CorrelateById(y => y.Message.CorrelationId));
        Event(() => InitCompleted, x => x.CorrelateById(y => y.Message.CorrelationId));
        Event(() => InitCancelled, x => x.CorrelateById(y => y.Message.CorrelationId));
        Event(() => InitFaulted, x => x.CorrelateById(y => y.Message.CorrelationId));
        Event(() => PreCheckCompleted, x => x.CorrelateById(y => y.Message.CorrelationId));
        Event(() => PreCheckCancelled, x => x.CorrelateById(y => y.Message.CorrelationId));
        Event(() => PreCheckFaulted, x => x.CorrelateById(y => y.Message.CorrelationId));
        Event(() => MigrateStateCompleted, x => x.CorrelateById(y => y.Message.CorrelationId));
        Event(() => MigrateStateCancelled, x => x.CorrelateById(y => y.Message.CorrelationId));
        Event(() => MigrateStateFaulted, x => x.CorrelateById(y => y.Message.CorrelationId));
        Event(() => ListCompleted, x => x.CorrelateById(y => y.Message.CorrelationId));
        Event(() => ListFaulted, x => x.CorrelateById(y => y.Message.CorrelationId));

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

        Initially(
            When(JobRequested)
                .Then(context =>
                {
                    context.Saga.CorrelationId = context.Message.CorrelationId;
                    context.Saga.OrganizationId = context.Message.Declared.OrganizationId;
                    context.Saga.ModuleId = context.Message.Declared.ModuleId;
                    context.Saga.DeclaredJson = JsonSerializer.Serialize(context.Message.Declared);
                    context.Saga.RunnerId = context.Message.Declared.RunnerId;
                    context.Saga.RunnerName = context.Message.Declared.RunnerName;
                    context.Saga.RunnerInstanceName = context.Message.Declared.RunnerInstanceName;
                    context.Saga.ApprovalTimeoutMinutes = context.Message.Declared.ApprovalTimeoutMinutes;
                    context.Saga.InstructionsJson = JsonSerializer.Serialize(context.Message.Instructions);

                    _logger.LogInformation(
                        "{Verb} of {Count} addresses starting on Module {ModuleId}",
                        Verb, context.Message.Instructions.Count, context.Saga.ModuleId);
                })
                .Publish(context => Request<TSelectRunnerInstanceRequested>(context.Saga))
                .ThenAsync(context => RecordDispatched(context, "SelectRunnerInstance"))
                .TransitionTo(SelectRunnerInstancePending)
        );

        Configure_Approval();
        Configure_Setup();
        Configure_CancelKill();

        // The state migration reports what it managed; the list that follows reports what is actually there.
        During(MigrateStatePending,
            When(MigrateStateCompleted)
                .ThenAsync(context => RecordCompleted(context, MigrateStateName, StateMigrationStepStatus.Succeeded))
                .ThenAsync(RecordMigrateState)
                .Activity(x => x.OfType<
                    SendTerraformStateMigrationStepToRunnerActivity<TSaga, TMigrateStateCompleted, LookupAddressesRequested>>())
                .IfElse(
                    context => context.Saga.PreviousStateBeforeWaiting != null,
                    noRunner => noRunner
                        .Activity(x => x.OfType<WaitingForRunnerActivity<TSaga, TMigrateStateCompleted>>())
                        .Then(context =>
                        {
                            context.Saga.WaitingSince = DateTime.UtcNow;
                            _logger.LogInformation(
                                "{Verb} on Module {ModuleId} wrote its addresses and is waiting for a runner to list them",
                                Verb, context.Saga.ModuleId);
                        })
                        .TransitionTo(ListWaitingForRunner),
                    sent => sent
                        .ThenAsync(context => RecordDispatched(context, "LookupAddresses"))
                        .Schedule(HeartbeatScheduled,
                            context => new HeartbeatScheduled
                            {
                                CorrelationId = context.Saga.CorrelationId,
                                OrganizationId = context.Saga.OrganizationId
                            })
                        .TransitionTo(ListPending)),

            When(MigrateStateCancelled)
                .ThenAsync(context => RecordCompleted(context, MigrateStateName, StateMigrationStepStatus.Cancelled))
                .Activity(x => x.OfType<CancelStateMigrationJobActivity<TSaga, TMigrateStateCancelled>>())
                .TransitionTo(Failed).Finalize(),

            When(MigrateStateFaulted)
                .ThenAsync(context => RecordCompleted(context, MigrateStateName, StateMigrationStepStatus.Faulted))
                .ThenJobFailed().TransitionTo(Failed).Finalize(),
            When(HeartbeatScheduled.Received).ThenHeartbeatScheduled(HeartbeatRequested),
            When(HeartbeatRequested.Completed).ThenHeartbeatCompleted(HeartbeatScheduled),
            When(HeartbeatRequested.Completed2)
                .ThenAsync(context => RecordCompleted(context, MigrateStateName, StateMigrationStepStatus.Faulted,
                    "The runner stopped responding."))
                .ThenJobFailed().TransitionTo(Failed).Finalize()
        );

        // The migration has already happened by the time this waits, so the job is not failed for
        // want of a runner to confirm it with.
        During(ListWaitingForRunner,
            When(ListWaitingForRunner.Enter)
                .Activity(x => x.OfType<CheckRunnerConnectionActivity<TSaga, TMigrateStateCompleted>>()),

            When(RunnerReconnectedEvent)
                .Activity(x => x.OfType<
                    SendTerraformStateMigrationStepToRunnerActivity<TSaga, RunnerReconnectedEvent, LookupAddressesRequested>>())
                .IfElse(
                    context => context.Saga.PreviousStateBeforeWaiting != null,
                    stillGone => stillGone,
                    sent => sent
                        .Activity(x => x.OfType<NotWaitingForRunnerActivity<TSaga, RunnerReconnectedEvent>>())
                        .Then(context => context.Saga.WaitingSince = null)
                        .ThenAsync(context => RecordDispatched(context, "LookupAddresses"))
                        .Schedule(HeartbeatScheduled,
                            context => new HeartbeatScheduled
                            {
                                CorrelationId = context.Saga.CorrelationId,
                                OrganizationId = context.Saga.OrganizationId
                            })
                        .TransitionTo(ListPending)),
            When(CancelRequested)
                .Then(context => _logger.LogInformation(
                    "{Verb} on Module {ModuleId} was cancelled while waiting to list what it wrote",
                    Verb, context.Saga.ModuleId))
                .ThenAsync(context => RecordCompleted(context, "LookupAddresses", StateMigrationStepStatus.Cancelled))
                .ThenJobPartiallyCompleted().TransitionTo(Completed).Finalize(),
            Ignore(HeartbeatScheduled.Received),
            Ignore(HeartbeatRequested.Completed),
            Ignore(HeartbeatRequested.Completed2));

        During(ListPending,
            When(ListCompleted)
                .ThenAsync(context => RecordCompleted(context, "LookupAddresses", StateMigrationStepStatus.Succeeded))
                .ThenAsync(ReportAddresses)
                .IfElse(
                    context => context.Saga.FailedCount == 0,
                    whole => whole.ThenJobCompleted().TransitionTo(Completed).Finalize(),
                    partial => partial
                        .Then(context => _logger.LogInformation(
                            "{Verb} on Module {ModuleId} left {Count} addresses undone",
                            Verb, context.Saga.ModuleId, context.Saga.FailedCount))
                        .ThenJobPartiallyCompleted().TransitionTo(Completed).Finalize()),

            // The state migration already happened, so a failed list leaves the job done but unobserved.
            When(ListFaulted)
                .ThenAsync(context => RecordCompleted(context, "LookupAddresses", StateMigrationStepStatus.Faulted))
                .ThenJobPartiallyCompleted().TransitionTo(Completed).Finalize(),

            // Nothing is being written by then, so the read is the operator's to abandon. The
            // migration still happened, so the job completes rather than failing.
            When(CancelRequested)
                .Then(context => _logger.LogInformation(
                    "{Verb} on Module {ModuleId} was cancelled while listing what it wrote",
                    Verb, context.Saga.ModuleId))
                .ThenAsync(context => RecordCompleted(context, "LookupAddresses", StateMigrationStepStatus.Cancelled))
                .ThenJobPartiallyCompleted().TransitionTo(Completed).Finalize(),
            When(HeartbeatScheduled.Received).ThenHeartbeatScheduled(HeartbeatRequested),
            When(HeartbeatRequested.Completed).ThenHeartbeatCompleted(HeartbeatScheduled),
            When(HeartbeatRequested.Completed2)
                .ThenAsync(context => RecordCompleted(context, "LookupAddresses", StateMigrationStepStatus.Faulted,
                    "The runner stopped responding."))
                .ThenJobPartiallyCompleted().TransitionTo(Completed).Finalize()
        );
    }

    /// <summary>
    /// Which addresses the list should ask the state about. An address the command refused is not
    /// worth asking about; one it claims to have managed is exactly what wants confirming.
    /// </summary>
    protected virtual List<string> AddressesToVerify(List<AddressResult> managed) =>
        managed.Select(r => r.Address).ToList();

    /// <summary>Anything filed beyond the addresses the state migration was asked for.</summary>
    protected virtual Task RecordExtraRows(
        TSaga saga, List<AddressResult> results, StateMigrationAddressService addresses) =>
        Task.CompletedTask;

    /// <summary>
    /// What the state migration itself managed, recorded so the list has something to check and the ledger has
    /// something to show.
    /// </summary>
    private async Task RecordMigrateState(BehaviorContext<TSaga, TMigrateStateCompleted> context)
    {
        var results = context.Message.Results;
        var managed = results.Where(r => r.Outcome == AddressOutcome.Succeeded).ToList();

        context.Saga.SucceededJson = JsonSerializer.Serialize(AddressesToVerify(managed));
        context.Saga.FailedCount = results.Count(r => r.Outcome == AddressOutcome.Failed);

        if (results.Count == 0) return;

        var addresses = PipeExtensions.GetPayload<IServiceProvider>(context)
            .GetRequiredService<StateMigrationAddressService>();

        await addresses.Record(
            context.Saga.CorrelationId, context.Saga.OrganizationId, context.Saga.ModuleId,
            RowOperation, results);

        await RecordExtraRows(context.Saga, results, addresses);
    }

    /// <summary>
    /// What the state says is there now, recorded beside what the state migration claimed. "The command
    /// worked" and "the address is there" stay separate facts.
    /// </summary>
    private static async Task ReportAddresses(
        BehaviorContext<TSaga, LookupAddressesCompleted> context)
    {
        var results = context.Message.Results;
        if (results.Count == 0) return;

        await PipeExtensions.GetPayload<IServiceProvider>(context)
            .GetRequiredService<StateMigrationAddressService>()
            .Record(
                context.Saga.CorrelationId, context.Saga.OrganizationId, context.Saga.ModuleId,
                AddressOperation.List, results);
    }
}
