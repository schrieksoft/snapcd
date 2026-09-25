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
using SnapCd.Server.Core.Events.Steps.Transfer;
using SnapCd.Server.Core.Events.System;
using SnapCd.Server.Core.Services.Crud.StateMigrations;
using SnapCd.Server.Core.Services.Crud.Transfers;
using SnapCd.Server.Core.Services.ResolvedConfiguration.HelperClasses;
using SnapCd.Server.Core.StateMachine.Jobs.Utils;
using SnapCd.Server.Core.StateMachine.ManualJobs.Finalization;

namespace SnapCd.Server.Core.StateMachine.StateMigrations;

/// <summary>
/// Asks which addresses are in a Module's state. It runs the ordinary preamble - checkout, init,
/// validate - because listing state needs a working directory, then reports and ends.
///
/// It writes nothing, so there is no plan, no approval and no way for it to fail destructively.
/// </summary>
public partial class StateListFilteredStateMachine : MassTransitStateMachine<StateListFilteredSaga>
{
    private readonly ILogger<StateListFilteredStateMachine> _logger;

    public Event<StateListFilteredJobRequested> JobRequested { get; } = null!;
    public Event<CancelManualModuleJobRequested> CancelRequested { get; } = null!;

    public Event<StateListFilteredSelectRunnerInstanceCompleted> SelectRunnerInstanceCompleted { get; } = null!;
    public Event<StateListFilteredSelectRunnerInstanceFaulted> SelectRunnerInstanceFaulted { get; } = null!;
    public Event<StateListFilteredGetModuleCompleted> GetModuleCompleted { get; } = null!;
    public Event<StateListFilteredGetModuleFaulted> GetModuleFaulted { get; } = null!;
    public Event<StateListFilteredInitCompleted> InitCompleted { get; } = null!;
    public Event<StateListFilteredInitFaulted> InitFaulted { get; } = null!;
    public Event<StateListFilteredCompleted> ListCompleted { get; } = null!;
    public Event<StateListFilteredFaulted> ListFaulted { get; } = null!;

    public Event<RunnerReconnectedEvent> RunnerReconnectedEvent { get; } = null!;
    public Request<StateListFilteredSaga, HeartbeatRequested, HeartbeatCompleted, HeartbeatFailed> HeartbeatRequested { get; } = null!;
    public Schedule<StateListFilteredSaga, HeartbeatScheduled> HeartbeatScheduled { get; } = null!;

    public State SelectRunnerInstancePending { get; } = null!;
    public State GetModulePending { get; } = null!;
    public State InitPending { get; } = null!;
    public State ListPending { get; } = null!;

    public State Completed { get; } = null!;
    public State Failed { get; } = null!;

    public StateListFilteredStateMachine(ILogger<StateListFilteredStateMachine> logger)
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
                    context.Saga.AddressesJson = JsonSerializer.Serialize(context.Message.Addresses);

                    _logger.LogDebug(
                        "Checking {Count} addresses against Module {ModuleId}",
                        context.Message.Addresses.Count, context.Saga.ModuleId);
                })
                .Publish(context => Request<StateListFilteredSelectRunnerInstanceRequested>(context.Saga))
                .ThenAsync(context => RecordDispatched(context, "SelectRunnerInstance"))
                .TransitionTo(SelectRunnerInstancePending)
        );

        Configure_Preamble();

        During(ListPending,
            When(ListCompleted)
                .ThenAsync(context => RecordCompleted(context, "StateListFiltered", ManualJobStepStatus.Succeeded))
                .ThenAsync(ReportAddresses)
                .ThenJobCompleted().TransitionTo(Completed).Finalize(),

            When(ListFaulted)
                .ThenAsync(context => RecordCompleted(context, "StateListFiltered", ManualJobStepStatus.Faulted))
                .ThenJobFailed().TransitionTo(Failed).Finalize(),
            When(HeartbeatScheduled.Received).ThenHeartbeatScheduled(HeartbeatRequested),
            When(HeartbeatRequested.Completed).ThenHeartbeatCompleted(HeartbeatScheduled),
            When(HeartbeatRequested.Completed2)
                .ThenAsync(context => RecordCompleted(context, "StateListFiltered", ManualJobStepStatus.Faulted,
                    "The runner stopped responding."))
                .ThenJobFailed().TransitionTo(Failed).Finalize()
        );
    }

    /// <summary>
    /// Records what was found and tells anything watching. The ledger closes from this without the
    /// job knowing a transfer exists.
    /// </summary>
    private static async Task ReportAddresses(
        BehaviorContext<StateListFilteredSaga, StateListFilteredCompleted> context)
    {
        var results = context.Message.Results;
        if (results.Count == 0) return;

        var services = PipeExtensions.GetPayload<IServiceProvider>(context);

        await services.GetRequiredService<ManualJobAddressService>().Record(
            context.Saga.CorrelationId, context.Saga.OrganizationId, context.Saga.ModuleId,
            AddressOperation.List, results);

    }
}
