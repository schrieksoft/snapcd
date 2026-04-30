using SnapCd.Contracts.Interfaces;

namespace SnapCd.Contracts.Dto.RoleAssignments;

public class UserRunnerRoleAssignmentUpdateDto : UserRunnerRoleAssignmentCreateDto, IUpdateDto
{
    public Guid Id { get; set; }
}
