namespace SnapCd.Contracts.Dto.RoleAssignments;

public class GroupStackRoleAssignmentCreateDto
{
    public Guid GroupId { get; set; }

    public Guid StackId { get; set; }

    public StackRole RoleName { get; set; }
}
