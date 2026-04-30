using SnapCd.Contracts.Interfaces;

namespace SnapCd.Contracts.Dto.RoleAssignments;

public class GroupStackRoleAssignmentReadDto : GroupStackRoleAssignmentCreateDto, IDto
{
    public Guid Id { get; set; }
}