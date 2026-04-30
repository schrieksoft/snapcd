using SnapCd.Server.Core.Events.Steps.Base;

namespace SnapCd.Server.Core.Events.Steps;

/// <summary>
/// Event indicating that destroy from plan has faulted with an error.
/// </summary>
public class DestroyFromPlanFaulted : StepFaultedBase
{
    public int? ActualResourceCount { get; set; }
}