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
using SnapCd.Server.Core.Events.Transfers;
using Microsoft.Extensions.DependencyInjection;
using SnapCd.Server.Core.Services.Crud.Transfers;
using SnapCd.Server.Core.Services.ResolvedConfiguration.HelperClasses;

namespace SnapCd.Server.Core.StateMachine.Transfers;

public partial class TransferParticipantStateMachine
{
    /// <summary>
    /// Wires one preamble step: on completion send the next request, on fault tell the coordinator
    /// where it stopped. Generic over the step's own types so the five steps differ only in which
    /// events they name, the way JobStateMachine is generic over Apply's and Destroy's.
    /// </summary>
    private void CreateStep<TCompleted, TFaulted, TNextRequest>(
        State duringState,
        Event<TCompleted> completedEvent,
        Event<TFaulted> faultedEvent,
        string task,
        State nextState,
        State nextWaitingState,
        Action<BehaviorContext<TransferParticipantSaga, TCompleted>>? onCompleted = null)
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
                        "Transfer {TransferId}: {Role} stopped at {Task}",
                        context.Saga.TransferId, context.Saga.Role, task);

                    context.Publish(Stopped(context.Saga, task, context.Message));
                })
                .TransitionTo(StoppedState),

            // A runner that stops answering is a dead step, not a slow one.
            When(HeartbeatScheduled.Received).ThenHeartbeatScheduled(HeartbeatRequested),
            When(HeartbeatRequested.Completed).ThenHeartbeatCompleted(HeartbeatScheduled),
            When(HeartbeatRequested.Completed2).Then(context =>
            {
                _logger.LogWarning(
                    "Transfer {TransferId}: {Role} lost its runner at {Task}",
                    context.Saga.TransferId, context.Saga.Role, task);

                context.Publish(StoppedAt(context.Saga, task, "The runner stopped responding."));
            }).TransitionTo(StoppedState),

            When(StopRequested).Then(LogStop(task)).TransitionTo(StoppedState)
        );

        // Waiting: the runner was gone when the step was sent, so it is re-sent on reconnect
        // rather than failed.
        During(nextWaitingState,
            When(RunnerReconnectedEvent)
                .Then(context =>
                {
                    context.Saga.WaitingSince = null;
                    _logger.LogInformation(
                        "Transfer {TransferId}: {Role} runner reconnected, re-sending {Task}",
                        context.Saga.TransferId, context.Saga.Role, task);

                    context.Publish(Request<TNextRequest>(context.Saga));
                })
                .TransitionTo(nextState),
            When(StopRequested).Then(LogStop(task)).TransitionTo(StoppedState),
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
        BehaviorContext<TransferParticipantSaga, TMessage> context, string task)
        where TMessage : class
    {
        if (context.Saga.CurrentJobId is not { } jobId) return;

        var steps = PipeExtensions.GetPayload<IServiceProvider>(context)
            .GetRequiredService<ManualJobStepService>();

        await steps.Dispatched(
            jobId, context.Saga.OrganizationId, context.Saga.ModuleId, task,
            context.Saga.TransferId, context.Saga.RunnerInstanceName, context.Saga.InputKey);
    }

    private static async Task RecordCompleted<TMessage>(
        BehaviorContext<TransferParticipantSaga, TMessage> context, string task, ManualJobStepStatus status)
        where TMessage : class
    {
        if (context.Saga.CurrentJobId is not { } jobId) return;

        var steps = PipeExtensions.GetPayload<IServiceProvider>(context)
            .GetRequiredService<ManualJobStepService>();

        var faulted = context.Message as TransferStepFaultedBase;

        await steps.Completed(
            jobId, context.Saga.OrganizationId, context.Saga.ModuleId, task, status,
            errorHeader: faulted?.ErrorMessage, error: faulted?.StackTrace);
    }

    /// <summary>This Module's map slice, carrying the fragment when it is the receiver.</summary>
    private static TransferMigrateMapRequested MapRequest(TransferParticipantSaga saga)
    {
        var request = Request<TransferMigrateMapRequested>(saga);
        request.Map = saga.Map!;
        request.FragmentState = saga.FragmentState;
        request.FragmentMeta = saga.FragmentMeta;
        return request;
    }

    /// <summary>This Module's prove slice, with whatever values it consumes from the other.</summary>
    private static TransferMigrateProveRequested ProveRequest(TransferParticipantSaga saga)
    {
        var request = Request<TransferMigrateProveRequested>(saga);
        request.Map = saga.Map!;
        request.Outputs = saga.OutputsJson == null
            ? new Dictionary<string, string>()
            : JsonSerializer.Deserialize<Dictionary<string, string>>(saga.OutputsJson)!;
        return request;
    }

    private Action<BehaviorContext<TransferParticipantSaga, TransferParticipantStopRequested>> LogStop(string task) =>
        context =>
        {
            _logger.LogInformation(
                "Transfer {TransferId}: {Role} told to stop at {Task}",
                context.Saga.TransferId, context.Saga.Role, task);

            context.Publish(StoppedAt(context.Saga, task, "Stopped by the transfer."));
        };

    private static TransferParticipantStopped StoppedAt(
        TransferParticipantSaga saga, string task, string reason) =>
        new()
        {
            CorrelationId = saga.CorrelationId,
            OrganizationId = saga.OrganizationId,
            TransferId = saga.TransferId,
            ModuleId = saga.ModuleId,
            Role = saga.Role,
            ProveRound = saga.ProveRound,
            Task = task,
            Status = ManualJobStepStatus.Faulted,
            ErrorHeader = reason
        };

    /// <summary>
    /// The fields every transfer step request carries. Written once here rather than at each step,
    /// so a step cannot be dispatched with the wrong participant's runner.
    /// </summary>
    private static TRequest Request<TRequest>(TransferParticipantSaga saga)
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

        // Only the checkout takes a ref, and it takes the one this participant consented to prove
        // rather than the Module's own.
        if (request is TransferGetModuleRequested getModule)
            getModule.SourceRevisionOverride = saga.ProveRef;

        return request;
    }

    private static TransferParticipantStopped Stopped(
        TransferParticipantSaga saga, string task, TransferStepFaultedBase faulted) =>
        new()
        {
            CorrelationId = saga.CorrelationId,
            OrganizationId = saga.OrganizationId,
            TransferId = saga.TransferId,
            ModuleId = saga.ModuleId,
            Role = saga.Role,
            ProveRound = saga.ProveRound,
            Task = task,
            Status = ManualJobStepStatus.Faulted,
            ErrorHeader = faulted.ErrorMessage,
            Error = faulted.StackTrace
        };

    /// <summary>
    /// The preamble: the same four steps any job runs before its real work, addressed to this one
    /// Module and checked out at the ref it consented to prove. Linear, because one Module's
    /// preamble depends on nothing but itself; only the coordinator waits for both.
    /// </summary>
    private void Configure_Preamble()
    {
        CreateStep<TransferSelectRunnerInstanceCompleted, TransferSelectRunnerInstanceFaulted, TransferGetModuleRequested>(
            SelectRunnerInstancePending, SelectRunnerInstanceCompleted, SelectRunnerInstanceFaulted,
            "SelectRunnerInstance", GetModulePending, GetModuleWaitingForRunner,
            context => context.Saga.RunnerInstanceName = context.Message.RunnerInstanceName);

        CreateStep<TransferGetModuleCompleted, TransferGetModuleFaulted, TransferInitRequested>(
            GetModulePending, GetModuleCompleted, GetModuleFaulted,
            "GetModule", InitPending, InitWaitingForRunner,
            context => context.Saga.DefinitiveRevision = context.Message.DefinitiveRevision);

        CreateStep<TransferInitCompleted, TransferInitFaulted, TransferValidateRequested>(
            InitPending, InitCompleted, InitFaulted, "Init", ValidatePending, ValidateWaitingForRunner);

        CreateStep<TransferValidateCompleted, TransferValidateFaulted, TransferPlanRequested>(
            ValidatePending, ValidateCompleted, ValidateFaulted, "Validate", PlanPending, PlanWaitingForRunner);

        // The plan ends the preamble rather than sending another request: whether a dirty plan ends
        // the round is the coordinator's call, because it depends on what the other participant did.
        During(PlanPending,
            // A transfer proves against a clean plan, so a dirty one ends this Module's run here.
            When(PlanCompleted, context => context.Message.TotalChangedCount != 0)
                .ThenAsync(context => RecordCompleted(context, "Plan", ManualJobStepStatus.Refused))
                .Then(context => context.Publish(StoppedAt(context.Saga, "Plan",
                    $"The plan plans {context.Message.TotalChangedCount} changes; a transfer proves against a clean plan.")))
                .TransitionTo(StoppedState),

            // Clean: straight on to this Module's own slices, which it runs without being told.
            // Filtered explicitly, because two handlers for one event both run otherwise.
            When(PlanCompleted, context => context.Message.TotalChangedCount == 0)
                .ThenAsync(context => RecordCompleted(context, "Plan", ManualJobStepStatus.Succeeded))
                .Publish(context => MapRequest(context.Saga))
                .ThenAsync(context => RecordDispatched(context, "MigrateMap"))
                .TransitionTo(MigrateMapPending),
            When(PlanFaulted)
                .ThenAsync(context => RecordCompleted(context, "Plan", ManualJobStepStatus.Faulted))
                .Then(context => context.Publish(Stopped(context.Saga, "Plan", context.Message)))
                .TransitionTo(StoppedState),

            When(HeartbeatScheduled.Received).ThenHeartbeatScheduled(HeartbeatRequested),
            When(HeartbeatRequested.Completed).ThenHeartbeatCompleted(HeartbeatScheduled),
            When(HeartbeatRequested.Completed2).Then(context =>
            {
                _logger.LogWarning(
                    "Transfer {TransferId}: {Role} lost its runner at Plan",
                    context.Saga.TransferId, context.Saga.Role);

                context.Publish(StoppedAt(context.Saga, "Plan", "The runner stopped responding."));
            }).TransitionTo(StoppedState),

            When(StopRequested).Then(LogStop("Plan")).TransitionTo(StoppedState)
        );

    }
}
