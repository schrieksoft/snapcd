using SnapCd.Contracts.Dto.RoleAssignments;
using SnapCd.Server.Core.Entities.Definition.RoleAssignments.Org;

namespace SnapCd.Server.Core.Mappers.RoleAssignments;

public static class UserModuleRoleAssignmentMapper
{
    public static UserModuleRoleAssignment ToEntity(UserModuleRoleAssignmentCreateDto dto, Guid organizationId)
    {
        return new UserModuleRoleAssignment
        {
            Id = Guid.NewGuid(),
            OrganizationId = organizationId,
            UserId = dto.UserId,
            ModuleId = dto.ModuleId,
            RoleName = dto.RoleName
        };
    }

    public static UserModuleRoleAssignmentReadDto ToDto(UserModuleRoleAssignment entity)
    {
        return new UserModuleRoleAssignmentReadDto
        {
            Id = entity.Id,
            UserId = entity.UserId,
            ModuleId = entity.ModuleId,
            RoleName = entity.RoleName
        };
    }

    public static void UpdateEntity(UserModuleRoleAssignment entity, UserModuleRoleAssignmentUpdateDto dto)
    {
        entity.UserId = dto.UserId;
        entity.ModuleId = dto.ModuleId;
        entity.RoleName = dto.RoleName;
    }
}