using SnapCd.Contracts.Dto.RoleAssignments;
using SnapCd.Server.Core.Entities.Definition.RoleAssignments.Org;

namespace SnapCd.Server.Core.Mappers.RoleAssignments;

public static class UserStackRoleAssignmentMapper
{
    public static UserStackRoleAssignment ToEntity(UserStackRoleAssignmentCreateDto dto, Guid organizationId)
    {
        return new UserStackRoleAssignment
        {
            Id = Guid.NewGuid(),
            OrganizationId = organizationId,
            UserId = dto.UserId,
            StackId = dto.StackId,
            RoleName = dto.RoleName
        };
    }

    public static UserStackRoleAssignmentReadDto ToDto(UserStackRoleAssignment entity)
    {
        return new UserStackRoleAssignmentReadDto
        {
            Id = entity.Id,
            UserId = entity.UserId,
            StackId = entity.StackId,
            RoleName = entity.RoleName
        };
    }

    public static void UpdateEntity(UserStackRoleAssignment entity, UserStackRoleAssignmentUpdateDto dto)
    {
        entity.UserId = dto.UserId;
        entity.StackId = dto.StackId;
        entity.RoleName = dto.RoleName;
    }
}