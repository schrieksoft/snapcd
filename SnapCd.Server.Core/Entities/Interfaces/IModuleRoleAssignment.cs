using SnapCd.Contracts;

namespace SnapCd.Server.Core.Entities.Interfaces;

public interface IModuleRoleAssignment : IRoleAssignment
{
    public Guid ModuleId { get; set; }

    public ModuleRole RoleName { get; set; }
}