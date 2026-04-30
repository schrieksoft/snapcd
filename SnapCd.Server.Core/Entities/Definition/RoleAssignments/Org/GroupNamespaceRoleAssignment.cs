using SnapCd.Server.Core.Entities.Definition.RoleAssignments.Org.Base;
using SnapCd.Server.Core.Entities.Interfaces;

namespace SnapCd.Server.Core.Entities.Definition.RoleAssignments.Org;

public class GroupNamespaceRoleAssignment : NamespaceRoleAssignment, INamespaceRoleAssignment, INamespaceChild
{
    public Guid GroupId { get; set; }

    public Group Group { get; set; } = null!;
}