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
                        .Then(_ => { _logger.LogDebug($"Nothing to apply, continuing to Output."); })
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
                    // Something to apply: policies first (if any), then approval
                    ///////////////////////////////////////////////////
                    x => x
                        .Activity(a => a.OfType<RecordPolicyOutcomeActivity<TSaga, TPlanCompleted>>())
                        .IfElse(
                            ctx => PolicyApplicability.Any(ctx.Saga.DeclaredJson, IsDestroyJob),
                            withPolicies => withPolicies
                                .Then(_ => { _logger.LogDebug("Policies in scope, dispatching PolicyValidate."); })
                                .Activity(a => a.OfType<SendToRunnerActivity<TSaga, TPlanCompleted, PolicyValidateRequested>>())
                                .IfElse(
                                    ctx => ctx.Saga.PreviousStateBeforeWaiting != null,
                                    whenTrue => whenTrue
                                        .Then(ctx =>
                                        {
                                            ctx.Saga.WaitingSince = DateTime.UtcNow;
                                            _logger.LogWarning(
                                                "PolicyValidate: Runner disconnected for job {CorrelationId}, entering waiting state",
                                                ctx.Saga.CorrelationId);
                                        })
                                        .TransitionTo(PolicyValidateWaitingForRunner),
                                    whenFalse => whenFalse
                                        .Schedule(HeartbeatScheduled,
                                            ctx => new HeartbeatScheduled { CorrelationId = ctx.Saga.CorrelationId, OrganizationId = ctx.Saga.OrganizationId })
                                        .TransitionTo(PolicyValidatePending)
                                ),
                            noPolicies => DealWithApprovalStatus(noPolicies, true)
                        )
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
                .IfElse(
                    x => x.Message.PolicyOutcome == PolicyOutcome.HardDenied,
                    ///////////////////////////////////////////////////
                    // The preview failed because a policy denied it (Pulumi/CrossGuard
                    // runs inside the plan) — refuse the job, don't fail it.
                    ///////////////////////////////////////////////////
                    x => x
                        .Activity(a => a.OfType<RecordPolicyOutcomeActivity<TSaga, PlanFaulted>>())
                        .Publish(context => new TResponseCancelled
                        {
                            ModuleId = context.Saga.ModuleId,
                            OrganizationId = context.Saga.OrganizationId,
                            ModuleJobId = context.Saga.CorrelationId,
                            CancellationReason = CancellationReason.PolicyDenied
                        })
                        .Activity(a => a.OfType<PolicyDeniedModuleJobActivity<TSaga, PlanFaulted>>())
                        .TransitionTo(PolicyDenied)
                        .Finalize(),
                    x => x.ThenFaulted<TSaga, TResponseFailed, PlanFaulted>(Failed, _logger)
                ),
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
                            _logger.LogDebug(
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
                .IfCancelAfterCurrent(_logger, CancellingAfterCurrent),
            Ignore(HeartbeatScheduled.Received),
            Ignore(HeartbeatRequested.Completed),
            Ignore(HeartbeatRequested.Completed2)
        );
    }
}