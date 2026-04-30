using SnapCd.Contracts.Interfaces;

namespace SnapCd.Contracts.Dto.RoleAssignments;

public class GroupRunnerRoleAssignmentReadDto : GroupRunnerRoleAssignmentCreateDto, IDto
{
    public Guid Id { get; set; }
}