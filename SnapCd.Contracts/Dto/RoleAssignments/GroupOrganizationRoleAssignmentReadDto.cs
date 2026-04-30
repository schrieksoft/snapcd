using SnapCd.Contracts.Interfaces;

namespace SnapCd.Contracts.Dto.RoleAssignments;

public class GroupOrganizationRoleAssignmentReadDto : GroupOrganizationRoleAssignmentCreateDto, IDto
{
    public Guid Id { get; set; }
}