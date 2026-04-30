using SnapCd.Contracts.Interfaces;

namespace SnapCd.Contracts.Dto.RoleAssignments;

public class ServicePrincipalStackRoleAssignmentReadDto : ServicePrincipalStackRoleAssignmentCreateDto, IDto
{
    public Guid Id { get; set; }
}