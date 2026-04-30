using SnapCd.Server.Core.Entities.Definition.RoleAssignments.Org.Base;
using SnapCd.Server.Core.Entities.Interfaces;

namespace SnapCd.Server.Core.Entities.Definition.RoleAssignments.Org;

public class UserNamespaceRoleAssignment : NamespaceRoleAssignment, INamespaceRoleAssignment, INamespaceChild
{
    public Guid UserId { get; set; }

    public OrganizationUser OrganizationUser { get; set; } = null!;
}