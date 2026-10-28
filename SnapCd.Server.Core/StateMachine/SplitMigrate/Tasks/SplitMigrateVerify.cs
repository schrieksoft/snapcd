// SPDX-License-Identifier: LicenseRef-Snap-CD-Source-Available-1.1
// Copyright (c) 2026 Karl Schriek / Schrieksoft.
// No license is granted to use this file, in whole or in part, (a) as training, fine-tuning, retrieval, or
// embedding data for any machine-learning model, or (b) as input to any machine-learning model, agent, or automated
// system for the purpose of producing a derivative work or reimplementation that is not otherwise permitted by the
// Snap CD Source-Available License (including any Competing Product as defined therein). Contact info@snapcd.io
// for terms covering either use.


using MassTransit;
using SnapCd.Server.Core.Entities.Sagas;
using SnapCd.Server.Core.Events.Jobs.Module;
using SnapCd.Server.Core.Events.Runners;
using SnapCd.Server.Core.Events.Steps;
using SnapCd.Server.Core.Events.Steps.SplitMigrate;
using SnapCd.Server.Core.StateMachine.Jobs.Activites;
using SnapCd.Server.Core.StateMachine.Jobs.Utils;
using SnapCd.Server.Core.StateMachine.SplitMigrate.Utils;

namespace SnapCd.Server.Core.StateMachine.SplitMigrate;

public partial class SplitMigrateStateMachine
{
    public Event<SplitMigrateVerifyCompleted> SplitMigrateVerifyCompleted { get; } = null!;
    public Event<SplitMigrateVerifyCancelled> SplitMigrateVerifyCancelled { get; } = null!;
    public Event<SplitMigrateVerifyFaulted> SplitMigrateVerifyFaulted { get; } = null!;

    public State SplitMigrateVerifyPending { get; } = null!;
    public State SplitMigrateVerifyWaitingForRunner { get; } = null!;

    /// <summary>The last step: the same proof re-run against the real backends. Ends the job.</summary>
    private void Configure_SplitMigrateVerify()
    {
        Event(() => SplitMigrateVerifyCompleted, x => x.CorrelateById(y => y.Message.CorrelationId));
        Event(() => SplitMigrateVerifyCancelled, x => x.CorrelateById(y => y.Message.CorrelationId));
        Event(() => SplitMigrateVerifyFaulted, x => x.CorrelateById(y => y.Message.CorrelationId));

        During(SplitMigrateVerifyWaitingForRunner,
            ContainerActivityExtensions
                .Activity<SplitMigrateSaga>(When(SplitMigrateVerifyWaitingForRunner.Enter), x => x.OfType<CheckRunnerConnectionActivity<SplitMigrateSaga, SplitMigrateVerifyCompleted>>()),
            JobExtensionMethods
                .IfCancelKill<SplitMigrateSaga, SplitMigrateCancelled, CancelStateMigrationJobRequested, SplitCancelKillRequested, DummySplitCancelKillCompleted>(When(CancelStateMigrationJobRequested), _logger, CancelKillRequested, CancellingImmediateKill, Cancelled)
                .IfCancelAfterCurrent<SplitMigrateSaga, CancelStateMigrationJobRequested>(_logger, CancellingAfterCurrent),
            Ignore(RunnerReconnectedEvent),
            Ignore(HeartbeatScheduled.Received),
            Ignore(HeartbeatRequested.Completed),
            Ignore(HeartbeatRequested.Completed2)
        );

        During(SplitMigrateVerifyPending,
            ThenExtensions
                .Then<SplitMigrateSaga, SplitMigrateVerifyCompleted>(When(SplitMigrateVerifyCompleted), context => { context.Saga.ProvenModuleCount = context.Message.ModulesPlanningClean; })
                .ThenSplitCompleted(Completed),
            JobExtensionMethods
                .ThenHeartbeatScheduled<SplitMigrateSaga, StateMigrationHeartbeatRequested>(When(HeartbeatScheduled.Received), HeartbeatRequested),
            JobExtensionMethods
                .ThenHeartbeatCompleted<SplitMigrateSaga>(When(HeartbeatRequested.Completed), HeartbeatScheduled),
            SplitMigrateExtensionMethods.ThenSplitTimedOut(When(HeartbeatRequested.Completed2), Failed),
            JobExtensionMethods
                .IfCancelKill<SplitMigrateSaga, SplitMigrateCancelled, CancelStateMigrationJobRequested, SplitCancelKillRequested, DummySplitCancelKillCompleted>(When(CancelStateMigrationJobRequested), _logger, CancelKillRequested, CancellingImmediateKill, Cancelled)
                .IfCancelAfterCurrent<SplitMigrateSaga, CancelStateMigrationJobRequested>(_logger, CancellingAfterCurrent),
            SplitMigrateExtensionMethods
                .ThenSplitCancelled<SplitMigrateVerifyCancelled>(When(SplitMigrateVerifyCancelled), Cancelled),
            SplitMigrateExtensionMethods
                .ThenSplitFaulted<SplitMigrateVerifyFaulted>(When(SplitMigrateVerifyFaulted), Failed, _logger),
            Ignore(RunnerReconnectedEvent)
        );
    }
}
