using SnapCd.Server.Core.Entities.Definition.RoleAssignments.Org.Runner.Base;
using SnapCd.Server.Core.Entities.Interfaces;

namespace SnapCd.Server.Core.Entities.Definition.RoleAssignments.Org;

public class UserRunnerRoleAssignment : RunnerRoleAssignment, IRunnerRoleAssignment
{
    public Guid UserId { get; set; }

    public OrganizationUser OrganizationUser { get; set; } = null!;
}