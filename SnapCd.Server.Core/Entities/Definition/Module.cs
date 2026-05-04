using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;
using SnapCd.Contracts;
using SnapCd.Server.Core.Entities.Definition.Base;
using SnapCd.Server.Core.Entities.Definition.RoleAssignments.Org;
using SnapCd.Server.Core.Entities.Definition.RoleAssignments.Org.Base;
using SnapCd.Server.Core.Entities.Definition.RunnerAssignments;
using SnapCd.Server.Core.Entities.Definition.Secrets.Scoped;
using SnapCd.Server.Core.Entities.Interfaces;
using SnapCd.Server.Core.Entities.Sagas;

namespace SnapCd.Server.Core.Entities.Definition;

public class Module : AuditBase, IEntity, ICreationTrackable, INamespaceChild
{
    public Guid Id { get; set; }
    public Guid OrganizationId { get; set; }
    public Guid NamespaceId { get; set; }
    public Guid RunnerId { get; set; }

    [JsonIgnore] public ModuleSaga? ModuleSaga { get; set; }
    [JsonIgnore] public ModuleModifiedSaga? ModuleModifiedSaga { get; set; }
    [MaxLength(255)] public string? RunnerInstanceName { get; set; }
    [MaxLength(255)] public string Name { get; set; } = null!;
    [MaxLength(800)] public string SourceUrl { get; set; } = null!;
    [MaxLength(255)] public string SourceRevision { get; set; } = null!;
    [MaxLength(255)] public string SourceSubdirectory { get; set; } = string.Empty;

    public int? ApplyApprovalThreshold { get; set; }

    public int? DestroyApprovalThreshold { get; set; }

    public int? ApprovalTimeoutMinutes { get; set; }
    public SourceType SourceType { get; set; } = SourceType.Git;
    public SourceRevisionType SourceRevisionType { get; set; } = SourceRevisionType.Default;
    public List<ModuleParamFromDefinition> ModuleParamFromDefinitions { get; set; } = new();
    public List<ModuleParamFromNamespace> ModuleParamFromNamespaces { get; set; } = new();
    public List<ModuleParamFromLiteral> ModuleParamFromLiterals { get; set; } = new();
    public List<ModuleParamFromOutput> ModuleParamFromOutputs { get; set; } = new();
    public List<ModuleParamFromOutputSet> ModuleParamFromOutputSets { get; set; } = new();
    public List<ModuleParamFromSecret> ModuleParamFromSecrets { get; set; } = new();

    public List<ModuleEnvVarFromDefinition> ModuleEnvVarFromDefinitions { get; set; } = new();
    public List<ModuleEnvVarFromNamespace> ModuleEnvVarFromNamespaces { get; set; } = new();
    public List<ModuleEnvVarFromLiteral> ModuleEnvVarFromLiterals { get; set; } = new();
    public List<ModuleEnvVarFromOutput> ModuleEnvVarFromOutputs { get; set; } = new();
    public List<ModuleEnvVarFromSecret> ModuleEnvVarFromSecrets { get; set; } = new();

    public List<OutputSet> OutputSets { get; set; } = new();
    public List<VariableSet> VariableSets { get; set; } = new();
    public List<ApplyJobSaga> ApplyModuleSaga { get; set; } = new();
    public List<DestroyJobSaga> DestroyModuleSaga { get; set; } = new();
    public List<ModuleJob> ModuleJobs { get; set; } = new();

    public List<ModuleExtraFile> ModuleExtraFiles { get; set; } = new();

    public List<DependsOnModule> DependsOnModules { get; set; } = new();

    public List<DependsOnModule> DependentModules { get; set; } = new();

    [JsonIgnore] public virtual Organization Organization { get; set; } = null!;
    [JsonIgnore] public Namespace Namespace { get; set; } = null!;

    [JsonIgnore] public Runner Runner { get; set; } = null!;

    public List<RunnerModuleAssignment>? RunnerModuleAssignments { get; set; } = new();

    public List<ModuleSecret>? SecretsScopedToModule { get; set; } = new();

    // NEW Role Assignment navigation properties
    public virtual ICollection<UserModuleRoleAssignment> UserModuleRoleAssignments { get; set; } = new List<UserModuleRoleAssignment>();
    public virtual ICollection<ServicePrincipalModuleRoleAssignment> ServicePrincipalModuleRoleAssignments { get; set; } = new List<ServicePrincipalModuleRoleAssignment>();
    public virtual ICollection<GroupModuleRoleAssignment> GroupModuleRoleAssignments { get; set; } = new List<GroupModuleRoleAssignment>();

    public bool IgnoreNamespaceExtraFiles { get; set; }
    public bool IgnoreNamespaceFlags { get; set; }
    public bool IgnoreNamespaceHooks { get; set; }

    public bool? CleanInitEnabled { get; set; }

    public List<ModuleHook> Hooks { get; set; } = new();

    public StateManagementEngine? Engine { get; set; }

    public List<ModulePulumiFlag> PulumiFlags { get; set; } = new();
    public List<ModulePulumiArrayFlag> PulumiArrayFlags { get; set; } = new();

    public List<ModuleTerraformFlag> TerraformFlags { get; set; } = new();
    public List<ModuleTerraformArrayFlag> TerraformArrayFlags { get; set; } = new();

    public DateTime CreatedDateTime { get; set; }

    public WaitForApplyDependencies WaitForApplyDependencies { get; set; } = WaitForApplyDependencies.OnFirstApply;

    public WaitForDestroyDependencies WaitForDestroyDependencies { get; set; } = WaitForDestroyDependencies.Always;

    public bool TriggerOnDefinitionChanged { get; set; }

    public bool TriggerOnUpstreamOutputChanged { get; set; }

    public bool TriggerOnSourceChanged { get; set; }

    public bool TriggerOnSourceChangedNotification { get; set; }

    public bool? DriftCheckEnabled { get; set; }
    public int? DriftCheckIntervalMinutes { get; set; }

    public virtual ICollection<ModuleRoleAssignment> ModuleRoleAssignments { get; set; } = new List<ModuleRoleAssignment>();

    public Guid ParentId()
    {
        return NamespaceId;
    }


    // public IQueryable<ParentToResourceView> GetParentResourceQuery()
    // {
    //     // Return namespace ID as parent
    //     var namespaceView = new List<ParentToResourceView>
    //     {
    //         new ParentToResourceView
    //         {
    //             ParentResourceId = NamespaceId,
    //             ResourceId = Id
    //         }
    //     };
    //
    //     // Return stack ID as parent (if namespace is loaded)
    //     var stackView = Namespace != null
    //         ? new List<ParentToResourceView>
    //         {
    //             new ParentToResourceView
    //             {
    //                 ParentResourceId = Namespace.StackId,
    //                 ResourceId = Id
    //             }
    //         }
    //         : new List<ParentToResourceView>();
    //
    //     return namespaceView.Concat(stackView).AsQueryable();
    // }
}