using SnapCd.Contracts;
using SnapCd.Server.Core.Enums;

namespace SnapCd.Server.Core.Dtos;

public class DestroyModuleNodeDto
{
    public Guid ModuleId { get; set; }
    public string ModuleName { get; set; } = null!;
    public Guid NamespaceId { get; set; }
    public string NamespaceName { get; set; } = null!;
    public Guid StackId { get; set; }
    public string StackName { get; set; } = null!;
    public string DisplayName { get; set; } = null!;

    public ActualStateHeadline? ActualState { get; set; }
    public DesiredStateHeadline? DesiredState { get; set; }

    /// <summary>
    /// Stage in the destruction process (1-indexed, Stage 1 = first to be destroyed)
    /// </summary>
    public int Stage { get; set; }

    /// <summary>
    /// Modules that depend on this module (will be destroyed first)
    /// </summary>
    public List<string> DependentModules { get; set; } = new();

    /// <summary>
    /// Modules this module depends on (will be destroyed after this)
    /// </summary>
    public List<string> DependencyModules { get; set; } = new();

    /// <summary>
    /// Whether this module is currently in a state that can be destroyed
    /// </summary>
    public bool CanBeDestroyed => ActualState == ActualStateHeadline.Applied;

    /// <summary>
    /// Whether this module is already destroyed
    /// </summary>
    public bool IsAlreadyDestroyed => ActualState == ActualStateHeadline.Destroyed;
}