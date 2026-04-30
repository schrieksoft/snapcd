using SnapCd.Contracts.Interfaces;

namespace SnapCd.Contracts.Dto.RoleAssignments;

public class UserModuleRoleAssignmentUpdateDto : UserModuleRoleAssignmentCreateDto, IUpdateDto
{
    public Guid Id { get; set; }
}
