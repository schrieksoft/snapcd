using SnapCd.Contracts.Interfaces;

namespace SnapCd.Contracts.Dto.RoleAssignments;

public class UserOrganizationRoleAssignmentUpdateDto : UserOrganizationRoleAssignmentCreateDto, IUpdateDto
{
    public Guid Id { get; set; }
}
