namespace SnapCd.Contracts.Dto.RoleAssignments.Base;

public class StackRoleAssignmentCreateDto
{

    public Guid StackId { get; set; }

    public Guid PrincipalId { get; set; }

    public RoleAssignmentPrincipalDiscriminator PrincipalDiscriminator { get; set; }

    public StackRole RoleName { get; set; }
}
