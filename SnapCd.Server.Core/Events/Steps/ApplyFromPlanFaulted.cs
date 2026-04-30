using SnapCd.Server.Core.Events.Steps.Base;

namespace SnapCd.Server.Core.Events.Steps;

/// <summary>
/// Event indicating that apply from plan has faulted with an error.
/// </summary>
public class ApplyFromPlanFaulted : StepFaultedBase
{
    public int? ActualResourceCount { get; set; }
}