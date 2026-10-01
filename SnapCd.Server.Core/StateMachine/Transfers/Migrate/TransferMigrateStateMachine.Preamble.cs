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
using SnapCd.Server.Core.StateMachine.Jobs.Activites;
using SnapCd.Server.Core.StateMachine.StateMigrations.Activities;
using SnapCd.Server.Core.Entities.Sagas;
using SnapCd.Server.Core.Enums;
using SnapCd.Server.Core.Events.Runners;
using SnapCd.Server.Core.Events.Steps;
using SnapCd.Server.Core.Events.Steps.Transfer;
using SnapCd.Server.Core.StateMachine.Jobs.Utils;
using Microsoft.Extensions.DependencyInjection;
using SnapCd.Server.Core.Services.Crud.Transfers;
using SnapCd.Server.Core.Services.ResolvedConfiguration.HelperClasses;

using SnapCd.Server.Core.StateMachine.Transfers.Migrate.Activities;
using SnapCd.Server.Core.Misc.Helpers;

namespace SnapCd.Server.Core.StateMachine.Transfers.Migrate;

public partial class TransferMigrateStateMachine
{
    /// <summary>
    /// Wires one preamble step: on completion send the next request, on fault end the job. Generic
    /// over the step's own types so the five steps differ only in which events they name, the way
    /// JobStateMachine is generic over Apply's and Destroy's.
    /// </summary>
    private void CreateStep<TCompleted, TCancelled, TFaulted, TNextRequest>(
        State duringState,
        Event<TCompleted> completedEvent,
        Event<TCancelled> cancelledEvent,
        Event<TFaulted> faultedEvent,
        string task,
        string nextTask,
        State nextState,
        State nextWaitingState,
        Action<BehaviorContext<TransferMigrateSaga, TCompleted>>? onCompleted = null)
        where TCompleted : TransferStepResponseBase
        where TCancelled : TransferStepCancelledBase
        where TFaulted : TransferStepFaultedBase
        where TNextRequest : TransferStepRequestBase, new()
    {
        During(duringState,
            SendOrWaitForRunner<TCompleted, TNextRequest>(
                When(completedEvent)
                    .Then(context => onCompleted?.Invoke(context))
                    .ThenAsync(context => RecordCompleted(context, task, StateMigrationStepStatus.Succeeded)),
                nextTask, nextState, nextWaitingState),
            When(cancelledEvent)
                .ThenAsync(context => RecordCompleted(context, task, StateMigrationStepStatus.Cancelled))
                .Then(context => _logger.LogInformation(
                    "Transfer: Module {ModuleId} cancelled at {Task}",
                    context.Saga.ModuleId, task))
                .ThenJobCancelled().TransitionTo(Failed).Finalize(),

            When(faultedEvent)
                .ThenAsync(context => RecordCompleted(context, task, StateMigrationStepStatus.Faulted))
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
                .ThenAsync(context => RecordCompleted(context, task, StateMigrationStepStatus.Faulted,
                    "The runner stopped responding."))
                .Then(context =>
                {
                    _logger.LogWarning(
                        "Transfer: Module {ModuleId} lost its runner at {Task}",
                        context.Saga.ModuleId, task);

                }).ThenJobFailed().TransitionTo(Failed).Finalize()
        );

        WaitForRunner<TCompleted, TNextRequest>(nextWaitingState, nextTask, nextState);
    }

    /// <summary>
    /// Hands the next step to a runner, or parks until one connects. A step is only parked when the
    /// send found no runner to take it, so nothing a runner already holds is sent a second time.
    /// </summary>
    private EventActivityBinder<TransferMigrateSaga, TMessage> SendOrWaitForRunner<TMessage, TNextRequest>(
        EventActivityBinder<TransferMigrateSaga, TMessage> binder,
        string nextTask,
        State nextState,
        State nextWaitingState)
        where TMessage : class
        where TNextRequest : TransferStepRequestBase, new() =>
        binder
            .Activity(x => x.OfType<RunnerConnectedActivity<TransferMigrateSaga, TMessage>>())
            .IfElse(
                context => context.Saga.PreviousStateBeforeWaiting != null,
                // The runner is away: park rather than dispatch into nothing.
                parked => parked
                    .Activity(x => x.OfType<WaitingForRunnerActivity<TransferMigrateSaga, TMessage>>())
                    .Then(context =>
                    {
                        context.Saga.WaitingSince = DateTime.UtcNow;
                        _logger.LogInformation(
                            "Transfer: Module {ModuleId} waits for its runner before {Task}",
                            context.Saga.ModuleId, nextTask);
                    })
                    .TransitionTo(nextWaitingState),
                ahead => ahead
                    .Activity(x => x.OfType<SendTransferStepToRunnerActivity<TMessage, TNextRequest>>())
                    .ThenAsync(context => RecordDispatched(context, nextTask))
                    .Schedule(HeartbeatScheduled,
                        context => new HeartbeatScheduled
                        {
                            CorrelationId = context.Saga.CorrelationId,
                            OrganizationId = context.Saga.OrganizationId
                        })
                    .TransitionTo(nextState));

    /// <summary>
    /// The same park-or-send, for a step dispatched by its own consume after a gate opened. The
    /// saga is already in the pending state and its heartbeat is running, so parking steps back out
    /// of both.
    /// </summary>
    private EventActivityBinder<TransferMigrateSaga, TMessage> SendOrWaitAfterGate<TMessage, TNextRequest>(
        EventActivityBinder<TransferMigrateSaga, TMessage> binder,
        string nextTask,
        State nextWaitingState)
        where TMessage : class
        where TNextRequest : TransferStepRequestBase, new() =>
        binder
            .Activity(x => x.OfType<RunnerConnectedActivity<TransferMigrateSaga, TMessage>>())
            .IfElse(
                context => context.Saga.PreviousStateBeforeWaiting != null,
                parked => parked
                    .Unschedule(HeartbeatScheduled)
                    .Activity(x => x.OfType<WaitingForRunnerActivity<TransferMigrateSaga, TMessage>>())
                    .Then(context =>
                    {
                        context.Saga.WaitingSince = DateTime.UtcNow;
                        _logger.LogInformation(
                            "Transfer: Module {ModuleId} waits for its runner before {Task}",
                            context.Saga.ModuleId, nextTask);
                    })
                    .TransitionTo(nextWaitingState),
                ahead => ahead
                    .Activity(x => x.OfType<SendTransferStepToRunnerActivity<TMessage, TNextRequest>>())
                    .ThenAsync(context => RecordDispatched(context, nextTask)));

    /// <summary>
    /// A step's waiting state: the request was never sent, so a reconnecting runner gets it then.
    /// </summary>
    private void WaitForRunner<TCompleted, TNextRequest>(
        State waitingState, string nextTask, State nextState)
        where TCompleted : class
        where TNextRequest : TransferStepRequestBase, new()
    {
        During(waitingState,
            // A runner that came back while the saga was parking is noticed on entry, rather than
            // the job waiting for a reconnect event that has already fired.
            When(waitingState.Enter)
                .Activity(x => x.OfType<CheckRunnerConnectionActivity<TransferMigrateSaga, TCompleted>>()),

            When(RunnerReconnectedEvent)
                .Then(context =>
                {
                    context.Saga.WaitingSince = null;
                    context.Saga.PreviousStateBeforeWaiting = null;
                    _logger.LogInformation(
                        "Transfer: Module {ModuleId} runner reconnected, re-sending {Task}",
                        context.Saga.ModuleId, nextTask);
                })
                .Activity(x => x.OfType<NotWaitingForRunnerActivity<TransferMigrateSaga, RunnerReconnectedEvent>>())
                .Activity(x => x.OfType<SendTransferStepToRunnerActivity<RunnerReconnectedEvent, TNextRequest>>())
                .ThenAsync(context => RecordDispatched(context, nextTask))
                .Schedule(HeartbeatScheduled,
                    context => new HeartbeatScheduled
                    {
                        CorrelationId = context.Saga.CorrelationId,
                        OrganizationId = context.Saga.OrganizationId
                    })
                .TransitionTo(nextState),
            Ignore(HeartbeatScheduled.Received),
            Ignore(HeartbeatRequested.Completed),
            Ignore(HeartbeatRequested.Completed2)
        );
    }

    private static async Task RecordDispatched<TMessage>(
        BehaviorContext<TransferMigrateSaga, TMessage> context, string task)
        where TMessage : class
    {
        var jobId = context.Saga.CorrelationId;

        var steps = PipeExtensions.GetPayload<IServiceProvider>(context)
            .GetRequiredService<StateMigrationStepService>();

        await steps.Dispatched(
            jobId, context.Saga.OrganizationId, context.Saga.ModuleId, task,
            null, context.Saga.RunnerInstanceName);
    }

    private static async Task RecordCompleted<TMessage>(
        BehaviorContext<TransferMigrateSaga, TMessage> context,
        string task,
        StateMigrationStepStatus status,
        string? errorHeader = null)
        where TMessage : class
    {
        var jobId = context.Saga.CorrelationId;

        var steps = PipeExtensions.GetPayload<IServiceProvider>(context)
            .GetRequiredService<StateMigrationStepService>();

        var faulted = context.Message as TransferStepFaultedBase;

        await steps.Completed(
            jobId, context.Saga.OrganizationId, context.Saga.ModuleId, task, status,
            errorHeader: errorHeader ?? ErrorText.Header(task, status),
            error: ErrorText.Detail(faulted?.ErrorMessage, faulted?.StackTrace));
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
        CreateStep<TransferSelectRunnerInstanceCompleted, TransferSelectRunnerInstanceCancelled,
            TransferSelectRunnerInstanceFaulted, TransferGetModuleRequested>(
            TransferSelectRunnerInstancePending, SelectRunnerInstanceCompleted,
            SelectRunnerInstanceCancelled, SelectRunnerInstanceFaulted,
            "SelectRunnerInstance", "GetModule", TransferGetModulePending, TransferGetModuleWaitingForRunner,
            context => context.Saga.RunnerInstanceName = context.Message.RunnerInstanceName);

        CreateStep<TransferGetModuleCompleted, TransferGetModuleCancelled,
            TransferGetModuleFaulted, TransferInitRequested>(
            TransferGetModulePending, GetModuleCompleted, GetModuleCancelled, GetModuleFaulted,
            "GetModule", "Init", TransferInitPending, TransferInitWaitingForRunner,
            context => context.Saga.DefinitiveRevision = context.Message.DefinitiveRevision);

        CreateStep<TransferInitCompleted, TransferInitCancelled, TransferInitFaulted, TransferValidateRequested>(
            TransferInitPending, InitCompleted, InitCancelled, InitFaulted,
            "Init", "Validate", TransferValidatePending, TransferValidateWaitingForRunner);

        // The prove is what gates a transfer: it plans against the moved state, which a plan here
        // cannot see.
        During(TransferValidatePending,
            SendOrWaitForRunner<TransferValidateCompleted, AnalyseTransferRefactorMapRequested>(
                When(ValidateCompleted)
                    .ThenAsync(context => RecordCompleted(context, "Validate", StateMigrationStepStatus.Succeeded)),
                "AnalyseTransferRefactorMap", AnalyseTransferRefactorMapPending, AnalyseTransferRefactorMapWaitingForRunner),

            When(ValidateCancelled)
                .ThenAsync(context => RecordCompleted(context, "Validate", StateMigrationStepStatus.Cancelled))
                .ThenJobCancelled().TransitionTo(Failed).Finalize(),

            When(ValidateFaulted)
                .ThenAsync(context => RecordCompleted(context, "Validate", StateMigrationStepStatus.Faulted))
                .ThenJobFailed().TransitionTo(Failed).Finalize(),

            When(HeartbeatScheduled.Received).ThenHeartbeatScheduled(HeartbeatRequested),
            When(HeartbeatRequested.Completed).ThenHeartbeatCompleted(HeartbeatScheduled),
            When(HeartbeatRequested.Completed2).Then(context =>
                    _logger.LogWarning(
                        "Transfer: Module {ModuleId} lost its runner at Validate",
                        context.Saga.ModuleId))
                .ThenJobFailed().TransitionTo(Failed).Finalize()
        );

        WaitForRunner<TransferValidateCompleted, AnalyseTransferRefactorMapRequested>(
            AnalyseTransferRefactorMapWaitingForRunner, "AnalyseTransferRefactorMap", AnalyseTransferRefactorMapPending);
    }
}
