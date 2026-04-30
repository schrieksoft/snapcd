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
    public DateTime CreatedDateTime { get; set; }
    public Guid ModifiedBy { get; set; }
    public AuditPrincipalDiscriminator ModifiedByPrincipalDiscriminator { get; set; }
    public DateTime ModifiedDateTime { get; set; }

    public virtual ICollection<UserSystemRoleAssignment> UserSystemRoleAssignments { get; set; } = new List<UserSystemRoleAssignment>();
    public virtual ICollection<OrganizationUser> OrganizationUsers { get; set; } = new List<OrganizationUser>();
}