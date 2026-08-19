// SPDX-License-Identifier: LicenseRef-Snap-CD-Source-Available-1.1
// Copyright (c) 2026 Karl Schriek / Schrieksoft.
// No license is granted to use this file, in whole or in part, (a) as training, fine-tuning, retrieval, or
// embedding data for any machine-learning model, or (b) as input to any machine-learning model, agent, or automated
// system for the purpose of producing a derivative work or reimplementation that is not otherwise permitted by the
// Snap CD Source-Available License (including any Competing Product as defined therein). Contact info@snapcd.io
// for terms covering either use.

using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;
using SnapCd.Contracts;
using SnapCd.Server.Core.Entities.Definition.Base;
using SnapCd.Server.Core.Entities.Definition.RoleAssignments.Org;
using SnapCd.Server.Core.Entities.Definition.Missions;
using SnapCd.Server.Core.Entities.Definition.RoleAssignments.Org.Base;
using SnapCd.Server.Core.Entities.Definition.AgentSupplies;
using SnapCd.Server.Core.Entities.Definition.RunnerSupplies;
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

    public List<RunnerNamespaceSupply> RunnerNamespaceSupplies { get; set; } = new();

    public List<AgentNamespaceSupply> AgentNamespaceSupplies { get; set; } = new();

    public virtual ICollection<NamespaceMission> NamespaceMissions { get; set; } = new List<NamespaceMission>();
    public List<NamespaceExtraFile> NamespaceExtraFiles { get; set; } = new();

    public List<NamespaceAdditionalTriggerPath> AdditionalTriggerPaths { get; set; } = new();
    public List<NamespaceTerraformInlinePolicy> TerraformInlinePolicies { get; set; } = new();
    public List<NamespaceTerraformRemotePolicy> TerraformRemotePolicies { get; set; } = new();
    public List<NamespaceTerraformLocalPolicy> TerraformLocalPolicies { get; set; } = new();
    public List<NamespacePulumiInlinePolicy> PulumiInlinePolicies { get; set; } = new();
    public List<NamespacePulumiRemotePolicy> PulumiRemotePolicies { get; set; } = new();
    public List<NamespacePulumiLocalPolicy> PulumiLocalPolicies { get; set; } = new();

    // NEW Role Assignment navigation properties
    public virtual ICollection<UserNamespaceRoleAssignment> UserNamespaceRoleAssignments { get; set; } = new List<UserNamespaceRoleAssignment>();
    public virtual ICollection<ServicePrincipalNamespaceRoleAssignment> ServicePrincipalNamespaceRoleAssignments { get; set; } = new List<ServicePrincipalNamespaceRoleAssignment>();
    public virtual ICollection<GroupNamespaceRoleAssignment> GroupNamespaceRoleAssignments { get; set; } = new List<GroupNamespaceRoleAssignment>();

    [JsonIgnore] // So that JSON Serialization does not create a loop
    public virtual Organization Organization { get; set; } = null!;

    [JsonIgnore] // So that JSON Serialization does not create a loop
    public Stack Stack { get; set; } = null!;

    public bool? DefaultCleanInitEnabled { get; set; }

    public bool? DefaultTriggerPathFilterEnabled { get; set; }

    public List<NamespaceHook> Hooks { get; set; } = new();

    public int? DefaultApplyApprovalThreshold { get; set; }

    public int? DefaultDestroyApprovalThreshold { get; set; }

    /// <summary>Default for Modules in this Namespace that do not set their own SplitMonolith threshold.</summary>
    public int? DefaultSplitMonolithApprovalThreshold { get; set; }

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