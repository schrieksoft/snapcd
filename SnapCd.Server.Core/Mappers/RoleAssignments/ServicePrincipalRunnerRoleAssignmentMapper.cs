using SnapCd.Contracts.Dto.RoleAssignments;
using SnapCd.Server.Core.Entities.Definition.RoleAssignments.Org;

namespace SnapCd.Server.Core.Mappers.RoleAssignments;

public static class ServicePrincipalRunnerRoleAssignmentMapper
{
    public static ServicePrincipalRunnerRoleAssignment ToEntity(ServicePrincipalRunnerRoleAssignmentCreateDto dto, Guid organizationId)
    {
        return new ServicePrincipalRunnerRoleAssignment
        {
            Id = Guid.NewGuid(),
            OrganizationId = organizationId,
            ServicePrincipalId = dto.ServicePrincipalId,
            RunnerId = dto.RunnerId,
            RoleName = dto.RoleName
        };
    }

    public static ServicePrincipalRunnerRoleAssignmentReadDto ToDto(ServicePrincipalRunnerRoleAssignment entity)
    {
        return new ServicePrincipalRunnerRoleAssignmentReadDto
        {
            Id = entity.Id,
            ServicePrincipalId = entity.ServicePrincipalId,
            RunnerId = entity.RunnerId,
            RoleName = entity.RoleName
        };
    }

    public static void UpdateEntity(ServicePrincipalRunnerRoleAssignment entity, ServicePrincipalRunnerRoleAssignmentUpdateDto dto)
    {
        entity.ServicePrincipalId = dto.ServicePrincipalId;
        entity.RunnerId = dto.RunnerId;
        entity.RoleName = dto.RoleName;
    }
}