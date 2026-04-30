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
    // GetModule events
    public Event<GetModuleCompleted> GetModuleCompleted { get; } = null!;
    public Event<GetModuleCancelled> GetModuleCancelled { get; } = null!;
    public Event<GetModuleFaulted> GetModuleFaulted { get; } = null!;

    // GetModule states
    public State GetModulePending { get; } = null!;
    public State GetModuleWaitingForRunner { get; } = null!;

    private void Configure_GetModule()
    {
        // GetModule events
        Event(() => GetModuleCompleted, x => x.CorrelateById(y => y.Message.CorrelationId));
        Event(() => GetModuleCancelled, x => x.CorrelateById(y => y.Message.CorrelationId));
        Event(() => GetModuleFaulted, x => x.CorrelateById(y => y.Message.CorrelationId));


        CreateStep<GetModuleCompleted, GetModuleCancelled, GetModuleFaulted, InitRequested>(
            GetModuleWaitingForRunner,
            GetModulePending,
            GetModuleCompleted,
            GetModuleCancelled,
            GetModuleFaulted,
            InitWaitingForRunner,
            InitPending
            
        );
    }
}