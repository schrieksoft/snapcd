// SPDX-License-Identifier: LicenseRef-Snap-CD-Source-Available-1.1
// Copyright (c) 2026 Karl Schriek / Schrieksoft.
// No license is granted to use this file, in whole or in part, (a) as training, fine-tuning, retrieval, or
// embedding data for any machine-learning model, or (b) as input to any machine-learning model, agent, or automated
// system for the purpose of producing a derivative work or reimplementation that is not otherwise permitted by the
// Snap CD Source-Available License (including any Competing Product as defined therein). Contact info@snapcd.io
// for terms covering either use.


using MassTransit;
using Microsoft.Extensions.Logging;
using SnapCd.Server.Core.Entities.Sagas;
using SnapCd.Server.Core.Events.Jobs.Module;
using SnapCd.Server.Core.Events.Runners;
using SnapCd.Server.Core.Events.Steps;
using SnapCd.Server.Core.Events.Steps.SplitMonolith;
using SnapCd.Server.Core.StateMachine.Jobs.Utils;

namespace SnapCd.Server.Core.StateMachine.SplitMonolith;

public partial class SplitMonolithStateMachine
{
    public Event<MigrateProveCompleted> MigrateProveCompleted { get; } = null!;
    public Event<MigrateProveCancelled> MigrateProveCancelled { get; } = null!;
    public Event<MigrateProveFaulted> MigrateProveFaulted { get; } = null!;

    public State MigrateProvePending { get; } = null!;
    public State MigrateProveWaitingForRunner { get; } = null!;

    /// <summary>
    /// The last reversible step. A negative verdict here stops the job with its reason; a clean
    /// proof carries the evidence an approver needs, so the gate follows immediately.
    /// </summary>
    private void Configure_MigrateProve()
    {
        Event(() => MigrateProveCompleted, x => x.CorrelateById(y => y.Message.CorrelationId));
        Event(() => MigrateProveCancelled, x => x.CorrelateById(y => y.Message.CorrelationId));
        Event(() => MigrateProveFaulted, x => x.CorrelateById(y => y.Message.CorrelationId));

        During(MigrateProveWaitingForRunner,
            When(MigrateProveWaitingForRunner.Enter)
                .Activity(x => x.OfType<SnapCd.Server.Core.StateMachine.Jobs.Activites.CheckRunnerConnectionActivity<SplitMonolithSaga, MigrateProveCompleted>>()),
            When(CancelModuleRequested)
                .IfCancelKill<SplitMonolithSaga, SplitMonolithCancelled>(_logger, CancelKillRequested, CancellingImmediateKill, Cancelled)
                .IfCancelGraceful<SplitMonolithSaga, SplitMonolithCancelled>(_logger, CancelGracefulRequested, CancellingImmediateGraceful, Cancelled)
                .IfCancelAfterCurrent(_logger, CancellingAfterCurrent),
            Ignore(RunnerReconnectedEvent),
            Ignore(HeartbeatScheduled.Received),
            Ignore(HeartbeatRequested.Completed),
            Ignore(HeartbeatRequested.Completed2)
        );

        During(MigrateProvePending,
            DealWithApprovalStatus(
                When(MigrateProveCompleted)
                    .Then(context => { context.Saga.ProvenModuleCount = context.Message.ModulesProven; }),
                true),
            When(HeartbeatScheduled.Received)
                .ThenHeartbeatScheduled(HeartbeatRequested),
            When(HeartbeatRequested.Completed)
                .ThenHeartbeatCompleted(HeartbeatScheduled),
            When(HeartbeatRequested.Completed2)
                .ThenSplitTimedOut(Failed),
            When(CancelModuleRequested)
                .IfCancelKill<SplitMonolithSaga, SplitMonolithCancelled>(_logger, CancelKillRequested, CancellingImmediateKill, Cancelled)
                .IfCancelGraceful<SplitMonolithSaga, SplitMonolithCancelled>(_logger, CancelGracefulRequested, CancellingImmediateGraceful, Cancelled)
                .IfCancelAfterCurrent(_logger, CancellingAfterCurrent),
            When(MigrateProveCancelled)
                .ThenSplitCancelled(Cancelled),
            When(MigrateProveFaulted)
                .ThenSplitFaulted(Failed, _logger),
            Ignore(RunnerReconnectedEvent)
        );
    }
}
