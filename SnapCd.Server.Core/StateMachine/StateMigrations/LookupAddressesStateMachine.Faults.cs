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

public partial class LookupAddressesStateMachine
{
    // A step request that could not be dispatched, or a step result this machine could not handle, fails the job in any state.
    public Event<Fault<LookupAddressesSelectRunnerInstanceRequested>> LookupAddressesSelectRunnerInstanceRequestFault { get; } = null!;
    public Event<Fault<LookupAddressesSelectRunnerInstanceCompleted>> LookupAddressesSelectRunnerInstanceCompletedFault { get; } = null!;
    public Event<Fault<LookupAddressesGetModuleRequested>> LookupAddressesGetModuleRequestFault { get; } = null!;
    public Event<Fault<LookupAddressesGetModuleCompleted>> LookupAddressesGetModuleCompletedFault { get; } = null!;
    public Event<Fault<LookupAddressesInitRequested>> LookupAddressesInitRequestFault { get; } = null!;
    public Event<Fault<LookupAddressesInitCompleted>> LookupAddressesInitCompletedFault { get; } = null!;
    public Event<Fault<LookupAddressesRequested>> LookupAddressesRequestFault { get; } = null!;
    public Event<Fault<LookupAddressesCompleted>> LookupAddressesCompletedFault { get; } = null!;

    private void Configure_Faults()
    {
        Event(() => LookupAddressesSelectRunnerInstanceRequestFault, x => { x.CorrelateById(y => y.Message.Message.CorrelationId); x.OnMissingInstance(m => m.Discard()); });
        Event(() => LookupAddressesSelectRunnerInstanceCompletedFault, x => { x.CorrelateById(y => y.Message.Message.CorrelationId); x.OnMissingInstance(m => m.Discard()); });
        Event(() => LookupAddressesGetModuleRequestFault, x => { x.CorrelateById(y => y.Message.Message.CorrelationId); x.OnMissingInstance(m => m.Discard()); });
        Event(() => LookupAddressesGetModuleCompletedFault, x => { x.CorrelateById(y => y.Message.Message.CorrelationId); x.OnMissingInstance(m => m.Discard()); });
        Event(() => LookupAddressesInitRequestFault, x => { x.CorrelateById(y => y.Message.Message.CorrelationId); x.OnMissingInstance(m => m.Discard()); });
        Event(() => LookupAddressesInitCompletedFault, x => { x.CorrelateById(y => y.Message.Message.CorrelationId); x.OnMissingInstance(m => m.Discard()); });
        Event(() => LookupAddressesRequestFault, x => { x.CorrelateById(y => y.Message.Message.CorrelationId); x.OnMissingInstance(m => m.Discard()); });
        Event(() => LookupAddressesCompletedFault, x => { x.CorrelateById(y => y.Message.Message.CorrelationId); x.OnMissingInstance(m => m.Discard()); });

        DuringAny(
            When(LookupAddressesSelectRunnerInstanceRequestFault).ThenJobFailed().TransitionTo(Failed).Finalize(),
            When(LookupAddressesSelectRunnerInstanceCompletedFault).ThenJobFailed().TransitionTo(Failed).Finalize(),
            When(LookupAddressesGetModuleRequestFault).ThenJobFailed().TransitionTo(Failed).Finalize(),
            When(LookupAddressesGetModuleCompletedFault).ThenJobFailed().TransitionTo(Failed).Finalize(),
            When(LookupAddressesInitRequestFault).ThenJobFailed().TransitionTo(Failed).Finalize(),
            When(LookupAddressesInitCompletedFault).ThenJobFailed().TransitionTo(Failed).Finalize(),
            When(LookupAddressesRequestFault).ThenJobFailed().TransitionTo(Failed).Finalize(),
            When(LookupAddressesCompletedFault).ThenJobFailed().TransitionTo(Failed).Finalize()
        );
    }
}
