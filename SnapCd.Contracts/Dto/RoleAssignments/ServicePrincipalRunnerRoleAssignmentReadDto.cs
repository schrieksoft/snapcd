using SnapCd.Contracts.Interfaces;

namespace SnapCd.Contracts.Dto.RoleAssignments;

public class ServicePrincipalRunnerRoleAssignmentReadDto : ServicePrincipalRunnerRoleAssignmentCreateDto, IDto
{
    public Guid Id { get; set; }
}