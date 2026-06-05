// SPDX-License-Identifier: LicenseRef-Snap-CD-Source-Available-1.1
// Copyright (c) 2026 Karl Schriek / Schrieksoft.
// No license is granted to use this file, in whole or in part, (a) as training, fine-tuning, retrieval, or
// embedding data for any machine-learning model, or (b) as input to any machine-learning model, agent, or automated
// system for the purpose of producing a derivative work or reimplementation that is not otherwise permitted by the
// Snap CD Source-Available License (including any Competing Product as defined therein). Contact info@snapcd.io
// for terms covering either use.

using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;
using SnapCd.Server.Core.Entities.Definition.Base;
using SnapCd.Server.Core.Entities.Definition.GroupMembers;
using SnapCd.Server.Core.Entities.Definition.RoleAssignments.Org;
using SnapCd.Server.Core.Entities.Interfaces;

namespace SnapCd.Server.Core.Entities.Definition;

public class OrganizationUser : AuditBase, IEntity, IOrganizationChild
{
    public Guid Id { get; set; }
    public Guid OrganizationId { get; set; }
    public Guid UserId { get; set; }
    public DateTime JoinedAt { get; set; }
    public DateTime? LastAccessedAt { get; set; }

    public bool IsDeactivated { get; set; } = false;

    [MaxLength(255)] public string? InvitationToken { get; set; }
    public DateTime? InvitationSentDateTime { get; set; }
    public DateTime? InvitationExpirationDateTime { get; set; }
    public bool InvitationCompleted { get; set; } = false;
    public DateTime? InvitationCompletedDateTime { get; set; }

    [JsonIgnore] public virtual Organization Organization { get; set; } = null!;
    [JsonIgnore] public virtual User User { get; set; } = null!;

    [JsonIgnore] public virtual ICollection<UserOrganizationRoleAssignment> UserOrganizationRoleAssignments { get; set; } = new List<UserOrganizationRoleAssignment>();
    [JsonIgnore] public virtual ICollection<UserStackRoleAssignment> UserStackRoleAssignments { get; set; } = new List<UserStackRoleAssignment>();
    [JsonIgnore] public virtual ICollection<UserNamespaceRoleAssignment> UserNamespaceRoleAssignments { get; set; } = new List<UserNamespaceRoleAssignment>();
    [JsonIgnore] public virtual ICollection<UserModuleRoleAssignment> UserModuleRoleAssignments { get; set; } = new List<UserModuleRoleAssignment>();
    [JsonIgnore] public virtual ICollection<UserRunnerRoleAssignment> UserRunnerRoleAssignments { get; set; } = new List<UserRunnerRoleAssignment>();
    [JsonIgnore] public virtual ICollection<UserAgentRoleAssignment> UserAgentRoleAssignments { get; set; } = new List<UserAgentRoleAssignment>();

    // Group organizationUser navigation property
    [JsonIgnore] public virtual ICollection<UserGroupMember> UserGroupMembers { get; set; } = new List<UserGroupMember>();

    public Guid ParentId()
    {
        return OrganizationId;
    }
}