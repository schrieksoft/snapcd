namespace SnapCd.Contracts.Dto.Namespaces;

public class NamespaceCreateDto
{

    public string Name { get; set; } = null!;

    public Guid? StackId { get; set; }

    [Obsolete("Use NamespaceHook entities instead. If both are set on the same (Task, Phase), the NamespaceHook entity wins.")]
    public string? DefaultInitBeforeHook { get; set; }
    [Obsolete("Use NamespaceHook entities instead. If both are set on the same (Task, Phase), the NamespaceHook entity wins.")]
    public string? DefaultInitAfterHook { get; set; }
    
    [Obsolete("Use TerraformFlag entities instead.")]
    public bool? DefaultAutoUpgradeEnabled { get; set; }

    [Obsolete("Use TerraformFlag entities instead.")]
    public bool? DefaultAutoReconfigureEnabled { get; set; }
    [Obsolete("Use TerraformFlag entities instead.")]
    public bool? DefaultAutoMigrateEnabled { get; set; }
    public bool? DefaultCleanInitEnabled { get; set; }

    [Obsolete("Use NamespaceHook entities instead. If both are set on the same (Task, Phase), the NamespaceHook entity wins.")]
    public string? DefaultPlanBeforeHook { get; set; }
    [Obsolete("Use NamespaceHook entities instead. If both are set on the same (Task, Phase), the NamespaceHook entity wins.")]
    public string? DefaultPlanAfterHook { get; set; }

    [Obsolete("Use NamespaceHook entities instead. If both are set on the same (Task, Phase), the NamespaceHook entity wins.")]
    public string? DefaultPlanDestroyBeforeHook { get; set; }
    [Obsolete("Use NamespaceHook entities instead. If both are set on the same (Task, Phase), the NamespaceHook entity wins.")]
    public string? DefaultPlanDestroyAfterHook { get; set; }

    [Obsolete("Use NamespaceHook entities instead. If both are set on the same (Task, Phase), the NamespaceHook entity wins.")]
    public string? DefaultApplyBeforeHook { get; set; }
    [Obsolete("Use NamespaceHook entities instead. If both are set on the same (Task, Phase), the NamespaceHook entity wins.")]
    public string? DefaultApplyAfterHook { get; set; }

    [Obsolete("Use NamespaceHook entities instead. If both are set on the same (Task, Phase), the NamespaceHook entity wins.")]
    public string? DefaultOutputBeforeHook { get; set; }
    [Obsolete("Use NamespaceHook entities instead. If both are set on the same (Task, Phase), the NamespaceHook entity wins.")]
    public string? DefaultOutputAfterHook { get; set; }

    [Obsolete("Use NamespaceHook entities instead. If both are set on the same (Task, Phase), the NamespaceHook entity wins.")]
    public string? DefaultDestroyBeforeHook { get; set; }
    [Obsolete("Use NamespaceHook entities instead. If both are set on the same (Task, Phase), the NamespaceHook entity wins.")]
    public string? DefaultDestroyAfterHook { get; set; }

    [Obsolete("Use NamespaceHook entities instead. If both are set on the same (Task, Phase), the NamespaceHook entity wins.")]
    public string? DefaultValidateBeforeHook { get; set; }
    [Obsolete("Use NamespaceHook entities instead. If both are set on the same (Task, Phase), the NamespaceHook entity wins.")]
    public string? DefaultValidateAfterHook { get; set; }

    public int? DefaultApplyApprovalThreshold { get; set; }

    public int? DefaultDestroyApprovalThreshold { get; set; }

    public int? DefaultApprovalTimeoutMinutes { get; set; }

    public StateManagementEngine? DefaultEngine { get; set; }

    public NamespaceTriggerBehaviour? TriggerBehaviourOnModified { get; set; }

    public bool? DefaultDriftCheckEnabled { get; set; }
    public int? DefaultDriftCheckIntervalMinutes { get; set; }
}