using SnapCd.Contracts.Interfaces;

namespace SnapCd.Contracts.Dto.RoleAssignments;

public class ServicePrincipalOrganizationRoleAssignmentUpdateDto : ServicePrincipalOrganizationRoleAssignmentCreateDto, IUpdateDto
{
    public Guid Id { get; set; }
}
