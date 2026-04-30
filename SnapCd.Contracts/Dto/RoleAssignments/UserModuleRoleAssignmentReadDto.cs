using SnapCd.Contracts.Interfaces;

namespace SnapCd.Contracts.Dto.RoleAssignments;

public class UserModuleRoleAssignmentReadDto : UserModuleRoleAssignmentCreateDto, IDto
{
    public Guid Id { get; set; }
}