// SPDX-License-Identifier: LicenseRef-Snap-CD-Source-Available-1.1
// Copyright (c) 2026 Karl Schriek / Schrieksoft.
// No license is granted to use this file, in whole or in part, (a) as training, fine-tuning, retrieval, or
// embedding data for any machine-learning model, or (b) as input to any machine-learning model, agent, or automated
// system for the purpose of producing a derivative work or reimplementation that is not otherwise permitted by the
// Snap CD Source-Available License (including any Competing Product as defined therein). Contact info@snapcd.io
// for terms covering either use.


using MassTransit;
using Microsoft.Extensions.Logging;
using SnapCd.Contracts.RunnerRequests;
using SnapCd.Server.Core.Entities.Sagas;
using SnapCd.Server.Core.Events.Runners;
using SnapCd.Server.Core.Events.Jobs.Module;
using SnapCd.Server.Core.Events.Steps;
using SnapCd.Server.Core.StateMachine.Jobs.Activites;
using SnapCd.Server.Core.StateMachine.SplitMonolith.Activites;
using SnapCd.Server.Core.StateMachine.Jobs.Utils;

namespace SnapCd.Server.Core.StateMachine.SplitMonolith;

public partial class SplitMonolithStateMachine
{
    public Event<SelectRunnerInstanceCompleted> SelectRunnerInstanceCompleted { get; } = null!;
    public Event<SelectRunnerInstanceCancelled> SelectRunnerInstanceCancelled { get; } = null!;
    public Event<SelectRunnerInstanceFaulted> SelectRunnerInstanceFaulted { get; } = null!;

    public State SelectRunnerInstancePending { get; } = null!;

    /// <summary>
    /// Pins the runner instance for the whole job. Every later step dispatches to it, so the
    /// working directory and demonolith's local artifacts persist across the pipeline.
    /// </summary>
    private void Configure_SelectRunnerInstance()
    {
        Event(() => SelectRunnerInstanceCompleted, x => x.CorrelateById(y => y.Message.CorrelationId));
        Event(() => SelectRunnerInstanceCancelled, x => x.CorrelateById(y => y.Message.CorrelationId));
        Event(() => SelectRunnerInstanceFaulted, x => x.CorrelateById(y => y.Message.CorrelationId));

        During(SelectRunnerInstancePending,
            When(CancelModuleRequested)
                .IfCancelKill<SplitMonolithSaga, SplitMonolithCancelled>(_logger, CancelKillRequested, CancellingImmediateKill, Cancelled)
                .IfCancelGraceful<SplitMonolithSaga, SplitMonolithCancelled>(_logger, CancelGracefulRequested, CancellingImmediateGraceful, Cancelled)
                .IfCancelAfterCurrent(_logger, CancellingAfterCurrent),
            When(SelectRunnerInstanceCompleted)
                .Then(context => { context.Saga.RunnerInstanceName = context.Message.RunnerInstanceName; })
                .Activity(x => x.OfType<SendSplitStepToRunnerActivity<SelectRunnerInstanceCompleted, GetModuleRequested>>())
                .IfElse(
                    context => context.Saga.PreviousStateBeforeWaiting != null,
                    whenTrue => whenTrue
                        .Then(context =>
                        {
                            context.Saga.WaitingSince = DateTime.UtcNow;
                            _logger.LogWarning(
                                "SelectRunnerInstance: Runner disconnected for job {CorrelationId}, entering waiting state",
                                context.Saga.CorrelationId);
                        })
                        .TransitionTo(GetModuleWaitingForRunner),
                    whenFalse => whenFalse
                        .Schedule(HeartbeatScheduled,
                            context => new HeartbeatScheduled { CorrelationId = context.Saga.CorrelationId, OrganizationId = context.Saga.OrganizationId })
                        .TransitionTo(GetModulePending)
                ),
            When(SelectRunnerInstanceCancelled)
                .ThenSplitCancelled(Cancelled),
            When(SelectRunnerInstanceFaulted)
                .ThenSplitFaulted(Failed, _logger),
            Ignore(RunnerReconnectedEvent),
            Ignore(HeartbeatScheduled.Received),
            Ignore(HeartbeatRequested.Completed),
            Ignore(HeartbeatRequested.Completed2)
        );
    }
}
