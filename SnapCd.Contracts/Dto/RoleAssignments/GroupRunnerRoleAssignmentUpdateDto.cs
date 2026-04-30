using SnapCd.Contracts.Interfaces;

namespace SnapCd.Contracts.Dto.RoleAssignments;

public class GroupRunnerRoleAssignmentUpdateDto : GroupRunnerRoleAssignmentCreateDto, IUpdateDto
{
    public Guid Id { get; set; }
}
