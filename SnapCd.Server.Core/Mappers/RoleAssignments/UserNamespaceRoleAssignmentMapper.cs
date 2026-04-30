using SnapCd.Contracts.Dto.RoleAssignments;
using SnapCd.Server.Core.Entities.Definition.RoleAssignments.Org;

namespace SnapCd.Server.Core.Mappers.RoleAssignments;

public static class UserNamespaceRoleAssignmentMapper
{
    public static UserNamespaceRoleAssignment ToEntity(UserNamespaceRoleAssignmentCreateDto dto, Guid organizationId)
    {
        return new UserNamespaceRoleAssignment
        {
            Id = Guid.NewGuid(),
            OrganizationId = organizationId,
            UserId = dto.UserId,
            NamespaceId = dto.NamespaceId,
            RoleName = dto.RoleName
        };
    }

    public static UserNamespaceRoleAssignmentReadDto ToDto(UserNamespaceRoleAssignment entity)
    {
        return new UserNamespaceRoleAssignmentReadDto
        {
            Id = entity.Id,
            UserId = entity.UserId,
            NamespaceId = entity.NamespaceId,
            RoleName = entity.RoleName
        };
    }

    public static void UpdateEntity(UserNamespaceRoleAssignment entity, UserNamespaceRoleAssignmentUpdateDto dto)
    {
        entity.UserId = dto.UserId;
        entity.NamespaceId = dto.NamespaceId;
        entity.RoleName = dto.RoleName;
    }
}