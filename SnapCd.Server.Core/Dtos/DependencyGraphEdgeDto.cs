namespace SnapCd.Server.Core.Dtos;

public class DependencyGraphEdgeDto
{
    public string DisplayName { get; set; } = null!;
    public Guid ModuleId { get; set; }
    public Guid NamespaceId { get; set; }
}