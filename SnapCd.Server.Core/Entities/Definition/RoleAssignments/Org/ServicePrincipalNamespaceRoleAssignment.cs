using SnapCd.Server.Core.Entities.Definition.RoleAssignments.Org.Base;
using SnapCd.Server.Core.Entities.Interfaces;

namespace SnapCd.Server.Core.Entities.Definition.RoleAssignments.Org;

public class ServicePrincipalNamespaceRoleAssignment : NamespaceRoleAssignment, INamespaceRoleAssignment, INamespaceChild
{
    public Guid ServicePrincipalId { get; set; }

    public ServicePrincipal ServicePrincipal { get; set; } = null!;
}