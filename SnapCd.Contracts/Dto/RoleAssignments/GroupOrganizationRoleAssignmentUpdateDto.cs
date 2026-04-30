using SnapCd.Contracts.Interfaces;

namespace SnapCd.Contracts.Dto.RoleAssignments;

public class GroupOrganizationRoleAssignmentUpdateDto : GroupOrganizationRoleAssignmentCreateDto, IUpdateDto
{
    public Guid Id { get; set; }
}
