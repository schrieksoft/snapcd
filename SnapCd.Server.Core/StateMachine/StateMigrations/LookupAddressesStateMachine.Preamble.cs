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
using SnapCd.Server.Core.Events.Steps;
using SnapCd.Server.Core.Events.Steps.Base;
using SnapCd.Server.Core.Events.Steps.StateMigrations;
using SnapCd.Server.Core.Events.Steps.StateMigrations;
using SnapCd.Server.Core.Services.Crud.Transfers;
using SnapCd.Server.Core.Services.ResolvedConfiguration.HelperClasses;
using SnapCd.Server.Core.StateMachine.Jobs.Utils;
using SnapCd.Server.Core.StateMachine.StateMigrations.Finalization;

using SnapCd.Server.Core.StateMachine.StateMigrations.Activities;

namespace SnapCd.Server.Core.StateMachine.StateMigrations;

public partial class LookupAddressesStateMachine
{
    /// <summary>
    /// Everything a job does before it does its own work: pick the runner, fetch the code,
    /// initialise the backend. Listing state needs all three and nothing more, so the preamble
    /// stops at Init rather than planning.
    /// </summary>
    private void Configure_Preamble()
    {
        CreateStep<LookupAddressesSelectRunnerInstanceCompleted, LookupAddressesSelectRunnerInstanceFaulted, LookupAddressesGetModuleRequested>(
            LookupAddressesSelectRunnerInstancePending, SelectRunnerInstanceCompleted, SelectRunnerInstanceFaulted,
            "SelectRunnerInstance", "GetModule", LookupAddressesGetModulePending,
            context => context.Saga.RunnerInstanceName = context.Message.RunnerInstanceName);

        CreateStep<LookupAddressesGetModuleCompleted, LookupAddressesGetModuleFaulted, LookupAddressesInitRequested>(
            LookupAddressesGetModulePending, GetModuleCompleted, GetModuleFaulted, "GetModule", "Init", LookupAddressesInitPending,
            context => context.Saga.DefinitiveRevision = context.Message.DefinitiveRevision);

        CreateStep<LookupAddressesInitCompleted, LookupAddressesInitFaulted, LookupAddressesRequested>(
            LookupAddressesInitPending, InitCompleted, InitFaulted, "Init", "LookupAddresses", ListPending);

        During(LookupAddressesSelectRunnerInstancePending, LookupAddressesGetModulePending, LookupAddressesInitPending, ListPending,
            When(CancelRequested)
                .Then(context => _logger.LogInformation(
                    "State list on Module {ModuleId} was cancelled", context.Saga.ModuleId))
                .Activity(x => x.OfType<CancelStateMigrationJobActivity<LookupAddressesSaga, CancelStateMigrationJobRequested>>())
                .TransitionTo(Failed)
                .Finalize());
    }

    private void CreateStep<TCompleted, TFaulted, TNextRequest>(
        State duringState,
        Event<TCompleted> completedEvent,
        Event<TFaulted> faultedEvent,
        string task,
        string nextTask,
        State nextState,
        Action<BehaviorContext<LookupAddressesSaga, TCompleted>>? onCompleted = null)
        where TCompleted : StateMigrationStepResponseBase
        where TFaulted : StateMigrationStepFaultedBase
        where TNextRequest : StepRequestBase, new()
    {
        During(duringState,
            When(completedEvent)
                .Then(context => onCompleted?.Invoke(context))
                .ThenAsync(context => RecordCompleted(context, task, StateMigrationStepStatus.Succeeded))
                .Activity(x => x.OfType<SendLookupAddressesStepToRunnerActivity<TCompleted, TNextRequest>>())
                .ThenAsync(context => RecordDispatched(context, nextTask))
                .Schedule(HeartbeatScheduled,
                    context => new HeartbeatScheduled
                    {
                        CorrelationId = context.Saga.CorrelationId,
                        OrganizationId = context.Saga.OrganizationId
                    })
                .TransitionTo(nextState),

            When(faultedEvent)
                .ThenAsync(context => RecordCompleted(context, task, StateMigrationStepStatus.Faulted))
                .ThenJobFailed().TransitionTo(Failed).Finalize(),

            When(HeartbeatScheduled.Received).ThenHeartbeatScheduled(HeartbeatRequested),
            When(HeartbeatRequested.Completed).ThenHeartbeatCompleted(HeartbeatScheduled),
            When(HeartbeatRequested.Completed2)
                .ThenAsync(context => RecordCompleted(context, task, StateMigrationStepStatus.Faulted,
                    "The runner stopped responding."))
                .ThenJobFailed().TransitionTo(Failed).Finalize()
        );
    }


    private static async Task RecordDispatched<TMessage>(
        BehaviorContext<LookupAddressesSaga, TMessage> context, string task)
        where TMessage : class =>
        await PipeExtensions.GetPayload<IServiceProvider>(context)
            .GetRequiredService<StateMigrationStepService>()
            .Dispatched(
                context.Saga.CorrelationId, context.Saga.OrganizationId, context.Saga.ModuleId, task,
                null, context.Saga.RunnerInstanceName);

    private static async Task RecordCompleted<TMessage>(
        BehaviorContext<LookupAddressesSaga, TMessage> context,
        string task,
        StateMigrationStepStatus status,
        string? errorHeader = null)
        where TMessage : class
    {
        var faulted = context.Message as StepFaultedBase;

        await PipeExtensions.GetPayload<IServiceProvider>(context)
            .GetRequiredService<StateMigrationStepService>()
            .Completed(
                context.Saga.CorrelationId, context.Saga.OrganizationId, context.Saga.ModuleId, task, status,
                errorHeader: faulted?.ErrorMessage ?? errorHeader, error: faulted?.StackTrace);
    }

    /// <summary>The fields every step request carries, written once so a step cannot go astray.</summary>
    private static TRequest Request<TRequest>(LookupAddressesSaga saga)
        where TRequest : StepRequestBase, new()
    {
        var request = new TRequest
        {
            CorrelationId = saga.CorrelationId,
            OrganizationId = saga.OrganizationId,
            RunnerId = saga.RunnerId,
            RunnerInstanceName = saga.RunnerInstanceName ?? string.Empty,
            Declared = JsonSerializer.Deserialize<ResolvedModule>(saga.DeclaredJson)!
        };

        if (request is StateMigrationStepRequestBase stateMigrationStep)
            stateMigrationStep.ModuleId = saga.ModuleId;

        if (request is LookupAddressesRequested list)
            list.Addresses = JsonSerializer.Deserialize<List<string>>(saga.AddressesJson) ?? [];

        return request;
    }
}
