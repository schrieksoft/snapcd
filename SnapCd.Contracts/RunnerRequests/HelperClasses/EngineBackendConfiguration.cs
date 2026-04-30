using SnapCd.Contracts.Dto.ModuleBackendConfigs;
using SnapCd.Contracts.Dto.NamespaceBackendConfigs;

namespace SnapCd.Contracts.RunnerRequests.HelperClasses;

/// <summary>
/// Backend configuration for engine state management.
/// Used during Init operation.
/// </summary>
public class EngineBackendConfiguration
{
    // Terraform/OpenTofu backend configuration
    public bool IgnoreNamespaceBackendConfigs { get; set; }
    public List<NamespaceBackendConfigReadDto> NamespaceBackendConfigs { get; set; } = [];
    public List<ModuleBackendConfigReadDto> ModuleBackendConfigs { get; set; } = [];

    // Pulumi flag configuration (resolved from namespace defaults + module overrides)
    public List<PulumiFlagEntry> PulumiFlags { get; set; } = [];
    public List<PulumiArrayFlagEntry> PulumiArrayFlags { get; set; } = [];

    // Terraform/OpenTofu flag configuration (resolved from namespace defaults + module overrides)
    public List<TerraformFlagEntry> TerraformFlags { get; set; } = [];
    public List<TerraformArrayFlagEntry> TerraformArrayFlags { get; set; } = [];
}

public class PulumiFlagEntry
{
    public PulumiCommandTask Task { get; set; }
    public PulumiFlag Flag { get; set; }
    public string? Value { get; set; }
}

public class PulumiArrayFlagEntry
{
    public PulumiCommandTask Task { get; set; }
    public PulumiArrayFlag Flag { get; set; }
    public string Value { get; set; } = null!;
}

public class TerraformFlagEntry
{
    public TerraformCommandTask Task { get; set; }
    public TerraformFlag Flag { get; set; }
    public string? Value { get; set; }
}

public class TerraformArrayFlagEntry
{
    public TerraformCommandTask Task { get; set; }
    public TerraformArrayFlag Flag { get; set; }
    public string Value { get; set; } = null!;
}
