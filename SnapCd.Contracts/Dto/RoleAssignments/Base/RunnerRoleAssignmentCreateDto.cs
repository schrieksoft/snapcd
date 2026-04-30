namespace SnapCd.Contracts.Dto.RoleAssignments.Base;

public class RunnerRoleAssignmentCreateDto
{

    public Guid RunnerId { get; set; }

    public Guid PrincipalId { get; set; }

    public RoleAssignmentPrincipalDiscriminator PrincipalDiscriminator { get; set; }

    public RunnerRole RoleName { get; set; }
}
