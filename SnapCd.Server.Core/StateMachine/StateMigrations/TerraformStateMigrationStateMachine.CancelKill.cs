// SPDX-License-Identifier: LicenseRef-Snap-CD-Source-Available-1.1
// Copyright (c) 2026 Karl Schriek / Schrieksoft.
// No license is granted to use this file, in whole or in part, (a) as training, fine-tuning, retrieval, or
// embedding data for any machine-learning model, or (b) as input to any machine-learning model, agent, or automated
// system for the purpose of producing a derivative work or reimplementation that is not otherwise permitted by the
// Snap CD Source-Available License (including any Competing Product as defined therein). Contact info@snapcd.io
// for terms covering either use.

using MassTransit;
using MassTransit.Contracts;
using Microsoft.Extensions.Logging;
using SnapCd.Server.Core.Entities.Sagas;
using SnapCd.Server.Core.Events.Jobs.Module;
using SnapCd.Server.Core.Events.Runners;
using SnapCd.Server.Core.Events.Steps;
using SnapCd.Server.Core.Misc.Helpers;
using SnapCd.Server.Core.StateMachine.Jobs.Utils;
using SnapCd.Server.Core.StateMachine.StateMigrations.Finalization;

namespace SnapCd.Server.Core.StateMachine.StateMigrations;

public abstract partial class TerraformStateMigrationStateMachine<
    TSaga, TJobRequested, TApproved,
    TSelectRunnerInstanceRequested, TGetModuleRequested, TInitRequested,
    TSelectRunnerInstanceCompleted, TSelectRunnerInstanceCancelled, TSelectRunnerInstanceFaulted,
    TGetModuleCompleted, TGetModuleCancelled, TGetModuleFaulted,
    TInitCompleted, TInitCancelled, TInitFaulted,
    TPreCheckRequested, TPreCheckCompleted, TPreCheckCancelled, TPreCheckFaulted,
    TMigrateStateRequested, TMigrateStateCompleted, TMigrateStateCancelled, TMigrateStateFaulted,
    TCancelKillRequested, TDummyCancelKillCompleted, TCancelKillCompleted>
{
    public Request<TSaga, TCancelKillRequested, TDummyCancelKillCompleted> CancelKillRequested { get; } = null!;

    public Event<TCancelKillCompleted> CancelKillCompleted { get; } = null!;

    public State CancellingImmediateKill { get; } = null!;

    public State Cancelled { get; } = null!;

    private static readonly TimeSpan CancelRequestTimeout = TimeSpan.FromSeconds(90);

    /// <summary>
    /// The write is the one step that cannot be abandoned by ignoring it: the addresses are being
    /// written as the request arrives, so stopping means telling the runner to kill the command.
    /// The job ends as cancelled with whatever landed before the kill, which the list that follows
    /// would otherwise have reported.
    /// </summary>
    private void Configure_CancelKill()
    {
        Request(() => CancelKillRequested, x => x.KillCancellationRequestId,
            o => { o.Timeout = CancelRequestTimeout; });

        Event(() => CancelKillCompleted, x => x.CorrelateById(y => y.Message.CorrelationId));

        During(MigrateStatePending,
            When(CancelRequested)
                .Then(context => _logger.LogInformation(
                    "{Verb} on Module {ModuleId} was cancelled while writing; killing the command",
                    Verb, context.Saga.ModuleId))
                .IfCancelKill<TSaga, TCancelKillRequested, TDummyCancelKillCompleted>(
                    CancelKillRequested, CancellingImmediateKill));

        During(CancellingImmediateKill,
            // Whichever arrives first ends the job: the kill's own acknowledgement, or the write
            // reporting back because it finished or was stopped.
            When(CancelKillCompleted).ThenStateMigrationCancelled<TSaga, TCancelKillCompleted>(Cancelled),
            When(MigrateStateCancelled).ThenStateMigrationCancelled<TSaga, TMigrateStateCancelled>(Cancelled),
            When(MigrateStateCompleted).ThenStateMigrationCancelled<TSaga, TMigrateStateCompleted>(Cancelled),
            When(MigrateStateFaulted).ThenStateMigrationCancelled<TSaga, TMigrateStateFaulted>(Cancelled),

            // A runner that never answers would otherwise leave the saga here for good.
            When(CancelKillRequested.TimeoutExpired)
                .Then(context => _logger.LogWarning(
                    "{Verb} on Module {ModuleId}: the kill was not acknowledged; ending the job anyway",
                    Verb, context.Saga.ModuleId))
                .ThenStateMigrationCancelled<TSaga, RequestTimeoutExpired<TCancelKillRequested>>(Cancelled),

            Ignore(CancelRequested),
            Ignore(RunnerReconnectedEvent),
            Ignore(HeartbeatScheduled.Received),
            Ignore(HeartbeatRequested.Completed),
            Ignore(HeartbeatRequested.Completed2));

        During(Cancelled,
            Ignore(CancelRequested),
            Ignore(RunnerReconnectedEvent),
            Ignore(HeartbeatScheduled.Received),
            Ignore(HeartbeatRequested.Completed),
            Ignore(HeartbeatRequested.Completed2));
    }
}
