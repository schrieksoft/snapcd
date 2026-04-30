using System.Text.Json;
using MassTransit;
using SnapCd.Server.Core.Entities.Sagas.Base;
using SnapCd.Server.Core.Enums;
using SnapCd.Server.Core.Events.Jobs.Base;
using SnapCd.Server.Core.Events.Runners;
using SnapCd.Server.Core.Events.Steps;
using SnapCd.Server.Core.Events.Steps.Base;
using SnapCd.Server.Core.Repositories.Organizations.Nonsecured;
using SnapCd.Server.Core.Services.ResolvedConfiguration.HelperClasses;

namespace SnapCd.Server.Core.StateMachine.Jobs;

// Delegate type for generic CreateStep method
public delegate void CreateStepDelegate<TCompleted, TCancelled, TFaulted, TNextEvent>(
    Event<TCompleted> completedEvent,
    Event<TCancelled> cancelledEvent,
    Event<TFaulted> faultedEvent,
    State duringState,
    State nextState,
    State waitingState
)
    where TNextEvent : StepRequestBase, new()
    where TCompleted : StepResponseBase
    where TCancelled : StepResponseBase
    where TFaulted : StepResponseBase;

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
    : MassTransitStateMachine<TSaga>
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
    private readonly ILogger<JobStateMachine<
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
        TApplyFromPlanCancelled
    >> _logger;

    public Event<TRequest> ModuleRequest { get; } = null!;
    public Event<RunnerReconnectedEvent> RunnerReconnectedEvent { get; } = null!;
    public Event<CleanupOrphanedJobRequested> CleanupOrphanedJobRequested { get; } = null!;
    public Request<TSaga, HeartbeatRequested, HeartbeatCompleted, HeartbeatFailed> HeartbeatRequested { get; } = null!;
    public Schedule<TSaga, HeartbeatScheduled> HeartbeatScheduled { get; } = null!;

    public State Completed { get; } = null!;
    public State Failed { get; } = null!;


    public JobStateMachine(
        ILogger<JobStateMachine<
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
            TApplyFromPlanCancelled
        >> logger
    )
    {
        _logger = logger;

        InstanceState(x => x.CurrentState);

        SetCompletedWhenFinalized();

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

        Configure_Cancel();

        Initially(
            When(ModuleRequest)
                .Then(context =>
                {
                    context.Saga.CorrelationId = context.Message.CorrelationId;
                    context.Saga.ModuleId = context.Message.Declared.ModuleId;
                    context.Saga.OrganizationId = context.Message.Declared.OrganizationId;
                    context.Saga.DeclaredJson = JsonSerializer.Serialize(context.Message.Declared);
                    context.Saga.RunnerId = context.Message.Declared.RunnerId;
                    context.Saga.RunnerName = context.Message.Declared.RunnerName;
                    context.Saga.RunnerInstanceName = context.Message.Declared.RunnerInstanceName;
                    context.Saga.ApprovalTimeoutMinutes = context.Message.Declared.ApprovalTimeoutMinutes;
                })
                .Publish(context => new SelectRunnerInstanceRequested
                {
                    RunnerId = context.Saga.RunnerId,
                    CorrelationId = context.Saga.CorrelationId,
                    OrganizationId = context.Saga.OrganizationId,
                    Declared = JsonSerializer.Deserialize<ResolvedModule>(context.Saga.DeclaredJson)!
                })
                .TransitionTo(SelectRunnerInstancePending)
        );

        Configure_SelectRunnerInstance();
        Configure_GetDefinitiveRevision();
        Configure_GetModule();
        Configure_Init();
        Configure_Validate();
        Configure_Variables();
        Configure_Plan();
        Configure_ApplyFromPlan();
        Configure_Output();
        Configure_Approval();

        // Terminal states - ignore runner reconnection events
        During(Completed,
            Ignore(RunnerReconnectedEvent)
        );

        During(Failed,
            Ignore(RunnerReconnectedEvent)
        );

        Event(() => CleanupOrphanedJobRequested, x => x
            .CorrelateById(y => y.Message.CorrelationId)
            .OnMissingInstance(m => m.ExecuteAsync(async context =>
            {
                var serviceProvider = context.GetPayload<IServiceProvider>();
                var repository = serviceProvider.GetRequiredService<ModuleJobRepository>();
                var publishEndpoint = serviceProvider.GetRequiredService<IPublishEndpoint>();

                var actualStateHeadline = typeof(TSaga).Name switch
                {
                    "ApplyJobSaga" => ActualStateHeadline.ApplyOrphaned,
                    "DestroyJobSaga" => ActualStateHeadline.DestroyOrphaned,
                    _ => (ActualStateHeadline?)null
                };

                _logger.LogWarning("Saga missing for {EventType} on job {JobId}, finalizing directly",
                    nameof(CleanupOrphanedJobRequested), context.Message.CorrelationId);

                var moduleJob = await repository.Get(context.Message.CorrelationId, context.Message.OrganizationId);

                if (moduleJob != null)
                {
                    var cancelled = new TResponseFailed()
                    {
                        ModuleId = moduleJob.ModuleId,
                        ModuleJobId = context.Message.CorrelationId,
                        OrganizationId = context.Message.OrganizationId
                    };
                    await publishEndpoint.Publish(cancelled);
                }

                await repository.Finalize(
                    context.Message.CorrelationId,
                    context.Message.OrganizationId,
                    ExecutionStatus.Orphaned,
                    nameof(CancelKillCompleted),
                    DateTimeOffset.UtcNow,
                    null,
                    actualStateHeadline);
            })));
    }
}