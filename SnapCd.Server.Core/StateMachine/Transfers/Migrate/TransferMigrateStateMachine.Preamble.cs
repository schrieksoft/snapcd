// SPDX-License-Identifier: LicenseRef-Snap-CD-Source-Available-1.1
// Copyright (c) 2026 Karl Schriek / Schrieksoft.
// No license is granted to use this file, in whole or in part, (a) as training, fine-tuning, retrieval, or
// embedding data for any machine-learning model, or (b) as input to any machine-learning model, agent, or automated
// system for the purpose of producing a derivative work or reimplementation that is not otherwise permitted by the
// Snap CD Source-Available License (including any Competing Product as defined therein). Contact info@snapcd.io
// for terms covering either use.


using MassTransit;
using Microsoft.Extensions.Logging;
using System.Text.Json;
using SnapCd.Server.Core.Entities.Sagas;
using SnapCd.Server.Core.Enums;
using SnapCd.Server.Core.Events.Runners;
using SnapCd.Server.Core.Events.Steps;
using SnapCd.Server.Core.Events.Steps.Transfer;
using SnapCd.Server.Core.StateMachine.Jobs.Utils;
using Microsoft.Extensions.DependencyInjection;
using SnapCd.Server.Core.Services.Crud.Transfers;
using SnapCd.Server.Core.Services.ResolvedConfiguration.HelperClasses;

namespace SnapCd.Server.Core.StateMachine.Transfers.Migrate;

public partial class TransferMigrateStateMachine
{
    /// <summary>
    /// Wires one preamble step: on completion send the next request, on fault end the job. Generic
    /// over the step's own types so the five steps differ only in which events they name, the way
    /// JobStateMachine is generic over Apply's and Destroy's.
    /// </summary>
    private void CreateStep<TCompleted, TFaulted, TNextRequest>(
        State duringState,
        Event<TCompleted> completedEvent,
        Event<TFaulted> faultedEvent,
        string task,
        State nextState,
        State nextWaitingState,
        Action<BehaviorContext<TransferMigrateSaga, TCompleted>>? onCompleted = null)
        where TCompleted : TransferStepResponseBase
        where TFaulted : TransferStepFaultedBase
        where TNextRequest : TransferStepRequestBase, new()
    {
        During(duringState,
            When(completedEvent)
                .Then(context => onCompleted?.Invoke(context))
                .ThenAsync(context => RecordCompleted(context, task, ManualJobStepStatus.Succeeded))
                .Publish(context => Request<TNextRequest>(context.Saga))
                .ThenAsync(context => RecordDispatched(context, TaskOf<TNextRequest>()))
                .Schedule(HeartbeatScheduled,
                    context => new HeartbeatScheduled
                    {
                        CorrelationId = context.Saga.CorrelationId,
                        OrganizationId = context.Saga.OrganizationId
                    })
                .TransitionTo(nextState),
            When(faultedEvent)
                .ThenAsync(context => RecordCompleted(context, task, ManualJobStepStatus.Faulted))
                .Then(context =>
                {
                    _logger.LogInformation(
                        "Transfer: Module {ModuleId} stopped at {Task}",
                        context.Saga.ModuleId, task);

                })
                .ThenJobFailed().TransitionTo(Failed).Finalize(),

            // A runner that stops answering is a dead step, not a slow one.
            When(HeartbeatScheduled.Received).ThenHeartbeatScheduled(HeartbeatRequested),
            When(HeartbeatRequested.Completed).ThenHeartbeatCompleted(HeartbeatScheduled),
            When(HeartbeatRequested.Completed2)
                .ThenAsync(context => RecordCompleted(context, task, ManualJobStepStatus.Faulted,
                    "The runner stopped responding."))
                .Then(context =>
                {
                    _logger.LogWarning(
                        "Transfer: Module {ModuleId} lost its runner at {Task}",
                        context.Saga.ModuleId, task);

                }).ThenJobFailed().TransitionTo(Failed).Finalize()
        );

        // Waiting: the runner was gone when the step was sent, so it is re-sent on reconnect
        // rather than failed.
        During(nextWaitingState,
            When(RunnerReconnectedEvent)
                .Then(context =>
                {
                    context.Saga.WaitingSince = null;
                    _logger.LogInformation(
                        "Transfer: Module {ModuleId} runner reconnected, re-sending {Task}",
                        context.Saga.ModuleId, task);

                    context.Publish(Request<TNextRequest>(context.Saga));
                })
                .TransitionTo(nextState),
            Ignore(HeartbeatScheduled.Received),
            Ignore(HeartbeatRequested.Completed),
            Ignore(HeartbeatRequested.Completed2)
        );
    }

    /// <summary>
    /// The task a request type names, taken from the type so it cannot drift from the contract the
    /// runner answers on.
    /// </summary>
    private static string TaskOf<TRequest>() where TRequest : TransferStepRequestBase =>
        typeof(TRequest).Name.Replace("Transfer", "").Replace("Requested", "");

    private static async Task RecordDispatched<TMessage>(
        BehaviorContext<TransferMigrateSaga, TMessage> context, string task)
        where TMessage : class
    {
        var jobId = context.Saga.CorrelationId;

        var steps = PipeExtensions.GetPayload<IServiceProvider>(context)
            .GetRequiredService<ManualJobStepService>();

        await steps.Dispatched(
            jobId, context.Saga.OrganizationId, context.Saga.ModuleId, task,
            null, context.Saga.RunnerInstanceName);
    }

    private static async Task RecordCompleted<TMessage>(
        BehaviorContext<TransferMigrateSaga, TMessage> context,
        string task,
        ManualJobStepStatus status,
        string? errorHeader = null)
        where TMessage : class
    {
        var jobId = context.Saga.CorrelationId;

        var steps = PipeExtensions.GetPayload<IServiceProvider>(context)
            .GetRequiredService<ManualJobStepService>();

        var faulted = context.Message as TransferStepFaultedBase;

        await steps.Completed(
            jobId, context.Saga.OrganizationId, context.Saga.ModuleId, task, status,
            errorHeader: faulted?.ErrorMessage ?? errorHeader, error: faulted?.StackTrace);
    }

    /// <summary>
    /// The fields every transfer step request carries. Written once here rather than at each step,
    /// so a step cannot be dispatched with the wrong runner.
    /// </summary>
    private static TRequest Request<TRequest>(TransferMigrateSaga saga, Action<TRequest> fill)
        where TRequest : TransferStepRequestBase, new()
    {
        var request = Request<TRequest>(saga);
        fill(request);
        return request;
    }

    private static TRequest Request<TRequest>(TransferMigrateSaga saga)
        where TRequest : TransferStepRequestBase, new()
    {
        var request = new TRequest
        {
            CorrelationId = saga.CorrelationId,
            OrganizationId = saga.OrganizationId,
            ModuleId = saga.ModuleId,
            RunnerId = saga.RunnerId,
            RunnerInstanceName = saga.RunnerInstanceName ?? string.Empty,
            RootDirectory = saga.RootDirectory,
            Declared = JsonSerializer.Deserialize<ResolvedModule>(saga.DeclaredJson)!
        };

        // Only the checkout takes a ref, and it takes the transfer's rather than the Module's own.
        if (request is TransferGetModuleRequested getModule)
            getModule.SourceRevisionOverride = saga.ProveRef;

        return request;
    }


    /// <summary>
    /// The preamble: the same four steps any job runs before its real work, checked out at the ref
    /// this transfer runs against.
    /// </summary>
    private void Configure_Preamble()
    {
        CreateStep<TransferSelectRunnerInstanceCompleted, TransferSelectRunnerInstanceFaulted, TransferGetModuleRequested>(
            SelectRunnerInstancePending, SelectRunnerInstanceCompleted, SelectRunnerInstanceFaulted,
            "SelectRunnerInstance", GetModulePending, GetModuleWaitingForRunner,
            context => context.Saga.RunnerInstanceName = context.Message.RunnerInstanceName);

        During(GetModulePending,
            When(GetModuleCompleted)
                .Then(context => context.Saga.DefinitiveRevision = context.Message.DefinitiveRevision)
                .ThenAsync(context => RecordCompleted(context, "GetModule", ManualJobStepStatus.Succeeded))
                .Publish(context => Request<TransferInitRequested>(context.Saga))
                .ThenAsync(context => RecordDispatched(context, "Init"))
                .TransitionTo(InitPending),

            When(GetModuleFaulted)
                .ThenAsync(context => RecordCompleted(context, "GetModule", ManualJobStepStatus.Faulted))
                .ThenJobFailed().TransitionTo(Failed).Finalize(),

            When(HeartbeatScheduled.Received).ThenHeartbeatScheduled(HeartbeatRequested),
            When(HeartbeatRequested.Completed).ThenHeartbeatCompleted(HeartbeatScheduled),
            When(HeartbeatRequested.Completed2)
                .ThenAsync(context => RecordCompleted(context, "GetModule", ManualJobStepStatus.Faulted,
                    "The runner stopped responding."))
                .Then(LostRunner("GetModule")).ThenJobFailed().TransitionTo(Failed).Finalize()
        );

        During(InitWaitingForRunner,
            When(RunnerReconnectedEvent)
                .Then(context =>
                {
                    context.Saga.WaitingSince = null;
                    context.Publish(Request<TransferInitRequested>(context.Saga));
                })
                .TransitionTo(InitPending),
            Ignore(HeartbeatScheduled.Received),
            Ignore(HeartbeatRequested.Completed),
            Ignore(HeartbeatRequested.Completed2)
        );

        CreateStep<TransferInitCompleted, TransferInitFaulted, TransferValidateRequested>(
            InitPending, InitCompleted, InitFaulted, "Init", ValidatePending, ValidateWaitingForRunner);

        CreateStep<TransferValidateCompleted, TransferValidateFaulted, TransferPlanRequested>(
            ValidatePending, ValidateCompleted, ValidateFaulted, "Validate", PlanPending, PlanWaitingForRunner);

        // The plan ends the preamble rather than sending another request.
        During(PlanPending,
            // A transfer proves against a clean plan, so a dirty one ends this Module's run here.
            When(PlanCompleted, context => context.Message.TotalChangedCount != 0)
                .ThenAsync(context => RecordCompleted(context, "Plan", ManualJobStepStatus.Refused))
                .Then(context => _logger.LogInformation(
                    "Transfer: Module {ModuleId} plans {Count} changes; a transfer needs a clean plan",
                    context.Saga.ModuleId, context.Message.TotalChangedCount))
                .ThenJobFailed().TransitionTo(Failed).Finalize(),

            // Clean: straight on to this Module's own slices, which it runs without being told.
            // Filtered explicitly, because two handlers for one event both run otherwise.
            When(PlanCompleted, context => context.Message.TotalChangedCount == 0)
                .ThenAsync(context => RecordCompleted(context, "Plan", ManualJobStepStatus.Succeeded))
                .Publish(context => Request<TransferMigrateMapRequested>(context.Saga))
                .ThenAsync(context => RecordDispatched(context, "MigrateMap"))
                .TransitionTo(MigrateMapPending),
            When(PlanFaulted)
                .ThenAsync(context => RecordCompleted(context, "Plan", ManualJobStepStatus.Faulted))
                .ThenJobFailed().TransitionTo(Failed).Finalize(),

            When(HeartbeatScheduled.Received).ThenHeartbeatScheduled(HeartbeatRequested),
            When(HeartbeatRequested.Completed).ThenHeartbeatCompleted(HeartbeatScheduled),
            When(HeartbeatRequested.Completed2).Then(context =>
            {
                _logger.LogWarning(
                    "Transfer: Module {ModuleId} lost its runner at Plan",
                    context.Saga.ModuleId);

            }).ThenJobFailed().TransitionTo(Failed).Finalize()
        );

    }
}
