using SnapCd.Contracts.Dto.RoleAssignments;
using SnapCd.Server.Core.Entities.Definition.RoleAssignments.Org;

namespace SnapCd.Server.Core.Mappers.RoleAssignments;

public static class ServicePrincipalNamespaceRoleAssignmentMapper
{
    public static ServicePrincipalNamespaceRoleAssignment ToEntity(ServicePrincipalNamespaceRoleAssignmentCreateDto dto, Guid organizationId)
    {
        return new ServicePrincipalNamespaceRoleAssignment
        {
            Id = Guid.NewGuid(),
            OrganizationId = organizationId,
            ServicePrincipalId = dto.ServicePrincipalId,
            NamespaceId = dto.NamespaceId,
            RoleName = dto.RoleName
        };
    }

    public static ServicePrincipalNamespaceRoleAssignmentReadDto ToDto(ServicePrincipalNamespaceRoleAssignment entity)
    {
        return new ServicePrincipalNamespaceRoleAssignmentReadDto
        {
            Id = entity.Id,
            ServicePrincipalId = entity.ServicePrincipalId,
            NamespaceId = entity.NamespaceId,
            RoleName = entity.RoleName
        };
    }

    public static void UpdateEntity(ServicePrincipalNamespaceRoleAssignment entity, ServicePrincipalNamespaceRoleAssignmentUpdateDto dto)
    {
        entity.ServicePrincipalId = dto.ServicePrincipalId;
        entity.NamespaceId = dto.NamespaceId;
        entity.RoleName = dto.RoleName;
    }
}