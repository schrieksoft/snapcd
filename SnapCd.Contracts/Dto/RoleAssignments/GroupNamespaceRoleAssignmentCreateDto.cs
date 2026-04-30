namespace SnapCd.Contracts.Dto.RoleAssignments;

public class GroupNamespaceRoleAssignmentCreateDto
{
    public Guid GroupId { get; set; }

    public Guid NamespaceId { get; set; }

    public NamespaceRole RoleName { get; set; }
}
