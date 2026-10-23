// SPDX-License-Identifier: LicenseRef-Snap-CD-Source-Available-1.1
// Copyright (c) 2026 Karl Schriek / Schrieksoft.
// No license is granted to use this file, in whole or in part, (a) as training, fine-tuning, retrieval, or
// embedding data for any machine-learning model, or (b) as input to any machine-learning model, agent, or automated
// system for the purpose of producing a derivative work or reimplementation that is not otherwise permitted by the
// Snap CD Source-Available License (including any Competing Product as defined therein). Contact info@snapcd.io
// for terms covering either use.

using MassTransit;
using SnapCd.Server.Core.Entities.Sagas;
using SnapCd.Server.Core.Events.Steps;
using SnapCd.Server.Core.Events.Steps.Base;
using SnapCd.Server.Core.Events.Steps.StateMigrations;
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
    // A step request that could not be dispatched, or a step result this machine could not handle, fails the job in any state.
    public Event<Fault<TSelectRunnerInstanceRequested>> SelectRunnerInstanceRequestFault { get; } = null!;
    public Event<Fault<TSelectRunnerInstanceCompleted>> SelectRunnerInstanceCompletedFault { get; } = null!;
    public Event<Fault<TGetModuleRequested>> GetModuleRequestFault { get; } = null!;
    public Event<Fault<TGetModuleCompleted>> GetModuleCompletedFault { get; } = null!;
    public Event<Fault<TInitRequested>> InitRequestFault { get; } = null!;
    public Event<Fault<TInitCompleted>> InitCompletedFault { get; } = null!;
    public Event<Fault<TPreCheckRequested>> PreCheckRequestFault { get; } = null!;
    public Event<Fault<TPreCheckCompleted>> PreCheckCompletedFault { get; } = null!;
    public Event<Fault<TMigrateStateRequested>> MigrateStateRequestFault { get; } = null!;
    public Event<Fault<TMigrateStateCompleted>> MigrateStateCompletedFault { get; } = null!;
    public Event<Fault<LookupAddressesRequested>> ListRequestFault { get; } = null!;
    public Event<Fault<LookupAddressesCompleted>> ListCompletedFault { get; } = null!;

    private void Configure_Faults()
    {
        Event(() => SelectRunnerInstanceRequestFault, x => { x.CorrelateById(y => y.Message.Message.CorrelationId); x.OnMissingInstance(m => m.Discard()); });
        Event(() => SelectRunnerInstanceCompletedFault, x => { x.CorrelateById(y => y.Message.Message.CorrelationId); x.OnMissingInstance(m => m.Discard()); });
        Event(() => GetModuleRequestFault, x => { x.CorrelateById(y => y.Message.Message.CorrelationId); x.OnMissingInstance(m => m.Discard()); });
        Event(() => GetModuleCompletedFault, x => { x.CorrelateById(y => y.Message.Message.CorrelationId); x.OnMissingInstance(m => m.Discard()); });
        Event(() => InitRequestFault, x => { x.CorrelateById(y => y.Message.Message.CorrelationId); x.OnMissingInstance(m => m.Discard()); });
        Event(() => InitCompletedFault, x => { x.CorrelateById(y => y.Message.Message.CorrelationId); x.OnMissingInstance(m => m.Discard()); });
        Event(() => PreCheckRequestFault, x => { x.CorrelateById(y => y.Message.Message.CorrelationId); x.OnMissingInstance(m => m.Discard()); });
        Event(() => PreCheckCompletedFault, x => { x.CorrelateById(y => y.Message.Message.CorrelationId); x.OnMissingInstance(m => m.Discard()); });
        Event(() => MigrateStateRequestFault, x => { x.CorrelateById(y => y.Message.Message.CorrelationId); x.OnMissingInstance(m => m.Discard()); });
        Event(() => MigrateStateCompletedFault, x => { x.CorrelateById(y => y.Message.Message.CorrelationId); x.OnMissingInstance(m => m.Discard()); });
        Event(() => ListRequestFault, x => { x.CorrelateById(y => y.Message.Message.CorrelationId); x.OnMissingInstance(m => m.Discard()); });
        Event(() => ListCompletedFault, x => { x.CorrelateById(y => y.Message.Message.CorrelationId); x.OnMissingInstance(m => m.Discard()); });

        DuringAny(
            When(SelectRunnerInstanceRequestFault).ThenJobFailed().TransitionTo(Failed).Finalize(),
            When(SelectRunnerInstanceCompletedFault).ThenJobFailed().TransitionTo(Failed).Finalize(),
            When(GetModuleRequestFault).ThenJobFailed().TransitionTo(Failed).Finalize(),
            When(GetModuleCompletedFault).ThenJobFailed().TransitionTo(Failed).Finalize(),
            When(InitRequestFault).ThenJobFailed().TransitionTo(Failed).Finalize(),
            When(InitCompletedFault).ThenJobFailed().TransitionTo(Failed).Finalize(),
            When(PreCheckRequestFault).ThenJobFailed().TransitionTo(Failed).Finalize(),
            When(PreCheckCompletedFault).ThenJobFailed().TransitionTo(Failed).Finalize(),
            When(MigrateStateRequestFault).ThenJobFailed().TransitionTo(Failed).Finalize(),
            When(MigrateStateCompletedFault).ThenJobFailed().TransitionTo(Failed).Finalize(),
            When(ListRequestFault).ThenJobFailed().TransitionTo(Failed).Finalize(),
            When(ListCompletedFault).ThenJobFailed().TransitionTo(Failed).Finalize()
        );
    }
}
