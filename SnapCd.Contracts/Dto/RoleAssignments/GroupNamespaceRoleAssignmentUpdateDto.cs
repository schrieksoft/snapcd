using SnapCd.Contracts.Interfaces;

namespace SnapCd.Contracts.Dto.RoleAssignments;

public class GroupNamespaceRoleAssignmentUpdateDto : GroupNamespaceRoleAssignmentCreateDto, IUpdateDto
{
    public Guid Id { get; set; }
}
