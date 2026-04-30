using SnapCd.Contracts.Interfaces;

namespace SnapCd.Contracts.Dto.RoleAssignments;

public class UserStackRoleAssignmentReadDto : UserStackRoleAssignmentCreateDto, IDto
{
    public Guid Id { get; set; }
}