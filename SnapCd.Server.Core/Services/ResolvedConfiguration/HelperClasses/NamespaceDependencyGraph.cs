namespace SnapCd.Server.Core.Services.ResolvedConfiguration.HelperClasses;

public class NamespaceDependencyGraph
{
    public required Guid StackId { get; set; }

    public required Guid NamespaceId { get; set; }

    public required string NamespaceName { get; set; }

    public required string StackName { get; set; }

    public List<ModuleDependencyNode> ModuleDependencyNodes { get; set; } = new();
}