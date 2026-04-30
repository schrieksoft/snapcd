using System.Collections.ObjectModel;
using OpenIddict.EntityFrameworkCore.Models;
using SnapCd.Server.Core.Entities.Definition.GroupMembers;
using SnapCd.Server.Core.Entities.Definition.RoleAssignments.Org;
using SnapCd.Server.Core.Entities.Definition.RoleAssignments.System;
using SnapCd.Server.Core.Entities.Interfaces;
using SnapCd.Server.Core.Enums;

namespace SnapCd.Server.Core.Entities.Definition;

public class ServicePrincipal : OpenIddictEntityFrameworkCoreApplication<Guid, Authorization, Token>, IEntity, IOrganizationChild
{
    public Guid OrganizationId { get; set; }

    // Navigation property (required)
    public virtual Organization Organization { get; set; } = null!;

    // NEW Role Assignment navigation properties
    public virtual ICollection<ServicePrincipalOrganizationRoleAssignment> ServicePrincipalOrganizationRoleAssignments { get; set; } = new List<ServicePrincipalOrganizationRoleAssignment>();
    public virtual ICollection<ServicePrincipalStackRoleAssignment> ServicePrincipalStackRoleAssignments { get; set; } = new List<ServicePrincipalStackRoleAssignment>();
    public virtual ICollection<ServicePrincipalNamespaceRoleAssignment> ServicePrincipalNamespaceRoleAssignments { get; set; } = new List<ServicePrincipalNamespaceRoleAssignment>();
    public virtual ICollection<ServicePrincipalModuleRoleAssignment> ServicePrincipalModuleRoleAssignments { get; set; } = new List<ServicePrincipalModuleRoleAssignment>();
    public virtual ICollection<ServicePrincipalRunnerRoleAssignment> ServicePrincipalRunnerRoleAssignments { get; set; } = new List<ServicePrincipalRunnerRoleAssignment>();
    public virtual ICollection<ServicePrincipalSystemRoleAssignment> ServicePrincipalSystemRoleAssignments { get; set; } = new List<ServicePrincipalSystemRoleAssignment>();

    // Runners directly assigned to this ServicePrincipal
    public virtual ICollection<Runner> Runners { get; set; } = new List<Runner>();

    // Group organizationUser navigation property
    public virtual ICollection<ServicePrincipalGroupMember> ServicePrincipalGroupMembers { get; set; } = new List<ServicePrincipalGroupMember>();

    // Non-nullable OrganizationId for ServicePrincipal - organization-scoped entities

    public override ICollection<Token> Tokens { get; } = new ObservableCollection<Token>();

    public override ICollection<Authorization> Authorizations { get; } =
        new ObservableCollection<Authorization>();

    public override Guid Id { get; set; } = Guid.NewGuid();

    public override string? ClientType { get; set; } = "confidential";

    public override string? ConcurrencyToken { get; set; } = Guid.NewGuid().ToString();

    public bool IsDisabled { get; set; }

    /// <summary>
    /// Returns the ClientId without the organization prefix.
    /// Storage format is "{organizationId}:{clientId}", this returns just "{clientId}".
    /// </summary>
    public string? DisplayClientId => ClientId != null && ClientId.Contains(':')
        ? ClientId[(ClientId.IndexOf(':') + 1)..]
        : ClientId;

    // Audit fields
    public Guid CreatedBy { get; set; }
    public AuditPrincipalDiscriminator CreatedByPrincipalDiscriminator { get; set; }
    public DateTime CreatedDateTime { get; set; }
    public Guid ModifiedBy { get; set; }
    public AuditPrincipalDiscriminator ModifiedByPrincipalDiscriminator { get; set; }
    public DateTime ModifiedDateTime { get; set; }

    public Guid ParentId()
    {
        return OrganizationId;
    }
}