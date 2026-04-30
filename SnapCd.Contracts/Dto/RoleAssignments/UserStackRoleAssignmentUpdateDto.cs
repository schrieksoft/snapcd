using SnapCd.Contracts.Interfaces;

namespace SnapCd.Contracts.Dto.RoleAssignments;

public class UserStackRoleAssignmentUpdateDto : UserStackRoleAssignmentCreateDto, IUpdateDto
{
    public Guid Id { get; set; }
}
