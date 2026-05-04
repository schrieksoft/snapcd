using SnapCd.Contracts;
using SnapCd.Contracts.Dto.Misc;
using SnapCd.Contracts.Dto.ModuleInputs;
using SnapCd.Contracts.Dto.NamespaceInputs;
using SnapCd.Contracts.RunnerRequests.HelperClasses;

namespace SnapCd.Server.Core.Services.ResolvedConfiguration.HelperClasses;

public class ResolvedModule
{
    public List<NamespaceInputFromLiteralReadDto>? NamespaceParamFromLiterals { get; set; } = new();

    public List<NamespaceInputFromLiteralReadDto>? NamespaceEnvVarFromLiterals { get; set; } = new();

    // public List<NamespaceParamFromSecretDto>? NamespaceParamFromSecrets { get; set; } = new();
    // public List<NamespaceEnvVarFromSecretDto>? NamespaceEnvVarFromSecrets { get; set; } = new();

    public List<NamespaceInputFromDefinitionReadDto>? NamespaceParamFromDefinitions { get; set; } = new();

    public List<NamespaceInputFromDefinitionReadDto>? NamespaceEnvVarFromDefinitions { get; set; } = new();

    public List<ModuleInputFromDefinitionReadDto>? ModuleParamFromDefinitions { get; set; } = new();
    public List<ModuleInputFromLiteralReadDto>? ModuleParamFromLiterals { get; set; } = new();
    public List<ModuleInputFromNamespaceReadDto>? ModuleParamFromNamespaces { get; set; } = new();

    public List<ModuleInputFromDefinitionReadDto>? ModuleEnvVarFromDefinitions { get; set; } = new();
    public List<ModuleInputFromLiteralReadDto>? ModuleEnvVarFromLiterals { get; set; } = new();
    public List<ModuleInputFromNamespaceReadDto>? ModuleEnvVarFromNamespaces { get; set; } = new();

    public List<ExtraFileDto>? ExtraFiles { get; set; } = new();

    public SourceType SourceType { get; set; } = SourceType.Git;
    public SourceRevisionType SourceRevisionType { get; set; } = SourceRevisionType.Default;

    public int? ApprovalTimeoutMinutes { get; set; }

    public Guid ModuleId { get; set; }
    public Guid NamespaceId { get; set; }
    public Guid StackId { get; set; }

    public Guid OrganizationId { get; set; }
    public Guid RunnerId { get; set; }

    public required string ModuleName { get; set; }
    public required string NamespaceName { get; set; }
    public required string StackName { get; set; }
    public required string RunnerName { get; set; }
    public required string SourceRevision { get; set; }
    public required string SourceUrl { get; set; }

    public required string SourceSubdirectory { get; set; }

    public string? RunnerInstanceName { get; set; }
    public string? InitBeforeHook { get; set; }
    public string? InitAfterHook { get; set; }
    public bool IgnoreNamespaceExtraFiles { get; set; }

    public bool CleanInitEnabled { get; set; }
    public bool DriftCheckEnabled { get; set; }
    public int? DriftCheckIntervalMinutes { get; set; }
    public string? PlanBeforeHook { get; set; }
    public string? PlanAfterHook { get; set; }
    public string? PlanDestroyBeforeHook { get; set; }
    public string? PlanDestroyAfterHook { get; set; }
    public string? ApplyBeforeHook { get; set; }
    public string? ApplyAfterHook { get; set; }
    public string? DestroyBeforeHook { get; set; }
    public string? DestroyAfterHook { get; set; }
    public string? OutputBeforeHook { get; set; }
    public string? OutputAfterHook { get; set; }
    public string? ValidateBeforeHook { get; set; }
    public string? ValidateAfterHook { get; set; }
    public required string Engine { get; set; }
    public List<PulumiFlagEntry> PulumiFlags { get; set; } = new();
    public List<PulumiArrayFlagEntry> PulumiArrayFlags { get; set; } = new();
    public List<TerraformFlagEntry> TerraformFlags { get; set; } = new();
    public List<TerraformArrayFlagEntry> TerraformArrayFlags { get; set; } = new();
    public List<DependsOnModuleResolved> DependsOnModules { get; set; } = new();

    public List<SelectedModuleSecret> SelectedModuleParamsFromSecrets { get; set; } = new();
    public List<SelectedModuleSecret> SelectedModuleEnvVarsFromSecrets { get; set; } = new();

    public List<SelectedNamespaceSecret> SelectedNamespaceParamsFromSecrets { get; set; } = new();
    public List<SelectedNamespaceSecret> SelectedNamespaceEnvVarsFromSecrets { get; set; } = new();
}