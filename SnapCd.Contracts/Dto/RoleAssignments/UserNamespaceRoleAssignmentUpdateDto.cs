using SnapCd.Contracts.Interfaces;

namespace SnapCd.Contracts.Dto.RoleAssignments;

public class UserNamespaceRoleAssignmentUpdateDto : UserNamespaceRoleAssignmentCreateDto, IUpdateDto
{
    public Guid Id { get; set; }
}
