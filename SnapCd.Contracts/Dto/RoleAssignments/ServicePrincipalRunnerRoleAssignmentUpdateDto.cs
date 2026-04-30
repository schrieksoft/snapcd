using SnapCd.Contracts.Interfaces;

namespace SnapCd.Contracts.Dto.RoleAssignments;

public class ServicePrincipalRunnerRoleAssignmentUpdateDto : ServicePrincipalRunnerRoleAssignmentCreateDto, IUpdateDto
{
    public Guid Id { get; set; }
}
