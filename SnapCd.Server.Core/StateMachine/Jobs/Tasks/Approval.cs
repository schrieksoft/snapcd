using MassTransit;
using SnapCd.Server.Core.Entities.Sagas.Base;
using SnapCd.Server.Core.Events.Jobs.Base;
using SnapCd.Server.Core.Events.Steps.Base;
using SnapCd.Server.Core.Events.System;

using SnapCd.Server.Core.StateMachine.Jobs.Activites;
using SnapCd.Server.Core.StateMachine.Jobs.Activites.Finalization;
using SnapCd.Server.Core.StateMachine.Jobs.Utils;
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
    // Approval events
    public Event<ApprovalReevaluationRequestedEvent> ApprovalModifiedEvent { get; } = null!;

    // Approval schedules
    public Schedule<TSaga, ApprovalTimeoutReceived> ApprovalTimeoutScheduled { get; } = null!;

    // Approval states
    public State WaitingForApproval { get; } = null!;
    public State Declined { get; } = null!;

    private void Configure_Approval()
    {
        // Approval events
        Event(() => ApprovalModifiedEvent, x => x.CorrelateById(y => y.Message.ModuleJobId));

        // Approval schedule
        Schedule(() => ApprovalTimeoutScheduled, saga => saga.ApprovalTimeoutScheduleTokenId,
            config => { config.Received = e => e.CorrelateById(context => context.Message.CorrelationId); }
        );

        During(WaitingForApproval,
            DealWithApprovalStatus(When(ApprovalModifiedEvent), false),
            Ignore(HeartbeatScheduled.Received),
            When(ApprovalTimeoutScheduled.Received)
                .Then(_ => { _logger.LogInformation("Approval request timed out"); })
                .Publish(context => new TResponseFailed
                {
                    ModuleId = context.Saga.ModuleId,
                    ModuleJobId = context.Saga.CorrelationId
                })
                .Activity(x => x.OfType<ApprovalTimeoutModuleJobActivity<TSaga, ApprovalTimeoutReceived>>())
                .TransitionTo(Cancelled)
                .Finalize(),
            Ignore(RunnerReconnectedEvent)
        );

        // Terminal state - ignore runner reconnection events
        During(Declined,
            Ignore(RunnerReconnectedEvent)
        );
    }
}
