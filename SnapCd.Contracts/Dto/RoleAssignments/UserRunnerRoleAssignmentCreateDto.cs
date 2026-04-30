namespace SnapCd.Contracts.Dto.RoleAssignments;

public class UserRunnerRoleAssignmentCreateDto
{
    public Guid UserId { get; set; }

    public Guid RunnerId { get; set; }

    public RunnerRole RoleName { get; set; }
}
