using SnapCd.Contracts.Interfaces;

namespace SnapCd.Contracts.Dto.RoleAssignments;

public class ServicePrincipalStackRoleAssignmentUpdateDto : ServicePrincipalStackRoleAssignmentCreateDto, IUpdateDto
{
    public Guid Id { get; set; }
}
