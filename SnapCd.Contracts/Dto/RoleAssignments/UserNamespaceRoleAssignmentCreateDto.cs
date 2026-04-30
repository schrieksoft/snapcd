namespace SnapCd.Contracts.Dto.RoleAssignments;

public class UserNamespaceRoleAssignmentCreateDto
{
    public Guid UserId { get; set; }

    public Guid NamespaceId { get; set; }

    public NamespaceRole RoleName { get; set; }
}
