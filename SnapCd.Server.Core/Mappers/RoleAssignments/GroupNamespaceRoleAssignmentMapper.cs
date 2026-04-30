using SnapCd.Contracts.Dto.RoleAssignments;
using SnapCd.Server.Core.Entities.Definition.RoleAssignments.Org;

namespace SnapCd.Server.Core.Mappers.RoleAssignments;

public static class GroupNamespaceRoleAssignmentMapper
{
    public static GroupNamespaceRoleAssignment ToEntity(GroupNamespaceRoleAssignmentCreateDto dto, Guid organizationId)
    {
        return new GroupNamespaceRoleAssignment
        {
            Id = Guid.NewGuid(),
            OrganizationId = organizationId,
            GroupId = dto.GroupId,
            NamespaceId = dto.NamespaceId,
            RoleName = dto.RoleName
        };
    }

    public static GroupNamespaceRoleAssignmentReadDto ToDto(GroupNamespaceRoleAssignment entity)
    {
        return new GroupNamespaceRoleAssignmentReadDto
        {
            Id = entity.Id,
            GroupId = entity.GroupId,
            NamespaceId = entity.NamespaceId,
            RoleName = entity.RoleName
        };
    }

    public static void UpdateEntity(GroupNamespaceRoleAssignment entity, GroupNamespaceRoleAssignmentUpdateDto dto)
    {
        entity.GroupId = dto.GroupId;
        entity.NamespaceId = dto.NamespaceId;
        entity.RoleName = dto.RoleName;
    }
}