using MassTransit;
using SnapCd.Server.Core.Entities.Sagas.Base;
using SnapCd.Server.Core.Events.Jobs.Base;
using SnapCd.Server.Core.Events.Steps;
using SnapCd.Server.Core.Events.Steps.Base;

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
    // Variables events
    public Event<VariablesCompleted> VariablesCompleted { get; } = null!;
    public Event<VariablesCancelled> VariablesCancelled { get; } = null!;
    public Event<VariablesFaulted> VariablesFaulted { get; } = null!;

    // Variables states
    public State VariablesPending { get; } = null!;
    public State VariablesWaitingForRunner { get; } = null!;

    private void Configure_Variables()
    {
        // Variables events
        Event(() => VariablesCompleted, x => x.CorrelateById(y => y.Message.CorrelationId));
        Event(() => VariablesCancelled, x => x.CorrelateById(y => y.Message.CorrelationId));
        Event(() => VariablesFaulted, x => x.CorrelateById(y => y.Message.CorrelationId));

        CreateStep<VariablesCompleted, VariablesCancelled, VariablesFaulted, TPlanRequested>(
            VariablesWaitingForRunner,
            VariablesPending,
            VariablesCompleted,
            VariablesCancelled,
            VariablesFaulted,
            PlanWaitingForRunner,
            PlanPending
        );
    }
}