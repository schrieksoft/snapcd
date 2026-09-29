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
using SnapCd.Server.Core.Events.Jobs.Module;
namespace SnapCd.Server.Core.StateMachine.Jobs;

public partial class JobStateMachine<
    TSaga,
    TRequest,
    TResponseFailed,
    TResponseCompleted,
    TResponseCancelled,
    TGetDefinitiveRevisionRequested,
    TGetDefinitiveRevisionCompleted,
    TGetDefinitiveRevisionCancelled,
    TGetDefinitiveRevisionFaulted,
    TPolicyValidateRequested,
    TPolicyValidateCompleted,
    TPolicyValidateCancelled,
    TPolicyValidateFaulted,
    TOutputRequested,
    TOutputCompleted,
    TOutputCancelled,
    TOutputFaulted,
    TGetModuleRequested,
    TGetModuleCompleted,
    TGetModuleCancelled,
    TGetModuleFaulted,
    TInitRequested,
    TInitCompleted,
    TInitCancelled,
    TInitFaulted,
    TValidateRequested,
    TValidateCompleted,
    TValidateCancelled,
    TValidateFaulted,
    TVariablesRequested,
    TVariablesCompleted,
    TVariablesCancelled,
    TVariablesFaulted,
    TPlanRequested,
    TPlanCompleted,
    TPlanCancelled,
    TApplyFromPlanRequested,
    TApplyFromPlanCompleted,
    TApplyFromPlanCancelled,
    TCancelKillRequested,
    TDummyCancelKillCompleted,
    TCancelKillCompleted>
    where TSaga : JobSagaBase
    where TRequest : ModuleJobEventBase
    where TResponseFailed : ModuleJobEventCompletedBase, new()
    where TResponseCompleted : ModuleJobEventCompletedBase, new()
    where TResponseCancelled : ModuleJobEventCompletedBase, new()
    where TGetDefinitiveRevisionRequested : GetDefinitiveRevisionRequestedBase, new()
    where TGetDefinitiveRevisionCompleted : GetDefinitiveRevisionCompletedBase
    where TGetDefinitiveRevisionCancelled : StepResponseBase
    where TGetDefinitiveRevisionFaulted : StepFaultedBase
    where TPolicyValidateRequested : PolicyValidateRequestedBase, new()
    where TPolicyValidateCompleted : PolicyValidateCompletedBase
    where TPolicyValidateCancelled : StepResponseBase
    where TPolicyValidateFaulted : StepFaultedBase
    where TOutputRequested : OutputRequestedBase, new()
    where TOutputCompleted : StepResponseBase
    where TOutputCancelled : StepResponseBase
    where TOutputFaulted : StepFaultedBase
    where TGetModuleRequested : GetModuleRequestedBase, new()
    where TGetModuleCompleted : StepResponseBase
    where TGetModuleCancelled : StepResponseBase
    where TGetModuleFaulted : StepFaultedBase
    where TInitRequested : InitRequestedBase, new()
    where TInitCompleted : StepResponseBase
    where TInitCancelled : StepResponseBase
    where TInitFaulted : StepFaultedBase
    where TValidateRequested : ValidateRequestedBase, new()
    where TValidateCompleted : StepResponseBase
    where TValidateCancelled : StepResponseBase
    where TValidateFaulted : StepFaultedBase
    where TVariablesRequested : VariablesRequestedBase, new()
    where TVariablesCompleted : StepResponseBase
    where TVariablesCancelled : StepResponseBase
    where TVariablesFaulted : StepFaultedBase
    where TPlanRequested : StepRequestBase, new()
    where TPlanCompleted : PlanCompletedBase
    where TPlanCancelled : StepResponseBase
    where TApplyFromPlanRequested : StepRequestBase, new()
    where TApplyFromPlanCompleted : ApplyResponseBase
    where TApplyFromPlanCancelled : StepResponseBase
    where TCancelKillRequested : CancelKillRequestedBase, new()
    where TDummyCancelKillCompleted : class
    where TCancelKillCompleted : StepResponseBase
{
    // Plan events
    public Event<TPlanRequested> ApplyPlanRequested { get; } = null!;
    public Event<TPlanCompleted> ApplyPlanCompleted { get; } = null!;
    public Event<ApplyPlanCancelled> ApplyPlanCancelled { get; } = null!;
    public Event<ApplyPlanFaulted> ApplyPlanFaulted { get; } = null!;

    // Plan states
    public State PlanPending { get; } = null!;
    public State PlanWaitingForRunner { get; } = null!;

    private void Configure_Plan()
    {
        Event(() => ApplyPlanRequested, x => x.CorrelateById(y => y.Message.CorrelationId));
        Event(() => ApplyPlanCompleted, x => x.CorrelateById(y => y.Message.CorrelationId));
        Event(() => ApplyPlanCancelled, x => x.CorrelateById(y => y.Message.CorrelationId));
        Event(() => ApplyPlanFaulted, x => x.CorrelateById(y => y.Message.CorrelationId));

        During(PlanPending,
            When(ApplyPlanCompleted)
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
                        .Activity(a => a.OfType<SendToRunnerActivity<TSaga, TPlanCompleted, TOutputRequested>>())
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
                                .Activity(a => a.OfType<SendToRunnerActivity<TSaga, TPlanCompleted, TPolicyValidateRequested>>())
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
                .IfCancelKill<TSaga, TResponseCancelled, CancelModuleRequested, TCancelKillRequested, TDummyCancelKillCompleted>(_logger, CancelKillRequested, CancellingImmediateKill, Cancelled)
                .IfCancelAfterCurrent<TSaga, CancelModuleRequested>(_logger, CancellingAfterCurrent),
            When(ApplyPlanCancelled)
                .ThenCancelled<TSaga, TResponseCancelled, ApplyPlanCancelled>(Cancelled),
            When(ApplyPlanFaulted)
                .IfElse(
                    x => x.Message.PolicyOutcome == PolicyOutcome.HardDenied,
                    ///////////////////////////////////////////////////
                    // The preview failed because a policy denied it (Pulumi/CrossGuard
                    // runs inside the plan) — refuse the job, don't fail it.
                    ///////////////////////////////////////////////////
                    x => x
                        .Activity(a => a.OfType<RecordPolicyOutcomeActivity<TSaga, ApplyPlanFaulted>>())
                        .Publish(context => new TResponseCancelled
                        {
                            ModuleId = context.Saga.ModuleId,
                            OrganizationId = context.Saga.OrganizationId,
                            ModuleJobId = context.Saga.CorrelationId,
                            CancellationReason = CancellationReason.PolicyDenied
                        })
                        .Activity(a => a.OfType<PolicyDeniedModuleJobActivity<TSaga, ApplyPlanFaulted>>())
                        .TransitionTo(PolicyDenied)
                        .Finalize(),
                    x => x.ThenFaulted<TSaga, TResponseFailed, ApplyPlanFaulted>(Failed, _logger)
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
                .IfCancelKill<TSaga, TResponseCancelled, CancelModuleRequested, TCancelKillRequested, TDummyCancelKillCompleted>(_logger, CancelKillRequested, CancellingImmediateKill, Cancelled)
                .IfCancelAfterCurrent<TSaga, CancelModuleRequested>(_logger, CancellingAfterCurrent),
            Ignore(HeartbeatScheduled.Received),
            Ignore(HeartbeatRequested.Completed),
            Ignore(HeartbeatRequested.Completed2)
        );
    }
}