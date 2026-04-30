using SnapCd.Contracts;

namespace SnapCd.Server.Core.Dtos;

public class ModuleDependencyGraphDto
{
    public Guid ModuleId { get; set; }
    public string ModuleName { get; set; } = null!;
    public string Direction { get; set; } = "Apply"; // "Apply" or "Destroy"
    public DesiredStateHeadline TargetState { get; set; }
    public List<DependencyGraphNodeStateDto> NodeStates { get; set; } = new();
}