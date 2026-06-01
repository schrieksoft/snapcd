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
    public Event<OutputRequested> OutputRequested { get; } = null!;
    public Event<OutputCompleted> OutputCompleted { get; } = null!;
    public Event<OutputCancelled> OutputCancelled { get; } = null!;
    public Event<OutputFaulted> OutputFaulted { get; } = null!;
    public State OutputWaitingForRunner { get; } = null!;

    public State OutputPending { get; } = null!;

    private void Configure_Output()
    {
        Event(() => OutputRequested, x => x.CorrelateById(y => y.Message.CorrelationId));
        Event(() => OutputCompleted, x => x.CorrelateById(y => y.Message.CorrelationId));
        Event(() => OutputCancelled, x => x.CorrelateById(y => y.Message.CorrelationId));
        Event(() => OutputFaulted, x => x.CorrelateById(y => y.Message.CorrelationId));


        // Handle waiting state when runner is disconnected before Output
        // Note: This state can be entered from two paths (Plan or ApplyFromPlan)
        // We use a generic object type for the activity since state entry doesn't have message context
        During(OutputWaitingForRunner,
            // Execute self-healing checks when entering this state
            When(OutputWaitingForRunner.Enter)
                .Activity(x => x.OfType<CheckRunnerConnectionActivity<TSaga, object>>()),
            When(RunnerReconnectedEvent)
                .Then(context =>
                {
                    _logger.LogInformation(
                        "Plan: Runner reconnected for job {CorrelationId}, retrying send",
                        context.Saga.CorrelationId);
                })
                // Retry sending OutputRequested
                .Activity(x => x.OfType<SendToRunnerActivity<TSaga, RunnerReconnectedEvent, OutputRequested>>())
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
                        .TransitionTo(OutputPending)
                ),
            When(CancelModuleRequested)
                .IfCancelKill<TSaga, TResponseCancelled>(_logger, CancelKillRequested, CancellingImmediateKill, Cancelled)
                .IfCancelGraceful<TSaga, TResponseCancelled>(_logger, CancelGracefulRequested, CancellingImmediateGraceful, Cancelled)
                .IfCancelAfterCurrent(_logger, CancellingAfterCurrent)
        );

        During(OutputPending,
            When(OutputCompleted)
                .Then(context => { _logger.LogInformation($"Job completed for Module {context.Saga.ModuleId}"); })
                .Publish(context => new TResponseCompleted
                {
                    ModuleId = context.Saga.ModuleId,
                    ModuleJobId = context.Saga.CorrelationId
                })
                .Activity(x => x.OfType<CompleteModuleJobActivity<TSaga, OutputCompleted>>())
                .TransitionTo(Completed)
                .Finalize(),
            When(CancelModuleRequested)
                .IfCancelKill<TSaga, TResponseCancelled>(_logger, CancelKillRequested, CancellingImmediateKill, Cancelled)
                .IfCancelGraceful<TSaga, TResponseCancelled>(_logger, CancelGracefulRequested, CancellingImmediateGraceful, Cancelled)
                .IfCancelAfterCurrent(_logger, CancellingAfterCurrent),
            When(OutputCancelled)
                .ThenCancelled<TSaga, TResponseCancelled, OutputCancelled>(Cancelled),
            When(OutputFaulted)
                .ThenFaulted<TSaga, TResponseFailed, OutputFaulted>(Failed, _logger),
            Ignore(RunnerReconnectedEvent)
        );
    }
}