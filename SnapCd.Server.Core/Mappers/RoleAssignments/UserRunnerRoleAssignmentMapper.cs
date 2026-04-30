using SnapCd.Contracts.Dto.RoleAssignments;
using SnapCd.Server.Core.Entities.Definition.RoleAssignments.Org;

namespace SnapCd.Server.Core.Mappers.RoleAssignments;

public static class UserRunnerRoleAssignmentMapper
{
    public static UserRunnerRoleAssignment ToEntity(UserRunnerRoleAssignmentCreateDto dto, Guid organizationId)
    {
        return new UserRunnerRoleAssignment
        {
            Id = Guid.NewGuid(),
            OrganizationId = organizationId,
            UserId = dto.UserId,
            RunnerId = dto.RunnerId,
            RoleName = dto.RoleName
        };
    }

    public static UserRunnerRoleAssignmentReadDto ToDto(UserRunnerRoleAssignment entity)
    {
        return new UserRunnerRoleAssignmentReadDto
        {
            Id = entity.Id,
            UserId = entity.UserId,
            RunnerId = entity.RunnerId,
            RoleName = entity.RoleName
        };
    }

    public static void UpdateEntity(UserRunnerRoleAssignment entity, UserRunnerRoleAssignmentUpdateDto dto)
    {
        entity.UserId = dto.UserId;
        entity.RunnerId = dto.RunnerId;
        entity.RoleName = dto.RoleName;
    }
}