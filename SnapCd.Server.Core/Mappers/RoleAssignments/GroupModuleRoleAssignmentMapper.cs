using SnapCd.Contracts.Dto.RoleAssignments;
using SnapCd.Server.Core.Entities.Definition.RoleAssignments.Org;

namespace SnapCd.Server.Core.Mappers.RoleAssignments;

public static class GroupModuleRoleAssignmentMapper
{
    public static GroupModuleRoleAssignment ToEntity(GroupModuleRoleAssignmentCreateDto dto, Guid organizationId)
    {
        return new GroupModuleRoleAssignment
        {
            Id = Guid.NewGuid(),
            OrganizationId = organizationId,
            GroupId = dto.GroupId,
            ModuleId = dto.ModuleId,
            RoleName = dto.RoleName
        };
    }

    public static GroupModuleRoleAssignmentReadDto ToDto(GroupModuleRoleAssignment entity)
    {
        return new GroupModuleRoleAssignmentReadDto
        {
            Id = entity.Id,
            GroupId = entity.GroupId,
            ModuleId = entity.ModuleId,
            RoleName = entity.RoleName
        };
    }

    public static void UpdateEntity(GroupModuleRoleAssignment entity, GroupModuleRoleAssignmentUpdateDto dto)
    {
        entity.GroupId = dto.GroupId;
        entity.ModuleId = dto.ModuleId;
        entity.RoleName = dto.RoleName;
    }
}