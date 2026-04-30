using SnapCd.Contracts.Dto.RoleAssignments;
using SnapCd.Server.Core.Entities.Definition.RoleAssignments.Org;

namespace SnapCd.Server.Core.Mappers.RoleAssignments;

public static class ServicePrincipalStackRoleAssignmentMapper
{
    public static ServicePrincipalStackRoleAssignment ToEntity(ServicePrincipalStackRoleAssignmentCreateDto dto, Guid organizationId)
    {
        return new ServicePrincipalStackRoleAssignment
        {
            Id = Guid.NewGuid(),
            OrganizationId = organizationId,
            ServicePrincipalId = dto.ServicePrincipalId,
            StackId = dto.StackId,
            RoleName = dto.RoleName
        };
    }

    public static ServicePrincipalStackRoleAssignmentReadDto ToDto(ServicePrincipalStackRoleAssignment entity)
    {
        return new ServicePrincipalStackRoleAssignmentReadDto
        {
            Id = entity.Id,
            ServicePrincipalId = entity.ServicePrincipalId,
            StackId = entity.StackId,
            RoleName = entity.RoleName
        };
    }

    public static void UpdateEntity(ServicePrincipalStackRoleAssignment entity, ServicePrincipalStackRoleAssignmentUpdateDto dto)
    {
        entity.ServicePrincipalId = dto.ServicePrincipalId;
        entity.StackId = dto.StackId;
        entity.RoleName = dto.RoleName;
    }
}