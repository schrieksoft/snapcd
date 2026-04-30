using SnapCd.Server.Core.Enums;

namespace SnapCd.Server.Core.Entities.Interfaces;

public interface ISystemRoleAssignment
{
    public Guid PrincipalId { get; set; }

    public SystemRole RoleName { get; set; }
}