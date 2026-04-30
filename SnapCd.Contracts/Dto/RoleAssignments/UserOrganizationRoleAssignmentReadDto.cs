using SnapCd.Contracts.Interfaces;

namespace SnapCd.Contracts.Dto.RoleAssignments;

public class UserOrganizationRoleAssignmentReadDto : UserOrganizationRoleAssignmentCreateDto, IDto
{
    public Guid Id { get; set; }
}