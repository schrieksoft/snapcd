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
using SnapCd.Server.Core.Events.Steps.SplitMigrate;

namespace SnapCd.Server.Core.StateMachine.SplitMigrate;

public partial class SplitMigrateStateMachine
{
    // A step request that could not be dispatched, or a step result this machine could not handle, fails the job in any state.
    public Event<Fault<SelectRunnerInstanceRequested>> SplitSelectRunnerInstanceRequestFault { get; } = null!;
    public Event<Fault<SelectRunnerInstanceCompleted>> SplitSelectRunnerInstanceCompletedFault { get; } = null!;
    public Event<Fault<SplitGetModuleRequested>> SplitGetModuleRequestFault { get; } = null!;
    public Event<Fault<SplitGetModuleCompleted>> SplitGetModuleCompletedFault { get; } = null!;
    public Event<Fault<SplitInitRequested>> SplitInitRequestFault { get; } = null!;
    public Event<Fault<SplitInitCompleted>> SplitInitCompletedFault { get; } = null!;
    public Event<Fault<SplitValidateRequested>> SplitValidateRequestFault { get; } = null!;
    public Event<Fault<SplitValidateCompleted>> SplitValidateCompletedFault { get; } = null!;
    public Event<Fault<SplitPlanRequested>> SplitPlanRequestFault { get; } = null!;
    public Event<Fault<SplitPlanCompleted>> SplitPlanCompletedFault { get; } = null!;
    public Event<Fault<SplitPlanEmptyVerifyRequested>> SplitPlanEmptyVerifyRequestFault { get; } = null!;
    public Event<Fault<SplitPlanEmptyVerifyCompleted>> SplitPlanEmptyVerifyCompletedFault { get; } = null!;
    public Event<Fault<SplitRefactorValidateRequested>> SplitRefactorValidateRequestFault { get; } = null!;
    public Event<Fault<SplitRefactorValidateCompleted>> SplitRefactorValidateCompletedFault { get; } = null!;
    public Event<Fault<SplitRefactorDiffRequested>> SplitRefactorDiffRequestFault { get; } = null!;
    public Event<Fault<SplitRefactorDiffCompleted>> SplitRefactorDiffCompletedFault { get; } = null!;
    public Event<Fault<SplitMigrateMapRequested>> SplitMigrateMapRequestFault { get; } = null!;
    public Event<Fault<SplitMigrateMapCompleted>> SplitMigrateMapCompletedFault { get; } = null!;
    public Event<Fault<SplitMigrateProveRequested>> SplitMigrateProveRequestFault { get; } = null!;
    public Event<Fault<SplitMigrateProveCompleted>> SplitMigrateProveCompletedFault { get; } = null!;
    public Event<Fault<SplitMigrateRunRequested>> SplitMigrateRunRequestFault { get; } = null!;
    public Event<Fault<SplitMigrateRunCompleted>> SplitMigrateRunCompletedFault { get; } = null!;
    public Event<Fault<SplitMigrateVerifyRequested>> SplitMigrateVerifyRequestFault { get; } = null!;
    public Event<Fault<SplitMigrateVerifyCompleted>> SplitMigrateVerifyCompletedFault { get; } = null!;

    private void Configure_Faults()
    {
        Event(() => SplitSelectRunnerInstanceRequestFault, x => { x.CorrelateById(y => y.Message.Message.CorrelationId); x.OnMissingInstance(m => m.Discard()); });
        Event(() => SplitSelectRunnerInstanceCompletedFault, x => { x.CorrelateById(y => y.Message.Message.CorrelationId); x.OnMissingInstance(m => m.Discard()); });
        Event(() => SplitGetModuleRequestFault, x => { x.CorrelateById(y => y.Message.Message.CorrelationId); x.OnMissingInstance(m => m.Discard()); });
        Event(() => SplitGetModuleCompletedFault, x => { x.CorrelateById(y => y.Message.Message.CorrelationId); x.OnMissingInstance(m => m.Discard()); });
        Event(() => SplitInitRequestFault, x => { x.CorrelateById(y => y.Message.Message.CorrelationId); x.OnMissingInstance(m => m.Discard()); });
        Event(() => SplitInitCompletedFault, x => { x.CorrelateById(y => y.Message.Message.CorrelationId); x.OnMissingInstance(m => m.Discard()); });
        Event(() => SplitValidateRequestFault, x => { x.CorrelateById(y => y.Message.Message.CorrelationId); x.OnMissingInstance(m => m.Discard()); });
        Event(() => SplitValidateCompletedFault, x => { x.CorrelateById(y => y.Message.Message.CorrelationId); x.OnMissingInstance(m => m.Discard()); });
        Event(() => SplitPlanRequestFault, x => { x.CorrelateById(y => y.Message.Message.CorrelationId); x.OnMissingInstance(m => m.Discard()); });
        Event(() => SplitPlanCompletedFault, x => { x.CorrelateById(y => y.Message.Message.CorrelationId); x.OnMissingInstance(m => m.Discard()); });
        Event(() => SplitPlanEmptyVerifyRequestFault, x => { x.CorrelateById(y => y.Message.Message.CorrelationId); x.OnMissingInstance(m => m.Discard()); });
        Event(() => SplitPlanEmptyVerifyCompletedFault, x => { x.CorrelateById(y => y.Message.Message.CorrelationId); x.OnMissingInstance(m => m.Discard()); });
        Event(() => SplitRefactorValidateRequestFault, x => { x.CorrelateById(y => y.Message.Message.CorrelationId); x.OnMissingInstance(m => m.Discard()); });
        Event(() => SplitRefactorValidateCompletedFault, x => { x.CorrelateById(y => y.Message.Message.CorrelationId); x.OnMissingInstance(m => m.Discard()); });
        Event(() => SplitRefactorDiffRequestFault, x => { x.CorrelateById(y => y.Message.Message.CorrelationId); x.OnMissingInstance(m => m.Discard()); });
        Event(() => SplitRefactorDiffCompletedFault, x => { x.CorrelateById(y => y.Message.Message.CorrelationId); x.OnMissingInstance(m => m.Discard()); });
        Event(() => SplitMigrateMapRequestFault, x => { x.CorrelateById(y => y.Message.Message.CorrelationId); x.OnMissingInstance(m => m.Discard()); });
        Event(() => SplitMigrateMapCompletedFault, x => { x.CorrelateById(y => y.Message.Message.CorrelationId); x.OnMissingInstance(m => m.Discard()); });
        Event(() => SplitMigrateProveRequestFault, x => { x.CorrelateById(y => y.Message.Message.CorrelationId); x.OnMissingInstance(m => m.Discard()); });
        Event(() => SplitMigrateProveCompletedFault, x => { x.CorrelateById(y => y.Message.Message.CorrelationId); x.OnMissingInstance(m => m.Discard()); });
        Event(() => SplitMigrateRunRequestFault, x => { x.CorrelateById(y => y.Message.Message.CorrelationId); x.OnMissingInstance(m => m.Discard()); });
        Event(() => SplitMigrateRunCompletedFault, x => { x.CorrelateById(y => y.Message.Message.CorrelationId); x.OnMissingInstance(m => m.Discard()); });
        Event(() => SplitMigrateVerifyRequestFault, x => { x.CorrelateById(y => y.Message.Message.CorrelationId); x.OnMissingInstance(m => m.Discard()); });
        Event(() => SplitMigrateVerifyCompletedFault, x => { x.CorrelateById(y => y.Message.Message.CorrelationId); x.OnMissingInstance(m => m.Discard()); });

        DuringAny(
            When(SplitSelectRunnerInstanceRequestFault).ThenSplitFaulted(Failed, _logger),
            When(SplitSelectRunnerInstanceCompletedFault).ThenSplitFaulted(Failed, _logger),
            When(SplitGetModuleRequestFault).ThenSplitFaulted(Failed, _logger),
            When(SplitGetModuleCompletedFault).ThenSplitFaulted(Failed, _logger),
            When(SplitInitRequestFault).ThenSplitFaulted(Failed, _logger),
            When(SplitInitCompletedFault).ThenSplitFaulted(Failed, _logger),
            When(SplitValidateRequestFault).ThenSplitFaulted(Failed, _logger),
            When(SplitValidateCompletedFault).ThenSplitFaulted(Failed, _logger),
            When(SplitPlanRequestFault).ThenSplitFaulted(Failed, _logger),
            When(SplitPlanCompletedFault).ThenSplitFaulted(Failed, _logger),
            When(SplitPlanEmptyVerifyRequestFault).ThenSplitFaulted(Failed, _logger),
            When(SplitPlanEmptyVerifyCompletedFault).ThenSplitFaulted(Failed, _logger),
            When(SplitRefactorValidateRequestFault).ThenSplitFaulted(Failed, _logger),
            When(SplitRefactorValidateCompletedFault).ThenSplitFaulted(Failed, _logger),
            When(SplitRefactorDiffRequestFault).ThenSplitFaulted(Failed, _logger),
            When(SplitRefactorDiffCompletedFault).ThenSplitFaulted(Failed, _logger),
            When(SplitMigrateMapRequestFault).ThenSplitFaulted(Failed, _logger),
            When(SplitMigrateMapCompletedFault).ThenSplitFaulted(Failed, _logger),
            When(SplitMigrateProveRequestFault).ThenSplitFaulted(Failed, _logger),
            When(SplitMigrateProveCompletedFault).ThenSplitFaulted(Failed, _logger),
            When(SplitMigrateRunRequestFault).ThenSplitFaulted(Failed, _logger),
            When(SplitMigrateRunCompletedFault).ThenSplitFaulted(Failed, _logger),
            When(SplitMigrateVerifyRequestFault).ThenSplitFaulted(Failed, _logger),
            When(SplitMigrateVerifyCompletedFault).ThenSplitFaulted(Failed, _logger)
        );
    }
}
