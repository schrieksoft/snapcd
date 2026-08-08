// SPDX-License-Identifier: LicenseRef-Snap-CD-Source-Available-1.1
// Copyright (c) 2026 Karl Schriek / Schrieksoft.
// No license is granted to use this file, in whole or in part, (a) as training, fine-tuning, retrieval, or
// embedding data for any machine-learning model, or (b) as input to any machine-learning model, agent, or automated
// system for the purpose of producing a derivative work or reimplementation that is not otherwise permitted by the
// Snap CD Source-Available License (including any Competing Product as defined therein). Contact info@snapcd.io
// for terms covering either use.

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
    public Event<TApplyFromPlanRequested> ApplyFromPlanRequested { get; } = null!;
    public Event<TApplyFromPlanCompleted> ApplyFromPlanCompleted { get; } = null!;
    public Event<ApplyFromPlanCancelled> ApplyFromPlanCancelled { get; } = null!;
    public Event<ApplyFromPlanFaulted> ApplyFromPlanFaulted { get; } = null!;

    public State ApplyFromPlanPending { get; } = null!;
    public State ApplyFromPlanWaitingForRunner { get; } = null!;

    private void Configure_ApplyFromPlan()
    {
        Event(() => ApplyFromPlanRequested, x => x.CorrelateById(y => y.Message.CorrelationId));
        Event(() => ApplyFromPlanCompleted, x => x.CorrelateById(y => y.Message.CorrelationId));
        Event(() => ApplyFromPlanCancelled, x => x.CorrelateById(y => y.Message.CorrelationId));
        Event(() => ApplyFromPlanFaulted, x => x.CorrelateById(y => y.Message.CorrelationId));

        // Handle waiting state when runner is disconnected after approval
        During(ApplyFromPlanWaitingForRunner,
            // Execute self-healing checks when entering this state
            When(ApplyFromPlanWaitingForRunner.Enter)
                .Activity(x => x.OfType<CheckRunnerConnectionActivity<TSaga, ApprovalReevaluationRequestedEvent>>()),
            When(RunnerReconnectedEvent)
                .Then(context =>
                {
                    _logger.LogInformation(
                        "Approval: Runner reconnected for job {CorrelationId}, retrying send",
                        context.Saga.CorrelationId);
                })
                // Retry sending TApplyFromPlanRequested
                .Activity(x => x.OfType<SendToRunnerActivity<TSaga, RunnerReconnectedEvent, TApplyFromPlanRequested>>())
                .IfElse(
                    context => context.Saga.PreviousStateBeforeWaiting != null,
                    // Still disconnected (race condition) - stay in waiting
                    whenTrue => whenTrue
                        .Then(context =>
                        {
                            _logger.LogWarning(
                                "Approval: Runner disconnected again for job {CorrelationId}, staying in waiting state",
                                context.Saga.CorrelationId);
                        }),
                    // Successfully sent - transition to pending
                    whenFalse => whenFalse
                        .Then(context =>
                        {
                            context.Saga.WaitingSince = null;
                            _logger.LogInformation(
                                "Approval: Successfully sent for job {CorrelationId}, transitioning to pending",
                                context.Saga.CorrelationId);
                        })
                        .Schedule(HeartbeatScheduled,
                            context => new HeartbeatScheduled { CorrelationId = context.Saga.CorrelationId, OrganizationId = context.Saga.OrganizationId })
                        .TransitionTo(ApplyFromPlanPending)
                ),
            When(CancelModuleRequested)
                .IfCancelKill<TSaga, TResponseCancelled>(_logger, CancelKillRequested, CancellingImmediateKill, Cancelled)
                .IfCancelGraceful<TSaga, TResponseCancelled>(_logger, CancelGracefulRequested, CancellingImmediateGraceful, Cancelled)
                .IfCancelAfterCurrent(_logger, CancellingAfterCurrent),
            Ignore(HeartbeatScheduled.Received),
            Ignore(HeartbeatRequested.Completed),
            Ignore(HeartbeatRequested.Completed2)
        );

        During(ApplyFromPlanPending,
            When(ApplyFromPlanCompleted)
                .Then(context =>
                {
                    // Extract ActualResourceCount from the completed message
                    var completedMessage = context.Message as TApplyFromPlanCompleted;
                    var actualResourceCount = completedMessage?.ActualResourceCount ?? 0;

                    // Publish ResourceCountRefreshedEvent
                    context.Publish(new ResourceCountRefreshedEvent
                    {
                        JobId = context.Saga.CorrelationId,
                        ModuleId = context.Saga.ModuleId,
                        OrganizationId = context.Saga.OrganizationId,
                        ActualResourceCount = actualResourceCount
                    });
                })
                // Use SendToRunnerActivity to target specific server instance
                .Activity(x => x.OfType<SendToRunnerActivity<TSaga, TApplyFromPlanCompleted, OutputRequested>>())
                .IfElse(
                    context => context.Saga.PreviousStateBeforeWaiting != null,
                    // Runner is disconnected - transition to waiting state
                    whenTrue => whenTrue
                        .Then(context =>
                        {
                            context.Saga.WaitingSince = DateTime.UtcNow;
                            _logger.LogWarning(
                                "ApplyFromPlan: Runner disconnected for job {CorrelationId}, entering waiting state",
                                context.Saga.CorrelationId);
                        })
                        .TransitionTo(OutputWaitingForRunner),
                    // Runner is connected - continue normally
                    whenFalse => whenFalse
                        .Schedule(HeartbeatScheduled,
                            context => new HeartbeatScheduled { CorrelationId = context.Saga.CorrelationId, OrganizationId = context.Saga.OrganizationId })
                        .TransitionTo(OutputPending)
                ),
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
            When(ApplyFromPlanCancelled)
                .ThenCancelled<TSaga, TResponseCancelled, ApplyFromPlanCancelled>(Cancelled),
            When(ApplyFromPlanFaulted)
                .Then(context =>
                {
                    // Extract ActualResourceCount from the faulted message
                    var actualResourceCount = context.Message.ActualResourceCount;

                    // Publish ResourceCountRefreshedEvent for faulted scenario
                    context.Publish(new ResourceCountRefreshedEvent
                    {
                        JobId = context.Saga.CorrelationId,
                        ModuleId = context.Saga.ModuleId,
                        OrganizationId = context.Saga.OrganizationId,
                        ActualResourceCount = actualResourceCount
                    });
                })
                .ThenFaulted<TSaga, TResponseFailed, ApplyFromPlanFaulted>(Failed, _logger),
            Ignore(RunnerReconnectedEvent)
        );
    }
}