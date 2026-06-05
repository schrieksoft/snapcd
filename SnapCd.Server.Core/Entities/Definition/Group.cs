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
using SnapCd.Server.Core.Entities.Definition.RoleAssignments.Org;
using SnapCd.Server.Core.Entities.Interfaces;

namespace SnapCd.Server.Core.Entities.Definition;

public class Group : AuditBase, IEntity, IOrganizationChild

{
    public Guid Id { get; set; }
    public Guid OrganizationId { get; set; }
    [MaxLength(255)] public string Name { get; set; } = null!;

    [MaxLength(800)] public string? Description { get; set; }
    public virtual ICollection<GroupMember>? GroupMembers { get; set; }
    public virtual ICollection<UserGroupMember>? UserGroupMembers { get; set; }
    public virtual ICollection<ServicePrincipalGroupMember>? ServicePrincipalGroupMembers { get; set; }
    public virtual ICollection<GroupGroupMember>? GroupGroupMembersAsParent { get; set; }
    public virtual ICollection<GroupGroupMember>? GroupGroupMembersAsMember { get; set; }

    // Group role assignment navigation properties
    public virtual ICollection<GroupOrganizationRoleAssignment> GroupOrganizationRoleAssignments { get; set; } = new List<GroupOrganizationRoleAssignment>();
    public virtual ICollection<GroupStackRoleAssignment> GroupStackRoleAssignments { get; set; } = new List<GroupStackRoleAssignment>();
    public virtual ICollection<GroupNamespaceRoleAssignment> GroupNamespaceRoleAssignments { get; set; } = new List<GroupNamespaceRoleAssignment>();
    public virtual ICollection<GroupModuleRoleAssignment> GroupModuleRoleAssignments { get; set; } = new List<GroupModuleRoleAssignment>();
    public virtual ICollection<GroupRunnerRoleAssignment> GroupRunnerRoleAssignments { get; set; } = new List<GroupRunnerRoleAssignment>();
    public virtual ICollection<GroupAgentRoleAssignment> GroupAgentRoleAssignments { get; set; } = new List<GroupAgentRoleAssignment>();

    public virtual Organization Organization { get; set; } = null!;

    public Guid ParentId()
    {
        return OrganizationId;
    }
}