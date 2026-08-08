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
    // GetDefinitiveRevision events
    public Event<GetDefinitiveRevisionCompleted> GetDefinitiveRevisionCompleted { get; } = null!;
    public Event<GetDefinitiveRevisionCancelled> GetDefinitiveRevisionCancelled { get; } = null!;
    public Event<GetDefinitiveRevisionFaulted> GetDefinitiveRevisionFaulted { get; } = null!;

    // States
    public State GetDefinitiveRevisionPending { get; } = null!;
    public State GetDefinitiveRevisionWaitingForRunner { get; } = null!;

    private void Configure_GetDefinitiveRevision()
    {
        // GetDefinitiveRevision events
        Event(() => GetDefinitiveRevisionCompleted, x => x.CorrelateById(y => y.Message.CorrelationId));
        Event(() => GetDefinitiveRevisionCancelled, x => x.CorrelateById(y => y.Message.CorrelationId));
        Event(() => GetDefinitiveRevisionFaulted, x => x.CorrelateById(y => y.Message.CorrelationId));

        // Handle waiting state when runner is disconnected after SelectRunnerInstance
        During(GetDefinitiveRevisionWaitingForRunner,
            // Execute self-healing checks when entering this state
            When(GetDefinitiveRevisionWaitingForRunner.Enter)
                .Activity(x => x.OfType<CheckRunnerConnectionActivity<TSaga, SelectRunnerInstanceCompleted>>()),
            When(RunnerReconnectedEvent)
                .Then(context =>
                {
                    _logger.LogInformation(
                        "GetDefinitiveRevision: Runner reconnected for job {CorrelationId}, retrying send",
                        context.Saga.CorrelationId);
                })
                // Retry sending GetDefinitiveRevisionRequested
                .Activity(x => x.OfType<SendToRunnerActivity<TSaga, RunnerReconnectedEvent, GetDefinitiveRevisionRequested>>())
                .IfElse(
                    context => context.Saga.PreviousStateBeforeWaiting != null,
                    // Still disconnected (race condition) - stay in waiting
                    whenTrue => whenTrue
                        .Then(context =>
                        {
                            _logger.LogWarning(
                                "GetDefinitiveRevision: Runner disconnected again for job {CorrelationId}, staying in waiting state",
                                context.Saga.CorrelationId);
                        }),
                    // Successfully sent - transition to pending
                    whenFalse => whenFalse
                        .Then(context =>
                        {
                            context.Saga.WaitingSince = null;
                            _logger.LogDebug(
                                "GetDefinitiveRevision: Successfully sent for job {CorrelationId}, transitioning to pending",
                                context.Saga.CorrelationId);
                        })
                        .Schedule(HeartbeatScheduled,
                            context => new HeartbeatScheduled { CorrelationId = context.Saga.CorrelationId, OrganizationId = context.Saga.OrganizationId })
                        .TransitionTo(GetDefinitiveRevisionPending)
                ),
            When(CancelModuleRequested)
                .IfCancelKill<TSaga, TResponseCancelled>(_logger, CancelKillRequested, CancellingImmediateKill, Cancelled)
                .IfCancelGraceful<TSaga, TResponseCancelled>(_logger, CancelGracefulRequested, CancellingImmediateGraceful, Cancelled)
                .IfCancelAfterCurrent(_logger, CancellingAfterCurrent),
            Ignore(HeartbeatScheduled.Received),
            Ignore(HeartbeatRequested.Completed),
            Ignore(HeartbeatRequested.Completed2),
            Ignore(SelectRunnerInstanceCompleted),
            Ignore(SelectRunnerInstanceCancelled),
            Ignore(SelectRunnerInstanceFaulted)
        );

        // GetDefinitiveRevisionPending state
        During(GetDefinitiveRevisionPending,
            When(GetDefinitiveRevisionCompleted)
                // Use activity to send GetModuleRequested to specific server instance
                .Then(context => { context.Saga.DefinitiveRevision = context.Message.DefinitiveRevision; })
                .Activity(x => x.OfType<SetDefinitiveRevisionActivity<TSaga, GetDefinitiveRevisionCompleted>>())
                .Activity(x => x.OfType<SendToRunnerActivity<TSaga, GetDefinitiveRevisionCompleted, GetModuleRequested>>())
                .IfElse(
                    context => context.Saga.PreviousStateBeforeWaiting != null,
                    // Runner is disconnected - transition to waiting state
                    whenTrue => whenTrue
                        .Then(context =>
                        {
                            context.Saga.WaitingSince = DateTime.UtcNow;
                            _logger.LogWarning(
                                "GetDefinitiveRevision: Runner disconnected for job {CorrelationId}, entering waiting state",
                                context.Saga.CorrelationId);
                        })
                        .TransitionTo(GetModuleWaitingForRunner),
                    // Runner is connected - continue normally
                    whenFalse => whenFalse
                        .Schedule(HeartbeatScheduled,
                            context => new HeartbeatScheduled { CorrelationId = context.Saga.CorrelationId, OrganizationId = context.Saga.OrganizationId })
                        .TransitionTo(GetModulePending)
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
            When(GetDefinitiveRevisionCancelled)
                .ThenCancelled<TSaga, TResponseCancelled, GetDefinitiveRevisionCancelled>(Cancelled),
            When(GetDefinitiveRevisionFaulted)
                .ThenFaulted<TSaga, TResponseFailed, GetDefinitiveRevisionFaulted>(Failed, _logger),
            Ignore(SelectRunnerInstanceCompleted),
            Ignore(SelectRunnerInstanceCancelled),
            Ignore(SelectRunnerInstanceFaulted)
        );
    }
}