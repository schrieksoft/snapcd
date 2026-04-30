using SnapCd.Contracts;

namespace SnapCd.Server.Core.Entities.Interfaces;

public interface IStackRoleAssignment : IRoleAssignment
{
    public Guid StackId { get; set; }

    public StackRole RoleName { get; set; }
}
