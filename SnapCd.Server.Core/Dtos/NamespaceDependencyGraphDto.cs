using SnapCd.Contracts;

namespace SnapCd.Server.Core.Dtos;

public class NamespaceDependencyGraphDto
{
    public Guid NamespaceId { get; set; }
    public string NamespaceName { get; set; } = null!;
    public string Direction { get; set; } = "Apply"; // "Apply" or "Destroy"
    public DesiredStateHeadline TargetState { get; set; }
    public List<DependencyGraphNodeStateDto> NodeStates { get; set; } = new();
}