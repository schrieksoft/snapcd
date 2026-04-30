using SnapCd.Contracts;

namespace SnapCd.Server.Core.Entities.Interfaces;

public interface INamespaceRoleAssignment : IRoleAssignment
{
    public Guid NamespaceId { get; set; }

    public NamespaceRole RoleName { get; set; }
}
