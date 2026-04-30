using SnapCd.Contracts.Interfaces;

namespace SnapCd.Contracts.Dto.RoleAssignments;

public class GroupNamespaceRoleAssignmentReadDto : GroupNamespaceRoleAssignmentCreateDto, IDto
{
    public Guid Id { get; set; }
}