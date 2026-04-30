namespace SnapCd.Contracts.Dto.RoleAssignments;

public class UserStackRoleAssignmentCreateDto
{
    public Guid UserId { get; set; }

    public Guid StackId { get; set; }

    public StackRole RoleName { get; set; }
}
