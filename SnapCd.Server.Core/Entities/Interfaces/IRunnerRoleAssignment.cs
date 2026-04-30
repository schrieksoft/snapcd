using SnapCd.Contracts;

namespace SnapCd.Server.Core.Entities.Interfaces;

public interface IRunnerRoleAssignment : IRoleAssignment
{
    public Guid RunnerId { get; set; }

    public RunnerRole RoleName { get; set; }
}