// SPDX-License-Identifier: LicenseRef-Snap-CD-Source-Available-1.1
// Copyright (c) 2026 Karl Schriek / Schrieksoft.
// No license is granted to use this file, in whole or in part, (a) as training, fine-tuning, retrieval, or
// embedding data for any machine-learning model, or (b) as input to any machine-learning model, agent, or automated
// system for the purpose of producing a derivative work or reimplementation that is not otherwise permitted by the
// Snap CD Source-Available License (including any Competing Product as defined therein). Contact info@snapcd.io
// for terms covering either use.

using MassTransit;
using SnapCd.Contracts;
using SnapCd.Server.Core.Entities.Sagas.Base;
using SnapCd.Server.Core.Events.Jobs.Base;
using SnapCd.Server.Core.Events.Runners;
using SnapCd.Server.Core.Events.Steps;
using SnapCd.Server.Core.Events.Steps.Base;
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
    // PolicyValidate events
    public Event<PolicyValidateCompleted> PolicyValidateCompleted { get; } = null!;
    public Event<PolicyValidateCancelled> PolicyValidateCancelled { get; } = null!;
    public Event<PolicyValidateFaulted> PolicyValidateFaulted { get; } = null!;

    // PolicyValidate states
    public State PolicyValidatePending { get; } = null!;
    public State PolicyValidateWaitingForRunner { get; } = null!;

    // PolicyDenied terminal state (modeled on Declined: a refusal, not a failure)
    public State PolicyDenied { get; } = null!;

    private static bool IsDestroyJob => typeof(TSaga).Name == "DestroyJobSaga";

    private void Configure_PolicyValidate()
    {
        Event(() => PolicyValidateCompleted, x => x.CorrelateById(y => y.Message.CorrelationId));
        Event(() => PolicyValidateCancelled, x => x.CorrelateById(y => y.Message.CorrelationId));
        Event(() => PolicyValidateFaulted, x => x.CorrelateById(y => y.Message.CorrelationId));

        During(PolicyValidatePending,
            When(PolicyValidateCompleted)
                .Activity(a => a.OfType<RecordPolicyOutcomeActivity<TSaga, PolicyValidateCompleted>>())
                .IfElse(
                    x => x.Message.Outcome == PolicyOutcome.HardDenied,
                    ///////////////////////////////////////////////////
                    // Hard deny: refuse the job. Not a failure.
                    ///////////////////////////////////////////////////
                    x => x
                        .Then(_ => { _logger.LogInformation("Policy validation denied the job, finalizing as PolicyDenied."); })
                        .Publish(context => new TResponseCancelled
                        {
                            ModuleId = context.Saga.ModuleId,
                            OrganizationId = context.Saga.OrganizationId,
                            ModuleJobId = context.Saga.CorrelationId,
                            CancellationReason = CancellationReason.PolicyDenied
                        })
                        .Activity(a => a.OfType<PolicyDeniedModuleJobActivity<TSaga, PolicyValidateCompleted>>())
                        .TransitionTo(PolicyDenied)
                        .Finalize(),
                    ///////////////////////////////////////////////////
                    // Passed or SoftWarned: continue to approval/apply
                    ///////////////////////////////////////////////////
                    x => DealWithApprovalStatus(x, true)
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
            When(PolicyValidateCancelled)
                .ThenCancelled<TSaga, TResponseCancelled, PolicyValidateCancelled>(Cancelled),
            When(PolicyValidateFaulted)
                .ThenFaulted<TSaga, TResponseFailed, PolicyValidateFaulted>(Failed, _logger),
            Ignore(RunnerReconnectedEvent)
        );

        // Handle waiting state when runner is disconnected before PolicyValidate
        During(PolicyValidateWaitingForRunner,
            When(PolicyValidateWaitingForRunner.Enter)
                .Activity(x => x.OfType<CheckRunnerConnectionActivity<TSaga, object>>()),
            When(RunnerReconnectedEvent)
                .Then(context =>
                {
                    _logger.LogInformation(
                        "PolicyValidate: Runner reconnected for job {CorrelationId}, retrying send",
                        context.Saga.CorrelationId);
                })
                .Activity(x => x.OfType<SendToRunnerActivity<TSaga, RunnerReconnectedEvent, PolicyValidateRequested>>())
                .IfElse(
                    context => context.Saga.PreviousStateBeforeWaiting != null,
                    whenTrue => whenTrue
                        .Then(context =>
                        {
                            _logger.LogWarning(
                                "PolicyValidate: Runner disconnected again for job {CorrelationId}, staying in waiting state",
                                context.Saga.CorrelationId);
                        }),
                    whenFalse => whenFalse
                        .Then(context =>
                        {
                            context.Saga.WaitingSince = null;
                            _logger.LogInformation(
                                "PolicyValidate: Successfully sent for job {CorrelationId}, transitioning to pending",
                                context.Saga.CorrelationId);
                        })
                        .Schedule(HeartbeatScheduled,
                            context => new HeartbeatScheduled { CorrelationId = context.Saga.CorrelationId, OrganizationId = context.Saga.OrganizationId })
                        .TransitionTo(PolicyValidatePending)
                ),
            When(CancelModuleRequested)
                .IfCancelKill<TSaga, TResponseCancelled>(_logger, CancelKillRequested, CancellingImmediateKill, Cancelled)
                .IfCancelGraceful<TSaga, TResponseCancelled>(_logger, CancelGracefulRequested, CancellingImmediateGraceful, Cancelled)
                .IfCancelAfterCurrent(_logger, CancellingAfterCurrent),
            Ignore(HeartbeatScheduled.Received),
            Ignore(HeartbeatRequested.Completed),
            Ignore(HeartbeatRequested.Completed2)
        );

        // Terminal state - ignore runner reconnection events
        During(PolicyDenied,
            Ignore(RunnerReconnectedEvent)
        );
    }
}
