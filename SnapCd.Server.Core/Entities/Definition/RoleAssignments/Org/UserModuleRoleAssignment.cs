using SnapCd.Server.Core.Entities.Definition.RoleAssignments.Org.Base;
using SnapCd.Server.Core.Entities.Interfaces;

namespace SnapCd.Server.Core.Entities.Definition.RoleAssignments.Org;

public class UserModuleRoleAssignment : ModuleRoleAssignment, IModuleRoleAssignment, IModuleChild
{
    public Guid UserId { get; set; }

    public OrganizationUser OrganizationUser { get; set; } = null!;
}