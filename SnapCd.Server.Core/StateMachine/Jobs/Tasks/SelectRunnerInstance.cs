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
    public Event<SelectRunnerInstanceCompleted> SelectRunnerInstanceCompleted { get; } = null!;
    public Event<SelectRunnerInstanceCancelled> SelectRunnerInstanceCancelled { get; } = null!;
    public Event<SelectRunnerInstanceFaulted> SelectRunnerInstanceFaulted { get; } = null!;

    public State SelectRunnerInstancePending { get; } = null!;

    private void Configure_SelectRunnerInstance()
    {
        Event(() => SelectRunnerInstanceCompleted, x => x.CorrelateById(y => y.Message.CorrelationId));
        Event(() => SelectRunnerInstanceCancelled, x => x.CorrelateById(y => y.Message.CorrelationId));
        Event(() => SelectRunnerInstanceFaulted, x => x.CorrelateById(y => y.Message.CorrelationId));

        During(SelectRunnerInstancePending,
            When(CancelModuleRequested)
                .IfCancelKill<TSaga, TResponseCancelled>(_logger, CancelKillRequested, CancellingImmediateKill, Cancelled)
                .IfCancelGraceful<TSaga, TResponseCancelled>(_logger, CancelGracefulRequested, CancellingImmediateGraceful, Cancelled)
                .IfCancelAfterCurrent(_logger, CancellingAfterCurrent),
            When(SelectRunnerInstanceCompleted)
                .Then(context => { context.Saga.RunnerInstanceName = context.Message.RunnerInstanceName; })
                // Use SendToRunnerActivity to target specific server instance
                .Activity(x => x.OfType<SendToRunnerActivity<TSaga, SelectRunnerInstanceCompleted, GetDefinitiveRevisionRequested>>())
                .IfElse(
                    context => context.Saga.PreviousStateBeforeWaiting != null,
                    // Runner is disconnected - transition to waiting state
                    whenTrue => whenTrue
                        .Then(context =>
                        {
                            context.Saga.WaitingSince = DateTime.UtcNow;
                            _logger.LogWarning(
                                "SelectRunnerInstance: Runner disconnected for job {CorrelationId}, entering waiting state",
                                context.Saga.CorrelationId);
                        })
                        .TransitionTo(GetDefinitiveRevisionWaitingForRunner),
                    // Runner is connected - continue normally
                    whenFalse => whenFalse
                        .Schedule(HeartbeatScheduled,
                            context => new HeartbeatScheduled { CorrelationId = context.Saga.CorrelationId, OrganizationId = context.Saga.OrganizationId })
                        .TransitionTo(GetDefinitiveRevisionPending)
                ),
            When(SelectRunnerInstanceCancelled)
                .ThenCancelled<TSaga, TResponseCancelled, SelectRunnerInstanceCancelled>(Cancelled),
            When(SelectRunnerInstanceFaulted)
                .ThenFaulted<TSaga, TResponseFailed, SelectRunnerInstanceFaulted>(Failed, _logger),
            Ignore(HeartbeatScheduled.Received),
            Ignore(HeartbeatRequested.Completed),
            Ignore(HeartbeatRequested.Completed2),
            Ignore(RunnerReconnectedEvent)
        );
    }
}