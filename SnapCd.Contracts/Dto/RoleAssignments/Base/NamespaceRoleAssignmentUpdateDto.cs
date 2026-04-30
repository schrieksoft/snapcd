using SnapCd.Contracts.Interfaces;

namespace SnapCd.Contracts.Dto.RoleAssignments.Base;

public class NamespaceRoleAssignmentUpdateDto : NamespaceRoleAssignmentCreateDto, IUpdateDto
{
    public Guid Id { get; set; }
}
