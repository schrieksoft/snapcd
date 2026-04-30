using SnapCd.Contracts;

namespace SnapCd.Server.Core.Dtos;

public class StackDependencyGraphDto
{
    public Guid StackId { get; set; }
    public string StackName { get; set; } = null!;
    public string Direction { get; set; } = "Apply"; // "Apply" or "Destroy"
    public DesiredStateHeadline TargetState { get; set; }
    public List<DependencyGraphNodeStateDto> NodeStates { get; set; } = new();
}