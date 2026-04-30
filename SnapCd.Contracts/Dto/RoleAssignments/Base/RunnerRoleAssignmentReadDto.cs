using SnapCd.Contracts.Interfaces;

namespace SnapCd.Contracts.Dto.RoleAssignments.Base;

public class RunnerRoleAssignmentReadDto : RunnerRoleAssignmentCreateDto, IDto
{
    public Guid Id { get; set; }
}