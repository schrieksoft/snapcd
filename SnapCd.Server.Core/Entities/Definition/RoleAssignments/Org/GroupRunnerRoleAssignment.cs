using SnapCd.Server.Core.Entities.Definition.RoleAssignments.Org.Runner.Base;
using SnapCd.Server.Core.Entities.Interfaces;

namespace SnapCd.Server.Core.Entities.Definition.RoleAssignments.Org;

public class GroupRunnerRoleAssignment : RunnerRoleAssignment, IRunnerRoleAssignment
{
    public Guid GroupId { get; set; }

    public Group Group { get; set; } = null!;
}