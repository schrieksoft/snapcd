namespace SnapCd.Contracts.Dto.RoleAssignments;

public class ServicePrincipalStackRoleAssignmentCreateDto
{
    public Guid ServicePrincipalId { get; set; }

    public Guid StackId { get; set; }

    public StackRole RoleName { get; set; }
}
