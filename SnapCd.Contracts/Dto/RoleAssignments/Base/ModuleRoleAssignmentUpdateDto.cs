using SnapCd.Contracts.Interfaces;

namespace SnapCd.Contracts.Dto.RoleAssignments.Base;

public class ModuleRoleAssignmentUpdateDto : ModuleRoleAssignmentCreateDto, IUpdateDto
{
    public Guid Id { get; set; }
}
