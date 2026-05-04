namespace SnapCd.Contracts.Dto.Modules;

/// <summary>
/// DTO for creating a new Module (POST operations).
/// </summary>
public class ModuleCreateDto
{
    public string Name { get; set; } = null!;
    public Guid NamespaceId { get; set; }

    public string SourceRevision { get; set; } = null!;
    public string SourceUrl { get; set; } = null!;

    public string SourceSubdirectory { get; set; } = null!;

    public List<string>? DependsOnModules { get; set; }

    public SourceType SourceType { get; set; } = SourceType.Git;

    public SourceRevisionType SourceRevisionType { get; set; } = SourceRevisionType.Default;

    public WaitForApplyDependencies WaitForApplyDependencies { get; set; } = WaitForApplyDependencies.OnFirstApply;

    public WaitForDestroyDependencies WaitForDestroyDependencies { get; set; } = WaitForDestroyDependencies.Always;

    public int? ApplyApprovalThreshold { get; set; }

    public int? DestroyApprovalThreshold { get; set; }

    public int? ApprovalTimeoutMinutes { get; set; }

    public Guid RunnerId { get; set; }
    public string? RunnerInstanceName { get; set; }

    public bool IgnoreNamespaceExtraFiles { get; set; }
    public bool IgnoreNamespaceFlags { get; set; }
    public bool IgnoreNamespaceHooks { get; set; }

    public bool? CleanInitEnabled { get; set; }

    public StateManagementEngine? Engine { get; set; }

    public bool TriggerOnDefinitionChanged { get; set; }

    public bool TriggerOnUpstreamOutputChanged { get; set; }

    public bool TriggerOnSourceChanged { get; set; }

    public bool TriggerOnSourceChangedNotification { get; set; }

    public bool? DriftCheckEnabled { get; set; }
    public int? DriftCheckIntervalMinutes { get; set; }
}
