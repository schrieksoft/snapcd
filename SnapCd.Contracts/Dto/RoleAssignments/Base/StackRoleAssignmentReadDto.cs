using SnapCd.Contracts.Interfaces;

namespace SnapCd.Contracts.Dto.RoleAssignments.Base;

public class StackRoleAssignmentDto : StackRoleAssignmentCreateDto, IDto
{
    public Guid Id { get; set; }
}