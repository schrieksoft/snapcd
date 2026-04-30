using SnapCd.Contracts.Dto.RoleAssignments;
using SnapCd.Server.Core.Entities.Definition.RoleAssignments.Org;

namespace SnapCd.Server.Core.Mappers.RoleAssignments;

public static class ServicePrincipalModuleRoleAssignmentMapper
{
    public static ServicePrincipalModuleRoleAssignment ToEntity(ServicePrincipalModuleRoleAssignmentCreateDto dto, Guid organizationId)
    {
        return new ServicePrincipalModuleRoleAssignment
        {
            Id = Guid.NewGuid(),
            OrganizationId = organizationId,
            ServicePrincipalId = dto.ServicePrincipalId,
            ModuleId = dto.ModuleId,
            RoleName = dto.RoleName
        };
    }

    public static ServicePrincipalModuleRoleAssignmentReadDto ToDto(ServicePrincipalModuleRoleAssignment entity)
    {
        return new ServicePrincipalModuleRoleAssignmentReadDto
        {
            Id = entity.Id,
            ServicePrincipalId = entity.ServicePrincipalId,
            ModuleId = entity.ModuleId,
            RoleName = entity.RoleName
        };
    }

    public static void UpdateEntity(ServicePrincipalModuleRoleAssignment entity, ServicePrincipalModuleRoleAssignmentUpdateDto dto)
    {
        entity.ServicePrincipalId = dto.ServicePrincipalId;
        entity.ModuleId = dto.ModuleId;
        entity.RoleName = dto.RoleName;
    }
}