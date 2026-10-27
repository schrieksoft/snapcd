// SPDX-License-Identifier: LicenseRef-Snap-CD-Source-Available-1.1
// Copyright (c) 2026 Karl Schriek / Schrieksoft.
// No license is granted to use this file, in whole or in part, (a) as training, fine-tuning, retrieval, or
// embedding data for any machine-learning model, or (b) as input to any machine-learning model, agent, or automated
// system for the purpose of producing a derivative work or reimplementation that is not otherwise permitted by the
// Snap CD Source-Available License (including any Competing Product as defined therein). Contact info@snapcd.io
// for terms covering either use.

using MassTransit;
using SnapCd.Server.Core.Entities.Sagas.Base;
using SnapCd.Server.Core.Events.Jobs.Base;
using SnapCd.Server.Core.Events.Steps;
using SnapCd.Server.Core.Events.Steps.Base;
using SnapCd.Server.Core.StateMachine.Jobs.Utils;

namespace SnapCd.Server.Core.StateMachine.Jobs;

public partial class JobStateMachine<
    TSaga,
    TRequest,
    TResponseFailed,
    TResponseCompleted,
    TResponseCancelled,
    TGetDefinitiveRevisionRequested,
    TGetDefinitiveRevisionCompleted,
    TGetDefinitiveRevisionCancelled,
    TGetDefinitiveRevisionFaulted,
    TPolicyValidateRequested,
    TPolicyValidateCompleted,
    TPolicyValidateCancelled,
    TPolicyValidateFaulted,
    TOutputRequested,
    TOutputCompleted,
    TOutputCancelled,
    TOutputFaulted,
    TGetModuleRequested,
    TGetModuleCompleted,
    TGetModuleCancelled,
    TGetModuleFaulted,
    TInitRequested,
    TInitCompleted,
    TInitCancelled,
    TInitFaulted,
    TValidateRequested,
    TValidateCompleted,
    TValidateCancelled,
    TValidateFaulted,
    TVariablesRequested,
    TVariablesCompleted,
    TVariablesCancelled,
    TVariablesFaulted,
    TPlanRequested,
    TPlanCompleted,
    TPlanCancelled,
    TPlanFaulted,
    TApplyFromPlanRequested,
    TApplyFromPlanCompleted,
    TApplyFromPlanCancelled,
    TProbeRequested,
    TProbeCompleted,
    TProbeCancelled,
    TProbeFaulted,
    TApplyFromPlanFaulted,
    TCancelKillRequested,
    TDummyCancelKillCompleted,
    TCancelKillCompleted>
    where TSaga : JobSagaBase
    where TRequest : ModuleJobEventBase
    where TResponseFailed : ModuleJobEventCompletedBase, new()
    where TResponseCompleted : ModuleJobEventCompletedBase, new()
    where TResponseCancelled : ModuleJobEventCompletedBase, new()
    where TGetDefinitiveRevisionRequested : GetDefinitiveRevisionRequestedBase, new()
    where TGetDefinitiveRevisionCompleted : GetDefinitiveRevisionCompletedBase
    where TGetDefinitiveRevisionCancelled : StepResponseBase
    where TGetDefinitiveRevisionFaulted : StepFaultedBase
    where TPolicyValidateRequested : PolicyValidateRequestedBase, new()
    where TPolicyValidateCompleted : PolicyValidateCompletedBase
    where TPolicyValidateCancelled : StepResponseBase
    where TPolicyValidateFaulted : StepFaultedBase
    where TOutputRequested : OutputRequestedBase, new()
    where TOutputCompleted : StepResponseBase
    where TOutputCancelled : StepResponseBase
    where TOutputFaulted : StepFaultedBase
    where TGetModuleRequested : GetModuleRequestedBase, new()
    where TGetModuleCompleted : StepResponseBase
    where TGetModuleCancelled : StepResponseBase
    where TGetModuleFaulted : StepFaultedBase
    where TInitRequested : InitRequestedBase, new()
    where TInitCompleted : StepResponseBase
    where TInitCancelled : StepResponseBase
    where TInitFaulted : StepFaultedBase
    where TValidateRequested : ValidateRequestedBase, new()
    where TValidateCompleted : StepResponseBase
    where TValidateCancelled : StepResponseBase
    where TValidateFaulted : StepFaultedBase
    where TVariablesRequested : VariablesRequestedBase, new()
    where TVariablesCompleted : StepResponseBase
    where TVariablesCancelled : StepResponseBase
    where TVariablesFaulted : StepFaultedBase
    where TPlanRequested : StepRequestBase, new()
    where TPlanCompleted : PlanCompletedBase
    where TPlanCancelled : StepResponseBase
    where TPlanFaulted : StepFaultedBase, new()
    where TApplyFromPlanRequested : StepRequestBase, new()
    where TApplyFromPlanCompleted : ApplyResponseBase
    where TApplyFromPlanCancelled : StepResponseBase
    where TProbeRequested : ProbeRequestedBase, new()
    where TProbeCompleted : ProbeCompletedBase
    where TProbeCancelled : StepResponseBase
    where TProbeFaulted : StepFaultedBase
    where TApplyFromPlanFaulted : FromPlanFaultedBase
    where TCancelKillRequested : CancelKillRequestedBase, new()
    where TDummyCancelKillCompleted : class
    where TCancelKillCompleted : StepResponseBase
{
    // A step request that could not be dispatched, or a step result this machine could not handle, fails the job in any state.
    public Event<Fault<SelectRunnerInstanceRequested>> SelectRunnerInstanceRequestFault { get; } = null!;
    public Event<Fault<SelectRunnerInstanceCompleted>> SelectRunnerInstanceCompletedFault { get; } = null!;
    public Event<Fault<TGetDefinitiveRevisionRequested>> GetDefinitiveRevisionRequestFault { get; } = null!;
    public Event<Fault<TGetDefinitiveRevisionCompleted>> GetDefinitiveRevisionCompletedFault { get; } = null!;
    public Event<Fault<TGetModuleRequested>> GetModuleRequestFault { get; } = null!;
    public Event<Fault<TGetModuleCompleted>> GetModuleCompletedFault { get; } = null!;
    public Event<Fault<TInitRequested>> InitRequestFault { get; } = null!;
    public Event<Fault<TInitCompleted>> InitCompletedFault { get; } = null!;
    public Event<Fault<TValidateRequested>> ValidateRequestFault { get; } = null!;
    public Event<Fault<TValidateCompleted>> ValidateCompletedFault { get; } = null!;
    public Event<Fault<TVariablesRequested>> VariablesRequestFault { get; } = null!;
    public Event<Fault<TVariablesCompleted>> VariablesCompletedFault { get; } = null!;
    public Event<Fault<TPolicyValidateRequested>> PolicyValidateRequestFault { get; } = null!;
    public Event<Fault<TPolicyValidateCompleted>> PolicyValidateCompletedFault { get; } = null!;
    public Event<Fault<TPlanRequested>> PlanRequestFault { get; } = null!;
    public Event<Fault<TPlanCompleted>> PlanCompletedFault { get; } = null!;
    public Event<Fault<TApplyFromPlanRequested>> ApplyFromPlanRequestFault { get; } = null!;
    public Event<Fault<TApplyFromPlanCompleted>> ApplyFromPlanCompletedFault { get; } = null!;
    public Event<Fault<TProbeRequested>> ProbeRequestFault { get; } = null!;
    public Event<Fault<TProbeCompleted>> ProbeCompletedFault { get; } = null!;
    public Event<Fault<TOutputRequested>> OutputRequestFault { get; } = null!;
    public Event<Fault<TOutputCompleted>> OutputCompletedFault { get; } = null!;

    private void Configure_Faults()
    {
        Event(() => SelectRunnerInstanceRequestFault, x => { x.CorrelateById(y => y.Message.Message.CorrelationId); x.OnMissingInstance(m => m.Discard()); });
        Event(() => SelectRunnerInstanceCompletedFault, x => { x.CorrelateById(y => y.Message.Message.CorrelationId); x.OnMissingInstance(m => m.Discard()); });
        Event(() => GetDefinitiveRevisionRequestFault, x => { x.CorrelateById(y => y.Message.Message.CorrelationId); x.OnMissingInstance(m => m.Discard()); });
        Event(() => GetDefinitiveRevisionCompletedFault, x => { x.CorrelateById(y => y.Message.Message.CorrelationId); x.OnMissingInstance(m => m.Discard()); });
        Event(() => GetModuleRequestFault, x => { x.CorrelateById(y => y.Message.Message.CorrelationId); x.OnMissingInstance(m => m.Discard()); });
        Event(() => GetModuleCompletedFault, x => { x.CorrelateById(y => y.Message.Message.CorrelationId); x.OnMissingInstance(m => m.Discard()); });
        Event(() => InitRequestFault, x => { x.CorrelateById(y => y.Message.Message.CorrelationId); x.OnMissingInstance(m => m.Discard()); });
        Event(() => InitCompletedFault, x => { x.CorrelateById(y => y.Message.Message.CorrelationId); x.OnMissingInstance(m => m.Discard()); });
        Event(() => ValidateRequestFault, x => { x.CorrelateById(y => y.Message.Message.CorrelationId); x.OnMissingInstance(m => m.Discard()); });
        Event(() => ValidateCompletedFault, x => { x.CorrelateById(y => y.Message.Message.CorrelationId); x.OnMissingInstance(m => m.Discard()); });
        Event(() => VariablesRequestFault, x => { x.CorrelateById(y => y.Message.Message.CorrelationId); x.OnMissingInstance(m => m.Discard()); });
        Event(() => VariablesCompletedFault, x => { x.CorrelateById(y => y.Message.Message.CorrelationId); x.OnMissingInstance(m => m.Discard()); });
        Event(() => PolicyValidateRequestFault, x => { x.CorrelateById(y => y.Message.Message.CorrelationId); x.OnMissingInstance(m => m.Discard()); });
        Event(() => PolicyValidateCompletedFault, x => { x.CorrelateById(y => y.Message.Message.CorrelationId); x.OnMissingInstance(m => m.Discard()); });
        Event(() => PlanRequestFault, x => { x.CorrelateById(y => y.Message.Message.CorrelationId); x.OnMissingInstance(m => m.Discard()); });
        Event(() => PlanCompletedFault, x => { x.CorrelateById(y => y.Message.Message.CorrelationId); x.OnMissingInstance(m => m.Discard()); });
        Event(() => ApplyFromPlanRequestFault, x => { x.CorrelateById(y => y.Message.Message.CorrelationId); x.OnMissingInstance(m => m.Discard()); });
        Event(() => ApplyFromPlanCompletedFault, x => { x.CorrelateById(y => y.Message.Message.CorrelationId); x.OnMissingInstance(m => m.Discard()); });
        Event(() => ProbeRequestFault, x => { x.CorrelateById(y => y.Message.Message.CorrelationId); x.OnMissingInstance(m => m.Discard()); });
        Event(() => ProbeCompletedFault, x => { x.CorrelateById(y => y.Message.Message.CorrelationId); x.OnMissingInstance(m => m.Discard()); });
        Event(() => OutputRequestFault, x => { x.CorrelateById(y => y.Message.Message.CorrelationId); x.OnMissingInstance(m => m.Discard()); });
        Event(() => OutputCompletedFault, x => { x.CorrelateById(y => y.Message.Message.CorrelationId); x.OnMissingInstance(m => m.Discard()); });

        DuringAny(
            When(SelectRunnerInstanceRequestFault).ThenFaultedOnFault<TSaga, TResponseFailed, SelectRunnerInstanceRequested>(Failed, _logger),
            When(SelectRunnerInstanceCompletedFault).ThenFaultedOnFault<TSaga, TResponseFailed, SelectRunnerInstanceCompleted>(Failed, _logger),
            When(GetDefinitiveRevisionRequestFault).ThenFaultedOnFault<TSaga, TResponseFailed, TGetDefinitiveRevisionRequested>(Failed, _logger),
            When(GetDefinitiveRevisionCompletedFault).ThenFaultedOnFault<TSaga, TResponseFailed, TGetDefinitiveRevisionCompleted>(Failed, _logger),
            When(GetModuleRequestFault).ThenFaultedOnFault<TSaga, TResponseFailed, TGetModuleRequested>(Failed, _logger),
            When(GetModuleCompletedFault).ThenFaultedOnFault<TSaga, TResponseFailed, TGetModuleCompleted>(Failed, _logger),
            When(InitRequestFault).ThenFaultedOnFault<TSaga, TResponseFailed, TInitRequested>(Failed, _logger),
            When(InitCompletedFault).ThenFaultedOnFault<TSaga, TResponseFailed, TInitCompleted>(Failed, _logger),
            When(ValidateRequestFault).ThenFaultedOnFault<TSaga, TResponseFailed, TValidateRequested>(Failed, _logger),
            When(ValidateCompletedFault).ThenFaultedOnFault<TSaga, TResponseFailed, TValidateCompleted>(Failed, _logger),
            When(VariablesRequestFault).ThenFaultedOnFault<TSaga, TResponseFailed, TVariablesRequested>(Failed, _logger),
            When(VariablesCompletedFault).ThenFaultedOnFault<TSaga, TResponseFailed, TVariablesCompleted>(Failed, _logger),
            When(PolicyValidateRequestFault).ThenFaultedOnFault<TSaga, TResponseFailed, TPolicyValidateRequested>(Failed, _logger),
            When(PolicyValidateCompletedFault).ThenFaultedOnFault<TSaga, TResponseFailed, TPolicyValidateCompleted>(Failed, _logger),
            When(PlanRequestFault).ThenFaultedOnFault<TSaga, TResponseFailed, TPlanRequested>(Failed, _logger),
            When(PlanCompletedFault).ThenFaultedOnFault<TSaga, TResponseFailed, TPlanCompleted>(Failed, _logger),
            When(ApplyFromPlanRequestFault).ThenFaultedOnFault<TSaga, TResponseFailed, TApplyFromPlanRequested>(Failed, _logger),
            When(ApplyFromPlanCompletedFault).ThenFaultedOnFault<TSaga, TResponseFailed, TApplyFromPlanCompleted>(Failed, _logger),
            When(ProbeRequestFault).ThenFaultedOnFault<TSaga, TResponseFailed, TProbeRequested>(Failed, _logger),
            When(ProbeCompletedFault).ThenFaultedOnFault<TSaga, TResponseFailed, TProbeCompleted>(Failed, _logger),
            When(OutputRequestFault).ThenFaultedOnFault<TSaga, TResponseFailed, TOutputRequested>(Failed, _logger),
            When(OutputCompletedFault).ThenFaultedOnFault<TSaga, TResponseFailed, TOutputCompleted>(Failed, _logger)
        );
    }
}
