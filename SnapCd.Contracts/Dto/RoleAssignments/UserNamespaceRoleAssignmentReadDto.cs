using SnapCd.Contracts.Interfaces;

namespace SnapCd.Contracts.Dto.RoleAssignments;

public class UserNamespaceRoleAssignmentReadDto : UserNamespaceRoleAssignmentCreateDto, IDto
{
    public Guid Id { get; set; }
}