using SnapCd.Server.Core.Enums;

namespace SnapCd.Server.Core.Dtos;

public class ApplyModuleGraphDto
{
    public Guid RootModuleId { get; set; }
    public List<ApplyModuleNodeDto> NodeStates { get; set; } = new();
    public int TotalModuleCount { get; set; }
    public int TotalStages { get; set; }
}

public class ApplyModuleNodeDto
{
    public Guid ModuleId { get; set; }
    public string DisplayName { get; set; } = null!;
    public int Stage { get; set; }
    public ActualStateHeadline? ActualState { get; set; }

    /// <summary>
    /// Modules that depend on this module (must wait for this to be applied)
    /// </summary>
    public List<string> DependentModules { get; set; } = new();

    /// <summary>
    /// Modules this module depends on (must be applied before this)
    /// </summary>
    public List<string> DependencyModules { get; set; } = new();
}