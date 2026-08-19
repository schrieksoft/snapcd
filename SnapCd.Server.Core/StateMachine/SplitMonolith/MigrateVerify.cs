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
using SnapCd.Server.Core.StateMachine.Jobs.Activites;
using SnapCd.Server.Core.StateMachine.Jobs.Utils;

namespace SnapCd.Server.Core.StateMachine.SplitMonolith;

public partial class SplitMonolithStateMachine
{
    public Event<MigrateVerifyCompleted> MigrateVerifyCompleted { get; } = null!;
    public Event<MigrateVerifyCancelled> MigrateVerifyCancelled { get; } = null!;
    public Event<MigrateVerifyFaulted> MigrateVerifyFaulted { get; } = null!;

    public State MigrateVerifyPending { get; } = null!;
    public State MigrateVerifyWaitingForRunner { get; } = null!;

    /// <summary>The last step: the same proof re-run against the real backends. Ends the job.</summary>
    private void Configure_MigrateVerify()
    {
        Event(() => MigrateVerifyCompleted, x => x.CorrelateById(y => y.Message.CorrelationId));
        Event(() => MigrateVerifyCancelled, x => x.CorrelateById(y => y.Message.CorrelationId));
        Event(() => MigrateVerifyFaulted, x => x.CorrelateById(y => y.Message.CorrelationId));

        During(MigrateVerifyWaitingForRunner,
            When(MigrateVerifyWaitingForRunner.Enter)
                .Activity(x => x.OfType<CheckRunnerConnectionActivity<SplitMonolithSaga, MigrateVerifyCompleted>>()),
            When(CancelModuleRequested)
                .IfCancelKill<SplitMonolithSaga, SplitMonolithCancelled>(_logger, CancelKillRequested, CancellingImmediateKill, Cancelled)
                .IfCancelGraceful<SplitMonolithSaga, SplitMonolithCancelled>(_logger, CancelGracefulRequested, CancellingImmediateGraceful, Cancelled)
                .IfCancelAfterCurrent(_logger, CancellingAfterCurrent),
            Ignore(RunnerReconnectedEvent),
            Ignore(HeartbeatScheduled.Received),
            Ignore(HeartbeatRequested.Completed),
            Ignore(HeartbeatRequested.Completed2)
        );

        During(MigrateVerifyPending,
            When(MigrateVerifyCompleted)
                .Then(context => { context.Saga.ProvenModuleCount = context.Message.ModulesPlanningClean; })
                .ThenSplitCompleted(Completed),
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
            When(MigrateVerifyCancelled)
                .ThenSplitCancelled(Cancelled),
            When(MigrateVerifyFaulted)
                .ThenSplitFaulted(Failed, _logger),
            Ignore(RunnerReconnectedEvent)
        );
    }
}
