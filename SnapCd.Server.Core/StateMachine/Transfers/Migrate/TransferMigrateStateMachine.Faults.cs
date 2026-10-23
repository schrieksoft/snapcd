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
using SnapCd.Server.Core.Events.Steps.Transfer;

namespace SnapCd.Server.Core.StateMachine.Transfers.Migrate;

public partial class TransferMigrateStateMachine
{
    // A step request that could not be dispatched, or a step result this machine could not handle, fails the job in any state.
    public Event<Fault<TransferSelectRunnerInstanceRequested>> TransferSelectRunnerInstanceRequestFault { get; } = null!;
    public Event<Fault<TransferSelectRunnerInstanceCompleted>> TransferSelectRunnerInstanceCompletedFault { get; } = null!;
    public Event<Fault<TransferGetModuleRequested>> TransferGetModuleRequestFault { get; } = null!;
    public Event<Fault<TransferGetModuleCompleted>> TransferGetModuleCompletedFault { get; } = null!;
    public Event<Fault<TransferInitRequested>> TransferInitRequestFault { get; } = null!;
    public Event<Fault<TransferInitCompleted>> TransferInitCompletedFault { get; } = null!;
    public Event<Fault<TransferValidateRequested>> TransferValidateRequestFault { get; } = null!;
    public Event<Fault<TransferValidateCompleted>> TransferValidateCompletedFault { get; } = null!;
    public Event<Fault<AnalyseTransferRefactorMapRequested>> AnalyseTransferRefactorMapRequestFault { get; } = null!;
    public Event<Fault<AnalyseTransferRefactorMapCompleted>> AnalyseTransferRefactorMapCompletedFault { get; } = null!;
    public Event<Fault<TransferMigrateMapRequested>> TransferMigrateMapRequestFault { get; } = null!;
    public Event<Fault<TransferMigrateMapCompleted>> TransferMigrateMapCompletedFault { get; } = null!;
    public Event<Fault<TransferMigrateProveRequested>> TransferMigrateProveRequestFault { get; } = null!;
    public Event<Fault<TransferMigrateProveCompleted>> TransferMigrateProveCompletedFault { get; } = null!;
    public Event<Fault<TransferMigrateRunRequested>> TransferMigrateRunRequestFault { get; } = null!;
    public Event<Fault<TransferMigrateRunCompleted>> TransferMigrateRunCompletedFault { get; } = null!;
    public Event<Fault<TransferMigrateVerifyRequested>> TransferMigrateVerifyRequestFault { get; } = null!;
    public Event<Fault<TransferMigrateVerifyCompleted>> TransferMigrateVerifyCompletedFault { get; } = null!;

    private void Configure_Faults()
    {
        Event(() => TransferSelectRunnerInstanceRequestFault, x => { x.CorrelateById(y => y.Message.Message.CorrelationId); x.OnMissingInstance(m => m.Discard()); });
        Event(() => TransferSelectRunnerInstanceCompletedFault, x => { x.CorrelateById(y => y.Message.Message.CorrelationId); x.OnMissingInstance(m => m.Discard()); });
        Event(() => TransferGetModuleRequestFault, x => { x.CorrelateById(y => y.Message.Message.CorrelationId); x.OnMissingInstance(m => m.Discard()); });
        Event(() => TransferGetModuleCompletedFault, x => { x.CorrelateById(y => y.Message.Message.CorrelationId); x.OnMissingInstance(m => m.Discard()); });
        Event(() => TransferInitRequestFault, x => { x.CorrelateById(y => y.Message.Message.CorrelationId); x.OnMissingInstance(m => m.Discard()); });
        Event(() => TransferInitCompletedFault, x => { x.CorrelateById(y => y.Message.Message.CorrelationId); x.OnMissingInstance(m => m.Discard()); });
        Event(() => TransferValidateRequestFault, x => { x.CorrelateById(y => y.Message.Message.CorrelationId); x.OnMissingInstance(m => m.Discard()); });
        Event(() => TransferValidateCompletedFault, x => { x.CorrelateById(y => y.Message.Message.CorrelationId); x.OnMissingInstance(m => m.Discard()); });
        Event(() => AnalyseTransferRefactorMapRequestFault, x => { x.CorrelateById(y => y.Message.Message.CorrelationId); x.OnMissingInstance(m => m.Discard()); });
        Event(() => AnalyseTransferRefactorMapCompletedFault, x => { x.CorrelateById(y => y.Message.Message.CorrelationId); x.OnMissingInstance(m => m.Discard()); });
        Event(() => TransferMigrateMapRequestFault, x => { x.CorrelateById(y => y.Message.Message.CorrelationId); x.OnMissingInstance(m => m.Discard()); });
        Event(() => TransferMigrateMapCompletedFault, x => { x.CorrelateById(y => y.Message.Message.CorrelationId); x.OnMissingInstance(m => m.Discard()); });
        Event(() => TransferMigrateProveRequestFault, x => { x.CorrelateById(y => y.Message.Message.CorrelationId); x.OnMissingInstance(m => m.Discard()); });
        Event(() => TransferMigrateProveCompletedFault, x => { x.CorrelateById(y => y.Message.Message.CorrelationId); x.OnMissingInstance(m => m.Discard()); });
        Event(() => TransferMigrateRunRequestFault, x => { x.CorrelateById(y => y.Message.Message.CorrelationId); x.OnMissingInstance(m => m.Discard()); });
        Event(() => TransferMigrateRunCompletedFault, x => { x.CorrelateById(y => y.Message.Message.CorrelationId); x.OnMissingInstance(m => m.Discard()); });
        Event(() => TransferMigrateVerifyRequestFault, x => { x.CorrelateById(y => y.Message.Message.CorrelationId); x.OnMissingInstance(m => m.Discard()); });
        Event(() => TransferMigrateVerifyCompletedFault, x => { x.CorrelateById(y => y.Message.Message.CorrelationId); x.OnMissingInstance(m => m.Discard()); });

        DuringAny(
            When(TransferSelectRunnerInstanceRequestFault).ThenJobFailed().TransitionTo(Failed).Finalize(),
            When(TransferSelectRunnerInstanceCompletedFault).ThenJobFailed().TransitionTo(Failed).Finalize(),
            When(TransferGetModuleRequestFault).ThenJobFailed().TransitionTo(Failed).Finalize(),
            When(TransferGetModuleCompletedFault).ThenJobFailed().TransitionTo(Failed).Finalize(),
            When(TransferInitRequestFault).ThenJobFailed().TransitionTo(Failed).Finalize(),
            When(TransferInitCompletedFault).ThenJobFailed().TransitionTo(Failed).Finalize(),
            When(TransferValidateRequestFault).ThenJobFailed().TransitionTo(Failed).Finalize(),
            When(TransferValidateCompletedFault).ThenJobFailed().TransitionTo(Failed).Finalize(),
            When(AnalyseTransferRefactorMapRequestFault).ThenJobFailed().TransitionTo(Failed).Finalize(),
            When(AnalyseTransferRefactorMapCompletedFault).ThenJobFailed().TransitionTo(Failed).Finalize(),
            When(TransferMigrateMapRequestFault).ThenJobFailed().TransitionTo(Failed).Finalize(),
            When(TransferMigrateMapCompletedFault).ThenJobFailed().TransitionTo(Failed).Finalize(),
            When(TransferMigrateProveRequestFault).ThenJobFailed().TransitionTo(Failed).Finalize(),
            When(TransferMigrateProveCompletedFault).ThenJobFailed().TransitionTo(Failed).Finalize(),
            When(TransferMigrateRunRequestFault).ThenJobFailed().TransitionTo(Failed).Finalize(),
            When(TransferMigrateRunCompletedFault).ThenJobFailed().TransitionTo(Failed).Finalize(),
            When(TransferMigrateVerifyRequestFault).ThenJobFailed().TransitionTo(Failed).Finalize(),
            When(TransferMigrateVerifyCompletedFault).ThenJobFailed().TransitionTo(Failed).Finalize()
        );
    }
}
