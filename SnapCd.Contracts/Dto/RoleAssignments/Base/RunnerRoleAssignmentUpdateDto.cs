using SnapCd.Contracts.Interfaces;

namespace SnapCd.Contracts.Dto.RoleAssignments.Base;

public class RunnerRoleAssignmentUpdateDto : RunnerRoleAssignmentCreateDto, IUpdateDto
{
    public Guid Id { get; set; }
}
