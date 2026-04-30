namespace SnapCd.Contracts.Dto.RoleAssignments;

public class ServicePrincipalNamespaceRoleAssignmentCreateDto
{
    public Guid ServicePrincipalId { get; set; }

    public Guid NamespaceId { get; set; }

    public NamespaceRole RoleName { get; set; }
}
