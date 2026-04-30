namespace SnapCd.Contracts.Dto.RoleAssignments.Base;

public class NamespaceRoleAssignmentCreateDto
{

    public Guid NamespaceId { get; set; }

    public Guid PrincipalId { get; set; }

    public RoleAssignmentPrincipalDiscriminator PrincipalDiscriminator { get; set; }

    public NamespaceRole RoleName { get; set; }
}
