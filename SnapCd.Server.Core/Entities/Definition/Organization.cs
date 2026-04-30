using System.ComponentModel.DataAnnotations;
using SnapCd.Server.Core.Entities.Definition.Base;
using SnapCd.Server.Core.Entities.Definition.GroupMembers;
using SnapCd.Server.Core.Entities.Definition.RoleAssignments.Org;
using SnapCd.Server.Core.Entities.Definition.RoleAssignments.Org.Base;
using SnapCd.Server.Core.Entities.Definition.RoleAssignments.Org.Runner.Base;
using SnapCd.Server.Core.Entities.Definition.RunnerAssignments;
using SnapCd.Server.Core.Entities.Definition.Secrets;
using SnapCd.Server.Core.Entities.Interfaces;

namespace SnapCd.Server.Core.Entities.Definition;

public class Organization : AuditBase, ISystemEntity
{
    public Guid Id { get; set; }

    [Required] [MaxLength(127)] public required string Name { get; set; }

    [MaxLength(255)] public string? InputKeyVaultUrl { get; set; }

    [MaxLength(255)] public string? OutputKeyVaultUrl { get; set; }

    // Status and audit fields
    public OrganizationStatus Status { get; set; } = OrganizationStatus.Active;
    public DateTime? DeletedDateTime { get; set; }
    public Guid? DeletedByUserId { get; set; }

    public bool IsDeleted => DeletedDateTime.HasValue;

    // Navigation properties
    public virtual ICollection<OrganizationUser> OrganizationUsers { get; set; } = new List<OrganizationUser>();
    public virtual ICollection<Stack> Stacks { get; set; } = new List<Stack>();
    public virtual ICollection<Namespace> Namespaces { get; set; } = new List<Namespace>();
    public virtual ICollection<Module> Modules { get; set; } = new List<Module>();

    public virtual ICollection<Runner> Runners { get; set; } = new List<Runner>();
    public virtual ICollection<SourceRefresherPreselection> SourceRefresherPreselections { get; set; } = new List<SourceRefresherPreselection>();
    public virtual ICollection<Secret> Secrets { get; set; } = new List<Secret>();

    public virtual ICollection<Output> Outputs { get; set; } = new List<Output>();
    public virtual ICollection<OutputSet> OutputSets { get; set; } = new List<OutputSet>();
    public virtual ICollection<Variable> Variables { get; set; } = new List<Variable>();
    public virtual ICollection<VariableSet> VariableSets { get; set; } = new List<VariableSet>();

    public virtual ICollection<ModuleJob> ModuleJobs { get; set; } = new List<ModuleJob>();
    public virtual ICollection<ModuleJobApproval> ModuleJobApprovals { get; set; } = new List<ModuleJobApproval>();
    public virtual ICollection<ModuleExtraFile> ModuleExtraFiles { get; set; } = new List<ModuleExtraFile>();
    public virtual ICollection<NamespaceExtraFile> NamespaceExtraFiles { get; set; } = new List<NamespaceExtraFile>();

    public virtual ICollection<Group> Groups { get; set; } = new List<Group>();
    public virtual ICollection<GroupMember> GroupMembers { get; set; } = new List<GroupMember>();
    public virtual ICollection<UserGroupMember> UserGroupMembers { get; set; } = new List<UserGroupMember>();
    public virtual ICollection<ServicePrincipalGroupMember> ServicePrincipalGroupMembers { get; set; } = new List<ServicePrincipalGroupMember>();
    public virtual ICollection<GroupGroupMember> GroupGroupMembers { get; set; } = new List<GroupGroupMember>();
    public virtual ICollection<ServicePrincipal> ServicePrincipals { get; set; } = new List<ServicePrincipal>();

    public virtual ICollection<OrganizationRoleAssignment> OrganizationRoleAssignments { get; set; } = new List<OrganizationRoleAssignment>();
    public virtual ICollection<StackRoleAssignment> StackRoleAssignments { get; set; } = new List<StackRoleAssignment>();
    public virtual ICollection<NamespaceRoleAssignment> NamespaceRoleAssignments { get; set; } = new List<NamespaceRoleAssignment>();
    public virtual ICollection<ModuleRoleAssignment> ModuleRoleAssignments { get; set; } = new List<ModuleRoleAssignment>();

    public virtual ICollection<UserOrganizationRoleAssignment> UserOrganizationRoleAssignments { get; set; } = new List<UserOrganizationRoleAssignment>();
    public virtual ICollection<UserStackRoleAssignment> UserStackRoleAssignments { get; set; } = new List<UserStackRoleAssignment>();
    public virtual ICollection<UserNamespaceRoleAssignment> UserNamespaceRoleAssignments { get; set; } = new List<UserNamespaceRoleAssignment>();
    public virtual ICollection<UserModuleRoleAssignment> UserModuleRoleAssignments { get; set; } = new List<UserModuleRoleAssignment>();
    public virtual ICollection<ServicePrincipalOrganizationRoleAssignment> ServicePrincipalOrganizationRoleAssignments { get; set; } = new List<ServicePrincipalOrganizationRoleAssignment>();
    public virtual ICollection<ServicePrincipalStackRoleAssignment> ServicePrincipalStackRoleAssignments { get; set; } = new List<ServicePrincipalStackRoleAssignment>();
    public virtual ICollection<ServicePrincipalNamespaceRoleAssignment> ServicePrincipalNamespaceRoleAssignments { get; set; } = new List<ServicePrincipalNamespaceRoleAssignment>();
    public virtual ICollection<ServicePrincipalModuleRoleAssignment> ServicePrincipalModuleRoleAssignments { get; set; } = new List<ServicePrincipalModuleRoleAssignment>();
    public virtual ICollection<GroupOrganizationRoleAssignment> GroupOrganizationRoleAssignments { get; set; } = new List<GroupOrganizationRoleAssignment>();
    public virtual ICollection<GroupStackRoleAssignment> GroupStackRoleAssignments { get; set; } = new List<GroupStackRoleAssignment>();
    public virtual ICollection<GroupNamespaceRoleAssignment> GroupNamespaceRoleAssignments { get; set; } = new List<GroupNamespaceRoleAssignment>();
    public virtual ICollection<GroupModuleRoleAssignment> GroupModuleRoleAssignments { get; set; } = new List<GroupModuleRoleAssignment>();

    public virtual ICollection<RunnerRoleAssignment> RunnerRoleAssignments { get; set; } = new List<RunnerRoleAssignment>();

    public virtual ICollection<RunnerModuleAssignment> RunnerModuleAssignments { get; set; } = new List<RunnerModuleAssignment>();
    public virtual ICollection<RunnerNamespaceAssignment> RunnerNamespaceAssignments { get; set; } = new List<RunnerNamespaceAssignment>();
    public virtual ICollection<RunnerStackAssignment> RunnerStackAssignments { get; set; } = new List<RunnerStackAssignment>();
    public virtual ICollection<DependsOnModule> DependsOnModules { get; set; } = new List<DependsOnModule>();
}

public enum OrganizationStatus
{
    Active,
    Suspended,
    Cancelled,
    PendingDeletion
}