// SPDX-License-Identifier: LicenseRef-Snap-CD-Source-Available-1.1
// Copyright (c) 2026 Karl Schriek / Schrieksoft.
// No license is granted to use this file, in whole or in part, (a) as training, fine-tuning, retrieval, or
// embedding data for any machine-learning model, or (b) as input to any machine-learning model, agent, or automated
// system for the purpose of producing a derivative work or reimplementation that is not otherwise permitted by the
// Snap CD Source-Available License (including any Competing Product as defined therein). Contact info@snapcd.io
// for terms covering either use.

using System.ComponentModel.DataAnnotations;
using SnapCd.Server.Core.Entities.Definition.Base;
using SnapCd.Server.Core.Entities.Definition.GroupMembers;
using SnapCd.Server.Core.Entities.Definition.Missions;
using SnapCd.Server.Core.Entities.Definition.RoleAssignments.Org;
using SnapCd.Server.Core.Entities.Definition.RoleAssignments.Org.Base;
using SnapCd.Server.Core.Entities.Definition.RoleAssignments.Org.Runner.Base;
using SnapCd.Server.Core.Entities.Definition.RoleAssignments.Org.Agent.Base;
using SnapCd.Server.Core.Entities.Definition.AgentSupplies;
using SnapCd.Server.Core.Entities.Definition.RunnerSupplies;
using SnapCd.Server.Core.Entities.Definition.Secrets;
using SnapCd.Server.Core.Entities.Interfaces;

namespace SnapCd.Server.Core.Entities.Definition;

public class Organization : AuditBase, ISystemEntity, ICreationTrackable
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
    public virtual ICollection<Agent> Agents { get; set; } = new List<Agent>();
    public virtual ICollection<OrganizationMission> OrganizationMissions { get; set; } = new List<OrganizationMission>();
    public virtual ICollection<StackMission> StackMissions { get; set; } = new List<StackMission>();
    public virtual ICollection<NamespaceMission> NamespaceMissions { get; set; } = new List<NamespaceMission>();
    public virtual ICollection<ModuleMission> ModuleMissions { get; set; } = new List<ModuleMission>();
    public virtual ICollection<SourceRefresherPreselection> SourceRefresherPreselections { get; set; } = new List<SourceRefresherPreselection>();
    public virtual ICollection<Secret> Secrets { get; set; } = new List<Secret>();

    public virtual ICollection<Output> Outputs { get; set; } = new List<Output>();
    public virtual ICollection<OutputSet> OutputSets { get; set; } = new List<OutputSet>();
    public virtual ICollection<Variable> Variables { get; set; } = new List<Variable>();
    public virtual ICollection<VariableSet> VariableSets { get; set; } = new List<VariableSet>();

    public virtual ICollection<ModuleJob> ModuleJobs { get; set; } = new List<ModuleJob>();
    public virtual ICollection<ModuleJobApproval> ModuleJobApprovals { get; set; } = new List<ModuleJobApproval>();
    public virtual ICollection<ModuleJobMission> ModuleJobMissions { get; set; } = new List<ModuleJobMission>();
    public virtual ICollection<ModuleJobMissionRun> ModuleJobMissionRuns { get; set; } = new List<ModuleJobMissionRun>();
    public virtual ICollection<ModuleExtraFile> ModuleExtraFiles { get; set; } = new List<ModuleExtraFile>();
    public virtual ICollection<ModuleAdditionalTriggerPath> ModuleAdditionalTriggerPaths { get; set; } = new List<ModuleAdditionalTriggerPath>();
    public virtual ICollection<NamespaceAdditionalTriggerPath> NamespaceAdditionalTriggerPaths { get; set; } = new List<NamespaceAdditionalTriggerPath>();
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
    public virtual ICollection<AgentRoleAssignment> AgentRoleAssignments { get; set; } = new List<AgentRoleAssignment>();

    public virtual ICollection<RunnerModuleSupply> RunnerModuleSupplies { get; set; } = new List<RunnerModuleSupply>();
    public virtual ICollection<RunnerNamespaceSupply> RunnerNamespaceSupplies { get; set; } = new List<RunnerNamespaceSupply>();
    public virtual ICollection<RunnerStackSupply> RunnerStackSupplies { get; set; } = new List<RunnerStackSupply>();

    public virtual ICollection<AgentModuleSupply> AgentModuleSupplies { get; set; } = new List<AgentModuleSupply>();
    public virtual ICollection<AgentNamespaceSupply> AgentNamespaceSupplies { get; set; } = new List<AgentNamespaceSupply>();
    public virtual ICollection<AgentStackSupply> AgentStackSupplies { get; set; } = new List<AgentStackSupply>();
    public virtual ICollection<DependsOnModule> DependsOnModules { get; set; } = new List<DependsOnModule>();
}

public enum OrganizationStatus
{
    Active,
    Suspended,
    Cancelled,
    PendingDeletion
}