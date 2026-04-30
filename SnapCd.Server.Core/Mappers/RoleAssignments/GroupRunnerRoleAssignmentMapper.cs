using SnapCd.Contracts.Dto.RoleAssignments;
using SnapCd.Server.Core.Entities.Definition.RoleAssignments.Org;

namespace SnapCd.Server.Core.Mappers.RoleAssignments;

public static class GroupRunnerRoleAssignmentMapper
{
    public static GroupRunnerRoleAssignment ToEntity(GroupRunnerRoleAssignmentCreateDto dto, Guid organizationId)
    {
        return new GroupRunnerRoleAssignment
        {
            Id = Guid.NewGuid(),
            OrganizationId = organizationId,
            GroupId = dto.GroupId,
            RunnerId = dto.RunnerId,
            RoleName = dto.RoleName
        };
    }

    public static GroupRunnerRoleAssignmentReadDto ToDto(GroupRunnerRoleAssignment entity)
    {
        return new GroupRunnerRoleAssignmentReadDto
        {
            Id = entity.Id,
            GroupId = entity.GroupId,
            RunnerId = entity.RunnerId,
            RoleName = entity.RoleName
        };
    }

    public static void UpdateEntity(GroupRunnerRoleAssignment entity, GroupRunnerRoleAssignmentUpdateDto dto)
    {
        entity.GroupId = dto.GroupId;
        entity.RunnerId = dto.RunnerId;
        entity.RoleName = dto.RoleName;
    }
}