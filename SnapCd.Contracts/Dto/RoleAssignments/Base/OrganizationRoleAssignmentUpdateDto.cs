using SnapCd.Contracts.Interfaces;

namespace SnapCd.Contracts.Dto.RoleAssignments.Base;

public class OrganizationRoleAssignmentUpdateDto : OrganizationRoleAssignmentCreateDto, IUpdateDto
{
    public Guid Id { get; set; }
}
