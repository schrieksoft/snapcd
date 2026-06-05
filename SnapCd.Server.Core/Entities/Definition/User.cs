// SPDX-License-Identifier: LicenseRef-Snap-CD-Source-Available-1.1
// Copyright (c) 2026 Karl Schriek / Schrieksoft.
// No license is granted to use this file, in whole or in part, (a) as training, fine-tuning, retrieval, or
// embedding data for any machine-learning model, or (b) as input to any machine-learning model, agent, or automated
// system for the purpose of producing a derivative work or reimplementation that is not otherwise permitted by the
// Snap CD Source-Available License (including any Competing Product as defined therein). Contact info@snapcd.io
// for terms covering either use.

using Microsoft.AspNetCore.Identity;
using SnapCd.Server.Core.Entities.Definition.RoleAssignments.System;
using SnapCd.Server.Core.Entities.Interfaces;
using SnapCd.Server.Core.Enums;

namespace SnapCd.Server.Core.Entities.Definition;

public class User : IdentityUser<Guid>, ISystemEntity
{
    public bool IsDisabled { get; set; } = false;

    public bool IsRegistrationNotCompleted { get; set; } = false;
    public DateTime? InvitationCreatedDateTime { get; set; }

    public string? FirstName { get; set; }

    public string? LastName { get; set; }

    public int? OrganizationQuotaOverride { get; set; }

    public Guid CreatedBy { get; set; }
    public AuditPrincipalDiscriminator CreatedByPrincipalDiscriminator { get; set; }
    public Guid? CreatedByAgentId { get; set; }
    public DateTime CreatedDateTime { get; set; }
    public Guid ModifiedBy { get; set; }
    public AuditPrincipalDiscriminator ModifiedByPrincipalDiscriminator { get; set; }
    public Guid? ModifiedByAgentId { get; set; }
    public DateTime ModifiedDateTime { get; set; }

    public virtual ICollection<UserSystemRoleAssignment> UserSystemRoleAssignments { get; set; } = new List<UserSystemRoleAssignment>();
    public virtual ICollection<OrganizationUser> OrganizationUsers { get; set; } = new List<OrganizationUser>();
}