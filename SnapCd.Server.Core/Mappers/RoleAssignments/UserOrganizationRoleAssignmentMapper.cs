using SnapCd.Contracts.Dto.RoleAssignments;
using SnapCd.Server.Core.Entities.Definition.RoleAssignments.Org;

namespace SnapCd.Server.Core.Mappers.RoleAssignments;

public static class UserOrganizationRoleAssignmentMapper
{
    public static UserOrganizationRoleAssignment ToEntity(UserOrganizationRoleAssignmentCreateDto dto, Guid organizationId)
    {
        return new UserOrganizationRoleAssignment
        {
            Id = Guid.NewGuid(),
            OrganizationId = organizationId,
            UserId = dto.UserId,
            RoleName = dto.RoleName
        };
    }

    public static UserOrganizationRoleAssignmentReadDto ToDto(UserOrganizationRoleAssignment entity)
    {
        return new UserOrganizationRoleAssignmentReadDto
        {
            Id = entity.Id,
            UserId = entity.UserId,
            RoleName = entity.RoleName
        };
    }

    public static void UpdateEntity(UserOrganizationRoleAssignment entity, UserOrganizationRoleAssignmentUpdateDto dto)
    {
        entity.UserId = dto.UserId;
        entity.RoleName = dto.RoleName;
    }
}