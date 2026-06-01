// SPDX-License-Identifier: LicenseRef-Snap-CD-Source-Available-1.1
// Copyright (c) 2026 Karl Schriek / Schrieksoft.
// No license is granted to use this file, in whole or in part, (a) as training, fine-tuning, retrieval, or
// embedding data for any machine-learning model, or (b) as input to any machine-learning model, agent, or automated
// system for the purpose of producing a derivative work or reimplementation that is not otherwise permitted by the
// Snap CD Source-Available License (including any Competing Product as defined therein). Contact info@snapcd.io
// for terms covering either use.

using SnapCd.Server.Core.Entities.Definition;

namespace SnapCd.Server.Core.StateMachine.Jobs.Utils;

/// <summary>
/// Helper class to map event types to their corresponding ServerSideStep.
/// </summary>
public static class StepMapper
{
    /// <summary>
    /// Determines the ServerSideStep based on the faulted event type name.
    /// </summary>
    public static ServerSideStep? DetermineStepFromEventType(Type eventType)
    {
        var name = eventType.Name;

        return name switch
        {
            _ when name.Contains("SelectRunnerInstance") => ServerSideStep.SelectRunnerInstance,
            _ when name.Contains("GetDefinitiveRevision") => ServerSideStep.GetDefinitiveRevision,
            _ when name.Contains("GetModule") => ServerSideStep.GetModule,
            _ when name.Contains("Init") => ServerSideStep.Init,
            _ when name.Contains("Validate") => ServerSideStep.Validate,
            _ when name.Contains("Variables") => ServerSideStep.Variables,
            _ when name.Contains("PlanDestroy") => ServerSideStep.Plan,
            _ when name.Contains("Plan") => ServerSideStep.Plan,
            _ when name.Contains("ApplyFromPlan") => ServerSideStep.ApplyFromPlan,
            _ when name.Contains("DestroyFromPlan") => ServerSideStep.DestroyFromPlan,
            _ when name.Contains("Output") => ServerSideStep.Output,
            _ => null
        };
    }
}
