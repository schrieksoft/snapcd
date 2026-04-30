using SnapCd.Server.Core.Entities.Definition.RoleAssignments.Org.Runner.Base;
using SnapCd.Server.Core.Entities.Interfaces;

namespace SnapCd.Server.Core.Entities.Definition.RoleAssignments.Org;

public class ServicePrincipalRunnerRoleAssignment : RunnerRoleAssignment, IRunnerRoleAssignment
{
    public Guid ServicePrincipalId { get; set; }

    public ServicePrincipal ServicePrincipal { get; set; } = null!;
}