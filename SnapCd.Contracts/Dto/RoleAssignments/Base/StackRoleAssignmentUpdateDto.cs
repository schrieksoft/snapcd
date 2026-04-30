using SnapCd.Contracts.Interfaces;

namespace SnapCd.Contracts.Dto.RoleAssignments.Base;

public class StackRoleAssignmentUpdateDto : StackRoleAssignmentCreateDto, IUpdateDto
{
    public Guid Id { get; set; }
}
