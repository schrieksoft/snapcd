// SPDX-License-Identifier: LicenseRef-Snap-CD-Source-Available-1.1
// Copyright (c) 2026 Karl Schriek / Schrieksoft.
// No license is granted to use this file, in whole or in part, (a) as training, fine-tuning, retrieval, or
// embedding data for any machine-learning model, or (b) as input to any machine-learning model, agent, or automated
// system for the purpose of producing a derivative work or reimplementation that is not otherwise permitted by the
// Snap CD Source-Available License (including any Competing Product as defined therein). Contact info@snapcd.io
// for terms covering either use.

using MassTransit;
using Microsoft.Extensions.DependencyInjection;
using SnapCd.Server.Core.Entities.Sagas.Base;
using SnapCd.Server.Core.Enums;
using SnapCd.Server.Core.Events.Jobs.Base;
using SnapCd.Server.Core.Events.Jobs.Module;
using SnapCd.Server.Core.Events.Steps;
using SnapCd.Server.Core.Events.Steps.Base;
using SnapCd.Server.Core.Repositories.Organizations.Nonsecured;

using SnapCd.Server.Core.StateMachine.Jobs.Activites;
using SnapCd.Server.Core.StateMachine.Jobs.Activites.Finalization;
using SnapCd.Server.Core.StateMachine.Jobs.Utils;
using SnapCd.Contracts;
namespace SnapCd.Server.Core.StateMachine.Jobs;

public partial class JobStateMachine<
    TSaga,
    TRequest,
    TResponseFailed,
    TResponseCompleted,
    TResponseCancelled,
    TPlanRequested,
    TPlanCompleted,
    TPlanCancelled,
    TApplyFromPlanRequested,
    TApplyFromPlanCompleted,
    TApplyFromPlanCancelled>
    where TSaga : JobSagaBase
    where TRequest : ModuleJobEventBase
    where TResponseFailed : ModuleJobEventCompletedBase, new()
    where TResponseCompleted : ModuleJobEventCompletedBase, new()
    where TResponseCancelled : ModuleJobEventCompletedBase, new()
    where TPlanRequested : StepRequestBase, new()
    where TPlanCompleted : PlanCompletedBase
    where TPlanCancelled : StepResponseBase
    where TApplyFromPlanRequested : StepRequestBase, new()
    where TApplyFromPlanCompleted : ApplyResponseBase
    where TApplyFromPlanCancelled : StepResponseBase
{
    // Cancel requests
    public Request<TSaga, CancelKillRequested, DummyCancelKillCompleted> CancelKillRequested { get; } = null!;
    public Event<CancelKillCompleted> CancelKillCompleted { get; } = null!;

    public Request<TSaga, CancelGracefulRequested, DummyCancelGracefulCompleted> CancelGracefulRequested { get; } = null!;
    public Event<CancelGracefulCompleted> CancelGracefulCompleted { get; } = null!;

    // Cancel events
    public Event<CancelModuleRequested> CancelModuleRequested { get; } = null!;

    // Cancel states
    public State CancellingImmediateKill { get; } = null!;
    public State CancellingImmediateGraceful { get; } = null!;
    public State CancellingAfterCurrent { get; } = null!;
    public State Cancelled { get; } = null!;

    private void Configure_Cancel()
    {
        // Cancel module requested - with fallback for missing saga
        Event(() => CancelModuleRequested, x => x
            .CorrelateById(m => m.Message.CorrelationId)
            .OnMissingInstance(m => m.ExecuteAsync(async context =>
            {
                var serviceProvider = PipeExtensions.GetPayload<IServiceProvider>(context);
                var repository = serviceProvider.GetRequiredService<ModuleJobRepository>();
                var publishEndpoint = serviceProvider.GetRequiredService<IPublishEndpoint>();

                var actualStateHeadline = typeof(TSaga).Name switch
                {
                    "ApplyJobSaga" => ActualStateHeadline.ApplyCancelled,
                    "DestroyJobSaga" => ActualStateHeadline.DestroyCancelled,
                    _ => (ActualStateHeadline?)null
                };

                _logger.LogWarning("Saga missing for {EventType} on job {JobId}, finalizing directly",
                    nameof(CancelModuleRequested), context.Message.CorrelationId);

                var moduleJob = await repository.Get(context.Message.CorrelationId, context.Message.OrganizationId);

                if (moduleJob != null)
                {
                    var cancelled = new TResponseCancelled
                    {
                        ModuleId = moduleJob.ModuleId,
                        ModuleJobId = context.Message.CorrelationId,
                        OrganizationId = context.Message.OrganizationId,
                        CancellationReason = CancellationReason.UserRequested
                    };
                    await publishEndpoint.Publish(cancelled);
                }

                await repository.Finalize(
                    context.Message.CorrelationId,
                    context.Message.OrganizationId,
                    ExecutionStatus.Cancelled,
                    nameof(CancelModuleRequested),
                    DateTimeOffset.UtcNow,
                    null,
                    actualStateHeadline);
            })));

        // Cancel requests
        Request(() => CancelKillRequested, x => x.KillCancellationRequestId, o => { o.Timeout = TimeSpan.FromSeconds(90); });
        Event(() => CancelKillCompleted, x => x
            .CorrelateById(y => y.Message.CorrelationId)
            .OnMissingInstance(m => m.ExecuteAsync(async context =>
            {
                var serviceProvider = PipeExtensions.GetPayload<IServiceProvider>(context);
                var repository = serviceProvider.GetRequiredService<ModuleJobRepository>();
                var publishEndpoint = serviceProvider.GetRequiredService<IPublishEndpoint>();

                var actualStateHeadline = typeof(TSaga).Name switch
                {
                    "ApplyJobSaga" => ActualStateHeadline.ApplyCancelled,
                    "DestroyJobSaga" => ActualStateHeadline.DestroyCancelled,
                    _ => (ActualStateHeadline?)null
                };

                _logger.LogWarning("Saga missing for {EventType} on job {JobId}, finalizing directly",
                    nameof(CancelKillCompleted), context.Message.CorrelationId);

                var moduleJob = await repository.Get(context.Message.CorrelationId, context.Message.OrganizationId);

                if (moduleJob != null)
                {
                    var cancelled = new TResponseCancelled
                    {
                        ModuleId = moduleJob.ModuleId,
                        ModuleJobId = context.Message.CorrelationId,
                        OrganizationId = context.Message.OrganizationId,
                        CancellationReason = CancellationReason.UserRequested
                    };
                    await publishEndpoint.Publish(cancelled);
                }

                await repository.Finalize(
                    context.Message.CorrelationId,
                    context.Message.OrganizationId,
                    ExecutionStatus.Cancelled,
                    nameof(CancelKillCompleted),
                    DateTimeOffset.UtcNow,
                    null,
                    actualStateHeadline);
            })));

        Request(() => CancelGracefulRequested, x => x.GracefulCancellationRequestId, o => { o.Timeout = TimeSpan.FromSeconds(90); });
        Event(() => CancelGracefulCompleted, x => x
            .CorrelateById(y => y.Message.CorrelationId)
            .OnMissingInstance(m => m.ExecuteAsync(async context =>
            {
                var serviceProvider = PipeExtensions.GetPayload<IServiceProvider>(context);
                var repository = serviceProvider.GetRequiredService<ModuleJobRepository>();
                var publishEndpoint = serviceProvider.GetRequiredService<IPublishEndpoint>();

                var actualStateHeadline = typeof(TSaga).Name switch
                {
                    "ApplyJobSaga" => ActualStateHeadline.ApplyCancelled,
                    "DestroyJobSaga" => ActualStateHeadline.DestroyCancelled,
                    _ => (ActualStateHeadline?)null
                };

                _logger.LogWarning("Saga missing for {EventType} on job {JobId}, finalizing directly",
                    nameof(CancelGracefulCompleted), context.Message.CorrelationId);

                var moduleJob = await repository.Get(context.Message.CorrelationId, context.Message.OrganizationId);

                if (moduleJob != null)
                {
                    var cancelled = new TResponseCancelled
                    {
                        ModuleId = moduleJob.ModuleId,
                        ModuleJobId = context.Message.CorrelationId,
                        OrganizationId = context.Message.OrganizationId,
                        CancellationReason = CancellationReason.UserRequested
                    };
                    await publishEndpoint.Publish(cancelled);
                }

                await repository.Finalize(
                    context.Message.CorrelationId,
                    context.Message.OrganizationId,
                    ExecutionStatus.Cancelled,
                    nameof(CancelGracefulCompleted),
                    DateTimeOffset.UtcNow,
                    null,
                    actualStateHeadline);
            })));

        During(CancellingImmediateKill,
            When(SelectRunnerInstanceCompleted)
                .Cancel(_logger, Cancelled, new TResponseCancelled()),
            When(GetDefinitiveRevisionCompleted)
                .Cancel(_logger, Cancelled, new TResponseCancelled()),
            When(GetModuleCompleted)
                .Cancel(_logger, Cancelled, new TResponseCancelled()),
            When(InitCompleted)
                .Cancel(_logger, Cancelled, new TResponseCancelled()),
            When(ValidateCompleted)
                .Cancel(_logger, Cancelled, new TResponseCancelled()),
            When(VariablesCompleted)
                .Cancel(_logger, Cancelled, new TResponseCancelled()),
            When(PlanCompleted)
                .Cancel(_logger, Cancelled, new TResponseCancelled()),
            When(ApplyFromPlanCompleted)
                .Cancel(_logger, Cancelled, new TResponseCancelled()),
            When(OutputCompleted)
                .Cancel(_logger, Cancelled, new TResponseCancelled()),
            When(CancelKillCompleted)
                .Cancel(_logger, Cancelled, new TResponseCancelled()),
            When(CancelKillRequested.TimeoutExpired)
                .ThenCancelTimeout<TSaga, TResponseCancelled, CancelKillRequested>(_logger, Cancelled),
            Ignore(HeartbeatScheduled.Received),
            Ignore(HeartbeatRequested.Completed),
            Ignore(HeartbeatRequested.Completed2),
            Ignore(RunnerReconnectedEvent)
        );

        During(CancellingImmediateGraceful,
            When(SelectRunnerInstanceCompleted)
                .Cancel(_logger, Cancelled, new TResponseCancelled()),
            When(GetDefinitiveRevisionCompleted)
                .Cancel(_logger, Cancelled, new TResponseCancelled()),
            When(GetModuleCompleted)
                .Cancel(_logger, Cancelled, new TResponseCancelled()),
            When(InitCompleted)
                .Cancel(_logger, Cancelled, new TResponseCancelled()),
            When(ValidateCompleted)
                .Cancel(_logger, Cancelled, new TResponseCancelled()),
            When(VariablesCompleted)
                .Cancel(_logger, Cancelled, new TResponseCancelled()),
            When(PlanCompleted)
                .Cancel(_logger, Cancelled, new TResponseCancelled()),
            When(ApplyFromPlanCompleted)
                .Cancel(_logger, Cancelled, new TResponseCancelled()),
            When(OutputCompleted)
                .Cancel(_logger, Cancelled, new TResponseCancelled()),
            When(CancelModuleRequested)
                .IfCancelKill<TSaga, TResponseCancelled>(_logger, CancelKillRequested, CancellingImmediateKill, Cancelled),
            When(CancelGracefulCompleted)
                .Cancel(_logger, Cancelled, new TResponseCancelled()),
            When(CancelGracefulRequested.TimeoutExpired)
                .ThenCancelTimeout<TSaga, TResponseCancelled, CancelGracefulRequested>(_logger, Cancelled),
            Ignore(HeartbeatScheduled.Received),
            Ignore(HeartbeatRequested.Completed),
            Ignore(HeartbeatRequested.Completed2),
            Ignore(RunnerReconnectedEvent)
        );

        During(CancellingAfterCurrent,
            When(SelectRunnerInstanceCompleted)
                .Cancel(_logger, Cancelled, new TResponseCancelled()),
            When(GetDefinitiveRevisionCompleted)
                .Cancel(_logger, Cancelled, new TResponseCancelled()),
            When(GetModuleCompleted)
                .Cancel(_logger, Cancelled, new TResponseCancelled()),
            When(InitCompleted)
                .Cancel(_logger, Cancelled, new TResponseCancelled()),
            When(ValidateCompleted)
                .Cancel(_logger, Cancelled, new TResponseCancelled()),
            When(VariablesCompleted)
                .Cancel(_logger, Cancelled, new TResponseCancelled()),
            When(PlanCompleted)
                .Cancel(_logger, Cancelled, new TResponseCancelled()),
            When(ApplyFromPlanCompleted)
                .Cancel(_logger, Cancelled, new TResponseCancelled()),
            When(OutputCompleted)
                .Cancel(_logger, Cancelled, new TResponseCancelled()),
            When(CancelModuleRequested)
                .IfCancelKill<TSaga, TResponseCancelled>(_logger, CancelKillRequested, CancellingImmediateKill, Cancelled)
                .IfCancelGraceful<TSaga, TResponseCancelled>(_logger, CancelGracefulRequested, CancellingImmediateGraceful, Cancelled),
            Ignore(HeartbeatScheduled.Received),
            Ignore(HeartbeatRequested.Completed),
            Ignore(HeartbeatRequested.Completed2),
            Ignore(RunnerReconnectedEvent)
        );

        // Terminal state - ignore runner reconnection events
        During(Cancelled,
            Ignore(RunnerReconnectedEvent)
        );
    }
}