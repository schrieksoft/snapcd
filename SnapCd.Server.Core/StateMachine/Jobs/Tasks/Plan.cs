using MassTransit;
using SnapCd.Server.Core.Entities.Sagas.Base;
using SnapCd.Server.Core.Events.Jobs.Base;
using SnapCd.Server.Core.Events.Runners;
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
    // Plan events
    public Event<TPlanRequested> PlanRequested { get; } = null!;
    public Event<TPlanCompleted> PlanCompleted { get; } = null!;
    public Event<PlanCancelled> PlanCancelled { get; } = null!;
    public Event<PlanFaulted> PlanFaulted { get; } = null!;

    // Plan states
    public State PlanPending { get; } = null!;
    public State PlanWaitingForRunner { get; } = null!;

    private void Configure_Plan()
    {
        Event(() => PlanRequested, x => x.CorrelateById(y => y.Message.CorrelationId));
        Event(() => PlanCompleted, x => x.CorrelateById(y => y.Message.CorrelationId));
        Event(() => PlanCancelled, x => x.CorrelateById(y => y.Message.CorrelationId));
        Event(() => PlanFaulted, x => x.CorrelateById(y => y.Message.CorrelationId));

        During(PlanPending,
            When(PlanCompleted)
                .Then(async context =>
                {
                    // Publish ResourceCountRefreshedEvent
                    await context.Publish(new ResourceCountRefreshedEvent
                    {
                        JobId = context.Saga.CorrelationId,
                        ModuleId = context.Saga.ModuleId,
                        OrganizationId = context.Saga.OrganizationId,
                        ActualResourceCount = context.Message.TotalCountBefore
                    });
                })
                .Activity(a => a.OfType<UpdateOutputListsActivity<TSaga, TPlanCompleted>>())
                .IfElse(
                    x => x.Message.TotalChangedCount + x.Message.OutputsTotalChangedCount == 0,
                    ///////////////////////////////////////////////////
                    // Nothing to apply, continue to Output
                    ///////////////////////////////////////////////////
                    x => x
                        .Then(_ => { _logger.LogInformation($"Nothing to apply, continuing to Output."); })
                        // Use SendToRunnerActivity to target specific server instance
                        .Activity(a => a.OfType<SendToRunnerActivity<TSaga, TPlanCompleted, OutputRequested>>())
                        .IfElse(
                            context => context.Saga.PreviousStateBeforeWaiting != null,
                            // Runner is disconnected - transition to waiting state
                            whenTrue => whenTrue
                                .Then(context =>
                                {
                                    context.Saga.WaitingSince = DateTime.UtcNow;
                                    _logger.LogWarning(
                                        "Plan: Runner disconnected for job {CorrelationId}, entering waiting state",
                                        context.Saga.CorrelationId);
                                })
                                .TransitionTo(OutputWaitingForRunner),
                            // Runner is connected - continue normally
                            whenFalse => whenFalse
                                .TransitionTo(OutputPending)
                        ),
                    ///////////////////////////////////////////////////
                    // Something to apply (if approved)
                    ///////////////////////////////////////////////////
                    x => DealWithApprovalStatus(x, true)
                )
            //// TODO! use the below to raise an event that can be used for Dashboard notifications!
            // .Publish(
            //     context => new TApplyFromPlanRequested
            //     {
            //         CorrelationId = context.Saga.CorrelationId,
            //         Declared = JsonSerializer.Deserialize<ResolvedModule>(context.Saga.DeclaredJson)
            //     })
            ,
            When(HeartbeatScheduled.Received)
                .ThenHeartbeatScheduled(HeartbeatRequested),
            When(HeartbeatRequested.Completed)
                .ThenHeartbeatCompleted(HeartbeatScheduled),
            When(HeartbeatRequested.Completed2)
                .ThenJobTimedOut<TSaga, TResponseFailed>(Failed),
            When(CancelModuleRequested)
                .IfCancelKill<TSaga, TResponseCancelled>(_logger, CancelKillRequested, CancellingImmediateKill, Cancelled)
                .IfCancelGraceful<TSaga, TResponseCancelled>(_logger, CancelGracefulRequested, CancellingImmediateGraceful, Cancelled)
                .IfCancelAfterCurrent(_logger, CancellingAfterCurrent),
            When(PlanCancelled)
                .ThenCancelled<TSaga, TResponseCancelled, PlanCancelled>(Cancelled),
            When(PlanFaulted)
                .ThenFaulted<TSaga, TResponseFailed, PlanFaulted>(Failed, _logger),
            Ignore(RunnerReconnectedEvent)
        );

        // Handle waiting state when runner is disconnected before Plan
        During(PlanWaitingForRunner,
            // Execute self-healing checks when entering this state
            When(PlanWaitingForRunner.Enter)
                .Activity(x => x.OfType<CheckRunnerConnectionActivity<TSaga, object>>()),
            When(RunnerReconnectedEvent)
                .Then(context =>
                {
                    _logger.LogInformation(
                        "Plan: Runner reconnected for job {CorrelationId}, retrying send",
                        context.Saga.CorrelationId);
                })
                // Retry sending TPlanRequested
                .Activity(x => x.OfType<SendToRunnerActivity<TSaga, RunnerReconnectedEvent, TPlanRequested>>())
                .IfElse(
                    context => context.Saga.PreviousStateBeforeWaiting != null,
                    // Still disconnected (race condition) - stay in waiting
                    whenTrue => whenTrue
                        .Then(context =>
                        {
                            _logger.LogWarning(
                                "Plan: Runner disconnected again for job {CorrelationId}, staying in waiting state",
                                context.Saga.CorrelationId);
                        }),
                    // Successfully sent - transition to pending
                    whenFalse => whenFalse
                        .Then(context =>
                        {
                            context.Saga.WaitingSince = null;
                            _logger.LogInformation(
                                "Plan: Successfully sent for job {CorrelationId}, transitioning to pending",
                                context.Saga.CorrelationId);
                        })
                        .Schedule(HeartbeatScheduled,
                            context => new HeartbeatScheduled { CorrelationId = context.Saga.CorrelationId, OrganizationId = context.Saga.OrganizationId })
                        .TransitionTo(PlanPending)
                ),
            When(CancelModuleRequested)
                .IfCancelKill<TSaga, TResponseCancelled>(_logger, CancelKillRequested, CancellingImmediateKill, Cancelled)
                .IfCancelGraceful<TSaga, TResponseCancelled>(_logger, CancelGracefulRequested, CancellingImmediateGraceful, Cancelled)
                .IfCancelAfterCurrent(_logger, CancellingAfterCurrent)
        );
    }
}