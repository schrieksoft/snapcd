using SnapCd.Server.Core.Entities.Definition.RoleAssignments.Org.Base;
using SnapCd.Server.Core.Entities.Interfaces;

namespace SnapCd.Server.Core.Entities.Definition.RoleAssignments.Org;

public class ServicePrincipalOrganizationRoleAssignment : OrganizationRoleAssignment, IOrganizationRoleAssignment, IOrganizationChild
{
    public Guid ServicePrincipalId { get; set; }

    public ServicePrincipal ServicePrincipal { get; set; } = null!;
}