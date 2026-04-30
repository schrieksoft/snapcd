using SnapCd.Contracts;
using SnapCd.Server.Core.Enums;

namespace SnapCd.Server.Core.Views;


public class ModuleStateInfo
{
    // Identity fields
    public Guid ModuleId { get; set; }
    public string Name { get; set; } = null!;
    public string NamespaceName { get; set; } = null!;
    public Guid NamespaceId { get; set; }
    public string StackName { get; set; } = null!;
    public Guid StackId { get; set; }
    public string DisplayName { get; set; } = null!;

    // State fields
    public ActualStateHeadline? LatestActualState { get; set; }
    public DesiredStateHeadline? DesiredState { get; set; }
    public DesiredStateHeadline? RunningDesiredState { get; set; }
    public DesiredStateHeadline? QueuedDesiredState { get; set; }
    public ExecutionStatus LatestExecutionStatus { get; set; }
    public bool IsRunning { get; set; }
    public bool IsQueued { get; set; }
}