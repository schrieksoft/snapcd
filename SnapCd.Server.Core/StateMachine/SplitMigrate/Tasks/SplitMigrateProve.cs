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
using SnapCd.Server.Core.StateMachine.Jobs.Utils;
using SnapCd.Server.Core.StateMachine.SplitMigrate.Utils;

namespace SnapCd.Server.Core.StateMachine.SplitMigrate;

public partial class SplitMigrateStateMachine
{
    public Event<SplitMigrateProveCompleted> SplitMigrateProveCompleted { get; } = null!;
    public Event<SplitMigrateProveCancelled> SplitMigrateProveCancelled { get; } = null!;
    public Event<SplitMigrateProveFaulted> SplitMigrateProveFaulted { get; } = null!;

    public State SplitMigrateProvePending { get; } = null!;
    public State SplitMigrateProveWaitingForRunner { get; } = null!;

    /// <summary>
    /// The last reversible step. A negative verdict here stops the job with its reason; a clean
    /// proof carries the evidence an approver needs, so the gate follows immediately.
    /// </summary>
    private void Configure_SplitMigrateProve()
    {
        Event(() => SplitMigrateProveCompleted, x => x.CorrelateById(y => y.Message.CorrelationId));
        Event(() => SplitMigrateProveCancelled, x => x.CorrelateById(y => y.Message.CorrelationId));
        Event(() => SplitMigrateProveFaulted, x => x.CorrelateById(y => y.Message.CorrelationId));

        During(SplitMigrateProveWaitingForRunner,
            ContainerActivityExtensions
                .Activity<SplitMigrateSaga>(When(SplitMigrateProveWaitingForRunner.Enter), x => x.OfType<SnapCd.Server.Core.StateMachine.Jobs.Activites.CheckRunnerConnectionActivity<SplitMigrateSaga, SplitMigrateProveCompleted>>()),
            JobExtensionMethods
                .IfCancelKill<SplitMigrateSaga, SplitMigrateCancelled, CancelStateMigrationJobRequested, SplitCancelKillRequested, DummySplitCancelKillCompleted>(When(CancelStateMigrationJobRequested), _logger, CancelKillRequested, CancellingImmediateKill, Cancelled)
                .IfCancelAfterCurrent<SplitMigrateSaga, CancelStateMigrationJobRequested>(_logger, CancellingAfterCurrent),
            Ignore(RunnerReconnectedEvent),
            Ignore(HeartbeatScheduled.Received),
            Ignore(HeartbeatRequested.Completed),
            Ignore(HeartbeatRequested.Completed2)
        );

        During(SplitMigrateProvePending,
            ThenExtensions
                .Then<SplitMigrateSaga, SplitMigrateProveCompleted>(When(SplitMigrateProveCompleted), context => { context.Saga.ProvenModuleCount = context.Message.ModulesProven; })
                .IfElse(
                    context => context.Saga.StopAfterProve,
                    prove => prove
                        .Then(context => LoggerExtensions.LogInformation(_logger, "SplitProve: proof complete for job {JobId}", context.Saga.CorrelationId))
                        .ThenSplitCompleted(Completed),
                    migrate => DealWithApprovalStatus(migrate, true)),
            JobExtensionMethods
                .ThenHeartbeatScheduled<SplitMigrateSaga, StateMigrationHeartbeatRequested>(When(HeartbeatScheduled.Received), HeartbeatRequested),
            JobExtensionMethods
                .ThenHeartbeatCompleted<SplitMigrateSaga>(When(HeartbeatRequested.Completed), HeartbeatScheduled),
            SplitMigrateExtensionMethods.ThenSplitTimedOut(When(HeartbeatRequested.Completed2), Failed),
            JobExtensionMethods
                .IfCancelKill<SplitMigrateSaga, SplitMigrateCancelled, CancelStateMigrationJobRequested, SplitCancelKillRequested, DummySplitCancelKillCompleted>(When(CancelStateMigrationJobRequested), _logger, CancelKillRequested, CancellingImmediateKill, Cancelled)
                .IfCancelAfterCurrent<SplitMigrateSaga, CancelStateMigrationJobRequested>(_logger, CancellingAfterCurrent),
            SplitMigrateExtensionMethods
                .ThenSplitCancelled<SplitMigrateProveCancelled>(When(SplitMigrateProveCancelled), Cancelled),
            SplitMigrateExtensionMethods
                .ThenSplitFaulted<SplitMigrateProveFaulted>(When(SplitMigrateProveFaulted), Failed, _logger),
            Ignore(RunnerReconnectedEvent)
        );
    }
}
