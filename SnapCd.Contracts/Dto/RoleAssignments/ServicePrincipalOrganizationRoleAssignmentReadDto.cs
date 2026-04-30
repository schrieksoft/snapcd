using SnapCd.Contracts.Interfaces;

namespace SnapCd.Contracts.Dto.RoleAssignments;

public class ServicePrincipalOrganizationRoleAssignmentReadDto : ServicePrincipalOrganizationRoleAssignmentCreateDto, IDto
{
    public Guid Id { get; set; }
}