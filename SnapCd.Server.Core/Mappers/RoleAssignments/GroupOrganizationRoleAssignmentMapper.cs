using SnapCd.Contracts.Dto.RoleAssignments;
using SnapCd.Server.Core.Entities.Definition.RoleAssignments.Org;

namespace SnapCd.Server.Core.Mappers.RoleAssignments;

public static class GroupOrganizationRoleAssignmentMapper
{
    public static GroupOrganizationRoleAssignment ToEntity(GroupOrganizationRoleAssignmentCreateDto dto, Guid organizationId)
    {
        return new GroupOrganizationRoleAssignment
        {
            Id = Guid.NewGuid(),
            OrganizationId = organizationId,
            GroupId = dto.GroupId,
            RoleName = dto.RoleName
        };
    }

    public static GroupOrganizationRoleAssignmentReadDto ToDto(GroupOrganizationRoleAssignment entity)
    {
        return new GroupOrganizationRoleAssignmentReadDto
        {
            Id = entity.Id,
            GroupId = entity.GroupId,
            RoleName = entity.RoleName
        };
    }

    public static void UpdateEntity(GroupOrganizationRoleAssignment entity, GroupOrganizationRoleAssignmentUpdateDto dto)
    {
        entity.GroupId = dto.GroupId;
        entity.RoleName = dto.RoleName;
    }
}