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
