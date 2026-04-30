namespace SnapCd.Contracts.Dto.RoleAssignments;

public class ServicePrincipalOrganizationRoleAssignmentCreateDto
{
    public Guid ServicePrincipalId { get; set; }

    public OrganizationRole RoleName { get; set; }
}
