using SnapCd.Contracts.Interfaces;

namespace SnapCd.Contracts.Dto.RoleAssignments;

public class GroupModuleRoleAssignmentReadDto : GroupModuleRoleAssignmentCreateDto, IDto
{
    public Guid Id { get; set; }
}