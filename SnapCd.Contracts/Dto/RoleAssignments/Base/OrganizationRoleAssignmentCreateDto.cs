namespace SnapCd.Contracts.Dto.RoleAssignments.Base;

public class OrganizationRoleAssignmentCreateDto
{

    public Guid PrincipalId { get; set; }

    public RoleAssignmentPrincipalDiscriminator PrincipalDiscriminator { get; set; }

    public OrganizationRole RoleName { get; set; }
}
