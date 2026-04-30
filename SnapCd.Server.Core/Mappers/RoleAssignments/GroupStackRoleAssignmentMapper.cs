using SnapCd.Contracts.Dto.RoleAssignments;
using SnapCd.Server.Core.Entities.Definition.RoleAssignments.Org;

namespace SnapCd.Server.Core.Mappers.RoleAssignments;

public static class GroupStackRoleAssignmentMapper
{
    public static GroupStackRoleAssignment ToEntity(GroupStackRoleAssignmentCreateDto dto, Guid organizationId)
    {
        return new GroupStackRoleAssignment
        {
            Id = Guid.NewGuid(),
            OrganizationId = organizationId,
            GroupId = dto.GroupId,
            StackId = dto.StackId,
            RoleName = dto.RoleName
        };
    }

    public static GroupStackRoleAssignmentReadDto ToDto(GroupStackRoleAssignment entity)
    {
        return new GroupStackRoleAssignmentReadDto
        {
            Id = entity.Id,
            GroupId = entity.GroupId,
            StackId = entity.StackId,
            RoleName = entity.RoleName
        };
    }

    public static void UpdateEntity(GroupStackRoleAssignment entity, GroupStackRoleAssignmentUpdateDto dto)
    {
        entity.GroupId = dto.GroupId;
        entity.StackId = dto.StackId;
        entity.RoleName = dto.RoleName;
    }
}