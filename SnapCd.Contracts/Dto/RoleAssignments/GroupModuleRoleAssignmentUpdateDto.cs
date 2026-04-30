using SnapCd.Contracts.Interfaces;

namespace SnapCd.Contracts.Dto.RoleAssignments;

public class GroupModuleRoleAssignmentUpdateDto : GroupModuleRoleAssignmentCreateDto, IUpdateDto
{
    public Guid Id { get; set; }
}
