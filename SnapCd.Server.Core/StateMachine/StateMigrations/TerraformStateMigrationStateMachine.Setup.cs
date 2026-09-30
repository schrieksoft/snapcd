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

public abstract partial class TerraformStateMigrationStateMachine<
    TSaga, TJobRequested, TApproved,
    TSelectRunnerInstanceRequested, TGetModuleRequested, TInitRequested,
    TSelectRunnerInstanceCompleted, TSelectRunnerInstanceCancelled, TSelectRunnerInstanceFaulted,
    TGetModuleCompleted, TGetModuleCancelled, TGetModuleFaulted,
    TInitCompleted, TInitCancelled, TInitFaulted,
    TPreCheckRequested, TPreCheckCompleted, TPreCheckCancelled, TPreCheckFaulted,
    TEditRequested, TEditCompleted, TEditCancelled, TEditFaulted>
{
    /// <summary>
    /// Everything a job does before it does its own work: pick the runner, fetch the code,
    /// initialise the backend, then say what the edit would do. The pre-check is what the approval
    /// gate gives an approver something to answer against.
    /// </summary>
    private void Configure_Setup()
    {
        CreateStep<TSelectRunnerInstanceCompleted, TSelectRunnerInstanceCancelled, TSelectRunnerInstanceFaulted, TGetModuleRequested>(
            SelectRunnerInstancePending, SelectRunnerInstanceCompleted, SelectRunnerInstanceCancelled,
            SelectRunnerInstanceFaulted,
            "SelectRunnerInstance", "GetModule", GetModulePending,
            context => context.Saga.RunnerInstanceName = context.Message.RunnerInstanceName);

        CreateStep<TGetModuleCompleted, TGetModuleCancelled, TGetModuleFaulted, TInitRequested>(
            GetModulePending, GetModuleCompleted, GetModuleCancelled, GetModuleFaulted,
            "GetModule", "Init", InitPending,
            context => context.Saga.DefinitiveRevision = context.Message.DefinitiveRevision);

        CreateStep<TInitCompleted, TInitCancelled, TInitFaulted, TPreCheckRequested>(
            InitPending, InitCompleted, InitCancelled, InitFaulted,
            "Init", PreCheckName, PreCheckPending);

        // The edit is the one irreversible step, so the pre-check hands to the approval gate rather
        // than dispatching it: the gate sends it once the threshold is answered.
        During(PreCheckPending,
            DealWithApprovalStatus(
                When(PreCheckCompleted)
                    .ThenAsync(context => RecordCompleted(
                        context, PreCheckName, StateMigrationStepStatus.Succeeded)),
                transition: true),

            When(PreCheckCancelled)
                .ThenAsync(context => RecordCompleted(
                    context, PreCheckName, StateMigrationStepStatus.Cancelled))
                .Activity(x => x.OfType<CancelStateMigrationJobActivity<TSaga, TPreCheckCancelled>>())
                .TransitionTo(Failed).Finalize(),

            When(PreCheckFaulted)
                .ThenAsync(context => RecordCompleted(
                    context, PreCheckName, StateMigrationStepStatus.Faulted))
                .ThenJobFailed().TransitionTo(Failed).Finalize(),

            When(HeartbeatScheduled.Received).ThenHeartbeatScheduled(HeartbeatRequested),
            When(HeartbeatRequested.Completed).ThenHeartbeatCompleted(HeartbeatScheduled),
            When(HeartbeatRequested.Completed2)
                .ThenAsync(context => RecordCompleted(
                    context, PreCheckName, StateMigrationStepStatus.Faulted,
                    "The runner stopped responding."))
                .ThenJobFailed().TransitionTo(Failed).Finalize());

        // Cancel is ignored once the edit is running: its addresses are already being written.
        During(SelectRunnerInstancePending, GetModulePending, InitPending, PreCheckPending,
            When(CancelRequested)
                .Then(context => _logger.LogInformation(
                    "{Verb} on Module {ModuleId} was cancelled before anything was written",
                    Verb, context.Saga.ModuleId))
                .Activity(x => x.OfType<CancelStateMigrationJobActivity<TSaga, CancelStateMigrationJobRequested>>())
                .TransitionTo(Failed)
                .Finalize());
    }

    private void CreateStep<TCompleted, TCancelled, TFaulted, TNextRequest>(
        State duringState,
        Event<TCompleted> completedEvent,
        Event<TCancelled> cancelledEvent,
        Event<TFaulted> faultedEvent,
        string task,
        string nextTask,
        State nextState,
        Action<BehaviorContext<TSaga, TCompleted>>? onCompleted = null)
        where TCompleted : class
        where TCancelled : class
        where TFaulted : class
        where TNextRequest : StepRequestBase, new()
    {
        During(duringState,
            When(completedEvent)
                .Then(context => onCompleted?.Invoke(context))
                .ThenAsync(context => RecordCompleted(context, task, StateMigrationStepStatus.Succeeded))
                .Activity(x => x.OfType<SendTerraformStateMigrationStepToRunnerActivity<TSaga, TCompleted, TNextRequest>>())
                .ThenAsync(context => RecordDispatched(context, nextTask))
                .Schedule(HeartbeatScheduled,
                    context => new HeartbeatScheduled
                    {
                        CorrelationId = context.Saga.CorrelationId,
                        OrganizationId = context.Saga.OrganizationId
                    })
                .TransitionTo(nextState),

            When(cancelledEvent)
                .ThenAsync(context => RecordCompleted(context, task, StateMigrationStepStatus.Cancelled))
                .Then(context => _logger.LogInformation(
                    "{Verb} on Module {ModuleId} was cancelled at {Task}",
                    Verb, context.Saga.ModuleId, task))
                .Activity(x => x.OfType<CancelStateMigrationJobActivity<TSaga, TCancelled>>())
                .TransitionTo(Failed).Finalize(),

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
        BehaviorContext<TSaga, TMessage> context, string task)
        where TMessage : class =>
        await PipeExtensions.GetPayload<IServiceProvider>(context)
            .GetRequiredService<StateMigrationStepService>()
            .Dispatched(
                context.Saga.CorrelationId, context.Saga.OrganizationId, context.Saga.ModuleId, task,
                null, context.Saga.RunnerInstanceName);

    private static async Task RecordCompleted<TMessage>(
        BehaviorContext<TSaga, TMessage> context,
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
    private static TRequest Request<TRequest>(TSaga saga)
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

        if (request is TerraformStateMigrationRequestBase edit)
            edit.Instructions =
                JsonSerializer.Deserialize<List<AddressInstruction>>(saga.InstructionsJson) ?? [];

        // The list asks only about the addresses the edit managed.
        if (request is StateListFilteredRequested list)
            list.Addresses = JsonSerializer.Deserialize<List<string>>(saga.SucceededJson ?? "[]") ?? [];

        return request;
    }
}
