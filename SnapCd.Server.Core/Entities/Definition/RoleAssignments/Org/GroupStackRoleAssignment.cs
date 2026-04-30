using SnapCd.Server.Core.Entities.Definition.RoleAssignments.Org.Base;
using SnapCd.Server.Core.Entities.Interfaces;

namespace SnapCd.Server.Core.Entities.Definition.RoleAssignments.Org;

public class GroupStackRoleAssignment : StackRoleAssignment, IStackRoleAssignment, IStackChild
{
    public Guid GroupId { get; set; }

    public Group Group { get; set; } = null!;
}