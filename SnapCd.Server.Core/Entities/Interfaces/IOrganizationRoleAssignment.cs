using SnapCd.Contracts;

namespace SnapCd.Server.Core.Entities.Interfaces;

public interface IOrganizationRoleAssignment : IRoleAssignment
{
    public OrganizationRole RoleName { get; set; }
}