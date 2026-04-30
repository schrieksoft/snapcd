using SnapCd.Contracts.Interfaces;

namespace SnapCd.Contracts.Dto.RoleAssignments;

public class UserRunnerRoleAssignmentReadDto : UserRunnerRoleAssignmentCreateDto, IDto
{
    public Guid Id { get; set; }
}