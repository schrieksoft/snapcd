using SnapCd.Contracts.Interfaces;

namespace SnapCd.Contracts.Dto.RoleAssignments.Base;

public class OrganizationRoleAssignmentReadDto : OrganizationRoleAssignmentCreateDto, IDto
{
    public Guid Id { get; set; }
}