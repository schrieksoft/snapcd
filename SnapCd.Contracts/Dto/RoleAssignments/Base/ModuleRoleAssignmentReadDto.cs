using SnapCd.Contracts.Interfaces;

namespace SnapCd.Contracts.Dto.RoleAssignments.Base;

public class ModuleRoleAssignmentReadDto : ModuleRoleAssignmentCreateDto, IDto
{
    public Guid Id { get; set; }
}