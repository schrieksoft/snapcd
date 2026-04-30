namespace SnapCd.Server.Core.Services.ResolvedConfiguration.HelperClasses;

public class ModuleDependencyNode
{
    public Guid ModuleId { get; set; }

    public required string ModuleName { get; set; }
    public List<DependsOnModuleResolved> DependsOnModules { get; set; } = new();
}