using SnapCd.Contracts.Interfaces;

namespace SnapCd.Contracts.Dto.RoleAssignments.Base;

public class NamespaceRoleAssignmentReadDto : NamespaceRoleAssignmentCreateDto, IDto
{
    public Guid Id { get; set; }
}