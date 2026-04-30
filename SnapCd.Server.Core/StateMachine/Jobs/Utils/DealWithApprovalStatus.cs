using MassTransit;
using SnapCd.Server.Core.Entities.Sagas.Base;
using SnapCd.Server.Core.Events.Jobs.Base;
using SnapCd.Server.Core.Events.Steps;
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
    private EventActivityBinder<TSaga, TMessage> DealWithApprovalStatus<TMessage>(EventActivityBinder<TSaga, TMessage> x, bool transition)
        where TMessage : class
    {
        return x // Check if approved and set x.Saga.IsApproved
            .Activity(y => y.OfType<NeedsApprovalJobActivity<TSaga, TMessage>>())
            .IfElse(
                y => y.Saga.IsApproved,
                ///////////////////////////////////////////////////
                // Approved, continue to Apply
                ///////////////////////////////////////////////////
                y => y
                    .Then(_ => { _logger.LogInformation($"Approved, continuing to Apply"); })
                    .Activity(z => z.OfType<NotWaitingForApprovalModuleJobActivity<TSaga, TMessage>>())
                    // Use SendToRunnerActivity to target specific server instance
                    .Activity(z => z.OfType<SendToRunnerActivity<TSaga, TMessage, TApplyFromPlanRequested>>())
                    .IfElse(
                        context => context.Saga.PreviousStateBeforeWaiting != null,
                        // Runner is disconnected - transition to waiting state
                        whenTrue => whenTrue
                            .Then(context =>
                            {
                                context.Saga.WaitingSince = DateTime.UtcNow;
                                _logger.LogWarning(
                                    "Approval: Runner disconnected for job {CorrelationId}, entering waiting state",
                                    context.Saga.CorrelationId);
                            })
                            .TransitionTo(ApplyFromPlanWaitingForRunner),
                        // Runner is connected - continue normally
                        whenFalse => whenFalse
                            .Schedule(HeartbeatScheduled,
                                context => new HeartbeatScheduled { CorrelationId = context.Saga.CorrelationId, OrganizationId = context.Saga.OrganizationId })
                            .TransitionTo(ApplyFromPlanPending)
                    ),

                ///////////////////////////////////////////////////
                // Not Approved (yet), check if outright declined
                ///////////////////////////////////////////////////
                y => y
                    .IfElse(
                        z => z.Saga.IsDeclined,
                        ///////////////////////////////////////////////////
                        // Outright declined, finalize.
                        ///////////////////////////////////////////////////
                        z => z
                            .Then(_ => { _logger.LogInformation($"Declined, continuing to Finalize"); })
                            .Activity(z1 => z1.OfType<ApprovalDeclinedModuleJobActivity<TSaga, TMessage>>())
                            .TransitionTo(Declined)
                            .Finalize()
                        ,

                        ///////////////////////////////////////////////////
                        // Not Approved (yet) and Not Declined (yet). Keep waiting.
                        ///////////////////////////////////////////////////
                        z => z
                            .If(
                                // we set "transition = true" when building the "DealWithApprovalStatus" logic from the "PlanRequested.Pending" state in order to transition to "WaitingForApproval"
                                _ => transition,
                                z1 => z1
                                    .Activity(z2 => z2.OfType<WaitingForApprovalModuleJobActivity<TSaga, TMessage>>())
                                    .Then(_ => { _logger.LogInformation($"Not yet approved, transitioning to WaitForApproval"); })
                                    .If(z2 => z2.Saga.ApprovalTimeoutMinutes > 0, z2 => z2
                                        .Schedule(ApprovalTimeoutScheduled,
                                            context => new ApprovalTimeoutReceived { CorrelationId = context.Saga.CorrelationId, OrganizationId = context.Saga.OrganizationId },
                                            z3 => TimeSpan.FromMinutes(z3.Saga.ApprovalTimeoutMinutes ?? 0)))
                                    .TransitionTo(WaitingForApproval)
                            )
                    )
            );
    }
}