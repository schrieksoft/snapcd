using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;
using SnapCd.Contracts;
using SnapCd.Server.Core.Entities.Definition.Base;
using SnapCd.Server.Core.Entities.Definition.RoleAssignments.Org;
using SnapCd.Server.Core.Entities.Definition.RoleAssignments.Org.Base;
using SnapCd.Server.Core.Entities.Definition.RunnerAssignments;
using SnapCd.Server.Core.Entities.Definition.Secrets.Scoped;
using SnapCd.Server.Core.Entities.Interfaces;

namespace SnapCd.Server.Core.Entities.Definition;

public class Namespace : AuditBase, IEntity, ICreationTrackable
{
    public Guid Id { get; set; }
    public Guid OrganizationId { get; set; }

    public Guid StackId { get; set; }
    [MaxLength(255)] public string Name { get; set; } = null!;
    public List<Module>? Modules { get; set; }

    public List<NamespaceSecret> SecretsScopedToNamespace { get; set; } = new();
    public List<NamespaceParamFromDefinition> NamespaceParamFromDefinitions { get; set; } = new();
    public List<NamespaceParamFromLiteral> NamespaceParamFromLiterals { get; set; } = new();
    public List<NamespaceParamFromSecret> NamespaceParamFromSecrets { get; set; } = new();
    public List<NamespaceEnvVarFromDefinition> NamespaceEnvVarFromDefinitions { get; set; } = new();
    public List<NamespaceEnvVarFromLiteral> NamespaceEnvVarFromLiterals { get; set; } = new();
    public List<NamespaceEnvVarFromSecret> NamespaceEnvVarFromSecrets { get; set; } = new();

    public List<RunnerNamespaceAssignment> RunnerNamespaceAssignments { get; set; } = new();
    public List<NamespaceExtraFile> NamespaceExtraFiles { get; set; } = new();

    // NEW Role Assignment navigation properties
    public virtual ICollection<UserNamespaceRoleAssignment> UserNamespaceRoleAssignments { get; set; } = new List<UserNamespaceRoleAssignment>();
    public virtual ICollection<ServicePrincipalNamespaceRoleAssignment> ServicePrincipalNamespaceRoleAssignments { get; set; } = new List<ServicePrincipalNamespaceRoleAssignment>();
    public virtual ICollection<GroupNamespaceRoleAssignment> GroupNamespaceRoleAssignments { get; set; } = new List<GroupNamespaceRoleAssignment>();

    [JsonIgnore] // So that JSON Serialization does not create a loop
    public virtual Organization Organization { get; set; } = null!;

    [JsonIgnore] // So that JSON Serialization does not create a loop
    public Stack Stack { get; set; } = null!;

    public bool? DefaultCleanInitEnabled { get; set; }

    public List<NamespaceHook> Hooks { get; set; } = new();

    public int? DefaultApplyApprovalThreshold { get; set; }

    public int? DefaultDestroyApprovalThreshold { get; set; }

    public int? DefaultApprovalTimeoutMinutes { get; set; }

    public NamespaceTriggerBehaviour? TriggerBehaviourOnModified { get; set; } = NamespaceTriggerBehaviour.DoNotTrigger;

    public virtual ICollection<NamespaceRoleAssignment> NamespaceRoleAssignments { get; set; } = new List<NamespaceRoleAssignment>();


    public StateManagementEngine? DefaultEngine { get; set; }

    public List<NamespacePulumiFlag> PulumiFlags { get; set; } = new();
    public List<NamespacePulumiArrayFlag> PulumiArrayFlags { get; set; } = new();

    public List<NamespaceTerraformFlag> TerraformFlags { get; set; } = new();
    public List<NamespaceTerraformArrayFlag> TerraformArrayFlags { get; set; } = new();

    public bool? DefaultDriftCheckEnabled { get; set; }
    public int? DefaultDriftCheckIntervalMinutes { get; set; }

    public Guid ParentId()
    {
        return StackId;
    }
}