using SnapCd.Contracts.Dto.RoleAssignments;
using SnapCd.Server.Core.Entities.Definition.RoleAssignments.Org;

namespace SnapCd.Server.Core.Mappers.RoleAssignments;

public static class ServicePrincipalOrganizationRoleAssignmentMapper
{
    public static ServicePrincipalOrganizationRoleAssignment ToEntity(ServicePrincipalOrganizationRoleAssignmentCreateDto dto, Guid organizationId)
    {
        return new ServicePrincipalOrganizationRoleAssignment
        {
            Id = Guid.NewGuid(),
            OrganizationId = organizationId,
            ServicePrincipalId = dto.ServicePrincipalId,
            RoleName = dto.RoleName
        };
    }

    public static ServicePrincipalOrganizationRoleAssignmentReadDto ToDto(ServicePrincipalOrganizationRoleAssignment entity)
    {
        return new ServicePrincipalOrganizationRoleAssignmentReadDto
        {
            Id = entity.Id,
            ServicePrincipalId = entity.ServicePrincipalId,
            RoleName = entity.RoleName
        };
    }

    public static void UpdateEntity(ServicePrincipalOrganizationRoleAssignment entity, ServicePrincipalOrganizationRoleAssignmentUpdateDto dto)
    {
        entity.ServicePrincipalId = dto.ServicePrincipalId;
        entity.RoleName = dto.RoleName;
    }
}