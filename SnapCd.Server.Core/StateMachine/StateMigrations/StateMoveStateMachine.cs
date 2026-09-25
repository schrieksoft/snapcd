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
using SnapCd.Server.Core.Events.Steps;
using SnapCd.Server.Core.Events.Steps.Base;
using SnapCd.Server.Core.Events.Steps.StateMigrations;
using SnapCd.Server.Core.Events.Steps.ManualJobs;
using SnapCd.Server.Core.Events.System;
using SnapCd.Server.Core.Services.Crud.StateMigrations;
using SnapCd.Server.Core.Services.Crud.Transfers;
using SnapCd.Server.Core.Services.ResolvedConfiguration.HelperClasses;
using SnapCd.Server.Core.StateMachine.Jobs.Utils;
using SnapCd.Server.Core.StateMachine.ManualJobs.Finalization;

namespace SnapCd.Server.Core.StateMachine.StateMigrations;

/// <summary>
/// Moves, imports or removes addresses in a Module's state, then asks the state what is there.
///
/// The move exiting cleanly is not proof the address arrived, so the list that follows is what the
/// ledger and the UI read. A batch that manages some and not others ends partially completed:
/// unfinished work with a remedy, not a failure.
/// </summary>
public partial class StateMoveStateMachine : MassTransitStateMachine<StateMoveSaga>
{
    private readonly ILogger<StateMoveStateMachine> _logger;

    public Event<StateMoveJobRequested> JobRequested { get; } = null!;
    public Event<CancelManualModuleJobRequested> CancelRequested { get; } = null!;

    public Event<StateMoveSelectRunnerInstanceCompleted> SelectRunnerInstanceCompleted { get; } = null!;
    public Event<StateMoveSelectRunnerInstanceFaulted> SelectRunnerInstanceFaulted { get; } = null!;
    public Event<StateMoveGetModuleCompleted> GetModuleCompleted { get; } = null!;
    public Event<StateMoveGetModuleFaulted> GetModuleFaulted { get; } = null!;
    public Event<StateMoveInitCompleted> InitCompleted { get; } = null!;
    public Event<StateMoveInitFaulted> InitFaulted { get; } = null!;
    public Event<StateMoveCompleted> MoveCompleted { get; } = null!;
    public Event<StateMoveFaulted> MoveFaulted { get; } = null!;
    public Event<StateListFilteredCompleted> ListCompleted { get; } = null!;
    public Event<StateListFilteredFaulted> ListFaulted { get; } = null!;

    public Event<RunnerReconnectedEvent> RunnerReconnectedEvent { get; } = null!;
    public Request<StateMoveSaga, HeartbeatRequested, HeartbeatCompleted, HeartbeatFailed> HeartbeatRequested { get; } = null!;
    public Schedule<StateMoveSaga, HeartbeatScheduled> HeartbeatScheduled { get; } = null!;

    public State SelectRunnerInstancePending { get; } = null!;
    public State GetModulePending { get; } = null!;
    public State InitPending { get; } = null!;
    public State MovePending { get; } = null!;
    public State ListPending { get; } = null!;

    public State Completed { get; } = null!;
    public State Failed { get; } = null!;

    public StateMoveStateMachine(ILogger<StateMoveStateMachine> logger)
    {
        _logger = logger;

        InstanceState(x => x.CurrentState);
        SetCompletedWhenFinalized();

        Event(() => JobRequested, x => x.CorrelateById(y => y.Message.CorrelationId));
        Event(() => CancelRequested, x => x.CorrelateById(y => y.Message.CorrelationId));
        Event(() => SelectRunnerInstanceCompleted, x => x.CorrelateById(y => y.Message.CorrelationId));
        Event(() => SelectRunnerInstanceFaulted, x => x.CorrelateById(y => y.Message.CorrelationId));
        Event(() => GetModuleCompleted, x => x.CorrelateById(y => y.Message.CorrelationId));
        Event(() => GetModuleFaulted, x => x.CorrelateById(y => y.Message.CorrelationId));
        Event(() => InitCompleted, x => x.CorrelateById(y => y.Message.CorrelationId));
        Event(() => InitFaulted, x => x.CorrelateById(y => y.Message.CorrelationId));
        Event(() => MoveCompleted, x => x.CorrelateById(y => y.Message.CorrelationId));
        Event(() => MoveFaulted, x => x.CorrelateById(y => y.Message.CorrelationId));
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
                    context.Saga.Operation = context.Message.Operation;
                    context.Saga.InstructionsJson = JsonSerializer.Serialize(context.Message.Instructions);

                    _logger.LogInformation(
                        "{Operation} of {Count} addresses starting on Module {ModuleId}",
                        context.Message.Operation, context.Message.Instructions.Count, context.Saga.ModuleId);
                })
                .Publish(context => Request<StateMoveSelectRunnerInstanceRequested>(context.Saga))
                .ThenAsync(context => RecordDispatched(context, "SelectRunnerInstance"))
                .TransitionTo(SelectRunnerInstancePending)
        );

        Configure_Preamble();

        // The move reports what it managed; the list that follows reports what is actually there.
        During(MovePending,
            When(MoveCompleted)
                .ThenAsync(context => RecordCompleted(context, "StateMove", ManualJobStepStatus.Succeeded))
                .ThenAsync(RecordMove)
                .Publish(context => Request<StateListFilteredRequested>(context.Saga))
                .ThenAsync(context => RecordDispatched(context, "StateListFiltered"))
                .TransitionTo(ListPending),

            When(MoveFaulted)
                .ThenAsync(context => RecordCompleted(context, "StateMove", ManualJobStepStatus.Faulted))
                .ThenJobFailed().TransitionTo(Failed).Finalize(),
            When(HeartbeatScheduled.Received).ThenHeartbeatScheduled(HeartbeatRequested),
            When(HeartbeatRequested.Completed).ThenHeartbeatCompleted(HeartbeatScheduled),
            When(HeartbeatRequested.Completed2)
                .ThenAsync(context => RecordCompleted(context, "StateMove", ManualJobStepStatus.Faulted,
                    "The runner stopped responding."))
                .ThenJobFailed().TransitionTo(Failed).Finalize()
        );

        During(ListPending,
            When(ListCompleted)
                .ThenAsync(context => RecordCompleted(context, "StateListFiltered", ManualJobStepStatus.Succeeded))
                .ThenAsync(ReportAddresses)
                .IfElse(
                    context => context.Saga.FailedCount == 0,
                    whole => whole.ThenJobCompleted().TransitionTo(Completed).Finalize(),
                    partial => partial
                        .Then(context => _logger.LogInformation(
                            "{Operation} on Module {ModuleId} left {Count} addresses undone",
                            context.Saga.Operation, context.Saga.ModuleId, context.Saga.FailedCount))
                        .ThenJobPartiallyCompleted().TransitionTo(Completed).Finalize()),

            // The move already happened, so a failed list leaves the job done but unobserved.
            When(ListFaulted)
                .ThenAsync(context => RecordCompleted(context, "StateListFiltered", ManualJobStepStatus.Faulted))
                .ThenJobPartiallyCompleted().TransitionTo(Completed).Finalize(),
            When(HeartbeatScheduled.Received).ThenHeartbeatScheduled(HeartbeatRequested),
            When(HeartbeatRequested.Completed).ThenHeartbeatCompleted(HeartbeatScheduled),
            When(HeartbeatRequested.Completed2)
                .ThenAsync(context => RecordCompleted(context, "StateListFiltered", ManualJobStepStatus.Faulted,
                    "The runner stopped responding."))
                .ThenJobPartiallyCompleted().TransitionTo(Completed).Finalize()
        );
    }

    /// <summary>
    /// What the move itself managed. The addresses it succeeded on are what the list then asks
    /// about: there is no point asking the state about an address the command refused.
    /// </summary>
    private static async Task RecordMove(BehaviorContext<StateMoveSaga, StateMoveCompleted> context)
    {
        var results = context.Message.Results;

        var succeeded = results
            .Where(r => r.Outcome == AddressOutcome.Succeeded)
            .Select(r => r.Address)
            .ToList();

        context.Saga.SucceededJson = JsonSerializer.Serialize(succeeded);
        context.Saga.FailedCount = results.Count(r => r.Outcome == AddressOutcome.Failed);

        if (results.Count == 0) return;

        var addresses = PipeExtensions.GetPayload<IServiceProvider>(context)
            .GetRequiredService<ManualJobAddressService>();

        var operation = context.Saga.Operation;

        await addresses.Record(
            context.Saga.CorrelationId, context.Saga.OrganizationId, context.Saga.ModuleId,
            RowOperationFor(operation), results);

        // A move touches two addresses, and either should be findable by its own name.
        if (operation != StateEditOperation.Move) return;

        var destinations = results
            .Where(r => r.Target != null)
            .Select(r => new AddressResult { Address = r.Target!, Target = r.Address, Outcome = r.Outcome })
            .ToList();

        await addresses.Record(
            context.Saga.CorrelationId, context.Saga.OrganizationId, context.Saga.ModuleId,
            AddressOperation.MoveTo, destinations);
    }

    /// <summary>
    /// What the state says is there now, recorded beside what the move claimed. "The command
    /// worked" and "the address is there" stay separate facts.
    /// </summary>
    private static async Task ReportAddresses(
        BehaviorContext<StateMoveSaga, StateListFilteredCompleted> context)
    {
        var results = context.Message.Results;
        if (results.Count == 0) return;

        await PipeExtensions.GetPayload<IServiceProvider>(context)
            .GetRequiredService<ManualJobAddressService>()
            .Record(
                context.Saga.CorrelationId, context.Saga.OrganizationId, context.Saga.ModuleId,
                AddressOperation.List, results);
    }

    /// <summary>How one address is filed. A move's two ends are recorded separately.</summary>
    private static AddressOperation RowOperationFor(StateEditOperation operation) => operation switch
    {
        StateEditOperation.Move => AddressOperation.MoveFrom,
        StateEditOperation.Import => AddressOperation.Import,
        StateEditOperation.Remove => AddressOperation.Remove,
        _ => throw new ArgumentOutOfRangeException(nameof(operation))
    };
}
