// SPDX-License-Identifier: LicenseRef-Snap-CD-Source-Available-1.1
// Copyright (c) 2026 Karl Schriek / Schrieksoft.
// No license is granted to use this file, in whole or in part, (a) as training, fine-tuning, retrieval, or
// embedding data for any machine-learning model, or (b) as input to any machine-learning model, agent, or automated
// system for the purpose of producing a derivative work or reimplementation that is not otherwise permitted by the
// Snap CD Source-Available License (including any Competing Product as defined therein). Contact info@snapcd.io
// for terms covering either use.

using SnapCd.Server.Core.Enums;

namespace SnapCd.Server.Core.Misc.Helpers;

/// <summary>
/// Maps RunnerHub methods to their expected saga states for authorization validation.
/// Each hub method should only be callable when the saga is in its corresponding state.
/// </summary>
public static class StateHelper
{
    public static ModuleJobSagaState Lookup(TaskEndpoint taskEndpoint)
    {
        return MethodToState[taskEndpoint];
    }

    /// <summary>
    /// Returns all cancellation states that a saga can transition to during cancellation.
    /// Used for validation to allow completion messages while in cancellation states.
    /// </summary>
    public static HashSet<ModuleJobSagaState> GetCancellingStates()
    {
        return new HashSet<ModuleJobSagaState>
        {
            ModuleJobSagaState.CancellingImmediateKill,
            ModuleJobSagaState.CancellingImmediateGraceful,
            ModuleJobSagaState.CancellingAfterCurrent
        };
    }

    public static readonly Dictionary<TaskEndpoint, ModuleJobSagaState> MethodToState = new()
    {
        // SelectRunnerInstance step
        [TaskEndpoint.SelectRunnerInstanceCompleted] = ModuleJobSagaState.SelectRunnerInstancePending,
        [TaskEndpoint.SelectRunnerInstanceCancelled] = ModuleJobSagaState.SelectRunnerInstancePending,
        [TaskEndpoint.SelectRunnerInstanceFaulted] = ModuleJobSagaState.SelectRunnerInstancePending,

        // GetDefinitiveRevision step
        [TaskEndpoint.GetDefinitiveRevisionCompleted] = ModuleJobSagaState.GetDefinitiveRevisionPending,
        [TaskEndpoint.GetDefinitiveRevisionCancelled] = ModuleJobSagaState.GetDefinitiveRevisionPending,
        [TaskEndpoint.GetDefinitiveRevisionFaulted] = ModuleJobSagaState.GetDefinitiveRevisionPending,

        // GetModule step
        [TaskEndpoint.GetModuleCompleted] = ModuleJobSagaState.GetModulePending,
        [TaskEndpoint.GetModuleCancelled] = ModuleJobSagaState.GetModulePending,
        [TaskEndpoint.GetModuleFaulted] = ModuleJobSagaState.GetModulePending,

        // Init step
        [TaskEndpoint.InitCompleted] = ModuleJobSagaState.InitPending,
        [TaskEndpoint.InitCancelled] = ModuleJobSagaState.InitPending,
        [TaskEndpoint.InitFaulted] = ModuleJobSagaState.InitPending,

        // Validate step
        [TaskEndpoint.ValidateCompleted] = ModuleJobSagaState.ValidatePending,
        [TaskEndpoint.ValidateCancelled] = ModuleJobSagaState.ValidatePending,
        [TaskEndpoint.ValidateFaulted] = ModuleJobSagaState.ValidatePending,

        // Variables step
        [TaskEndpoint.VariablesCompleted] = ModuleJobSagaState.VariablesPending,
        [TaskEndpoint.VariablesCancelled] = ModuleJobSagaState.VariablesPending,
        [TaskEndpoint.VariablesFaulted] = ModuleJobSagaState.VariablesPending,

        // Plan step (both apply and destroy use this)
        [TaskEndpoint.PlanCompleted] = ModuleJobSagaState.PlanPending,
        [TaskEndpoint.PlanCancelled] = ModuleJobSagaState.PlanPending,
        [TaskEndpoint.PlanFaulted] = ModuleJobSagaState.PlanPending,
        [TaskEndpoint.PlanDestroyCompleted] = ModuleJobSagaState.PlanPending,
        [TaskEndpoint.PlanDestroyCancelled] = ModuleJobSagaState.PlanPending,
        [TaskEndpoint.PlanDestroyFaulted] = ModuleJobSagaState.PlanPending,

        // ApplyFromPlan step (both apply and destroy use this)
        [TaskEndpoint.ApplyFromPlanCompleted] = ModuleJobSagaState.ApplyFromPlanPending,
        [TaskEndpoint.ApplyFromPlanCancelled] = ModuleJobSagaState.ApplyFromPlanPending,
        [TaskEndpoint.ApplyFromPlanFaulted] = ModuleJobSagaState.ApplyFromPlanPending,
        [TaskEndpoint.DestroyFromPlanCompleted] = ModuleJobSagaState.ApplyFromPlanPending,
        [TaskEndpoint.DestroyFromPlanCancelled] = ModuleJobSagaState.ApplyFromPlanPending,
        [TaskEndpoint.DestroyFromPlanFaulted] = ModuleJobSagaState.ApplyFromPlanPending,

        // Output step
        [TaskEndpoint.OutputCompleted] = ModuleJobSagaState.OutputPending,
        [TaskEndpoint.OutputCancelled] = ModuleJobSagaState.OutputPending,
        [TaskEndpoint.OutputFaulted] = ModuleJobSagaState.OutputPending,
            
        // Cancellation
        [TaskEndpoint.CancelKillCompleted] = ModuleJobSagaState.CancellingImmediateKill,
        [TaskEndpoint.CancelGracefulCompleted] = ModuleJobSagaState.CancellingImmediateGraceful

    };
}
