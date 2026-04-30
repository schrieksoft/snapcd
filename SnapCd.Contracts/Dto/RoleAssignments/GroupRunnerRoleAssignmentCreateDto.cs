namespace SnapCd.Contracts.Dto.RoleAssignments;

public class GroupRunnerRoleAssignmentCreateDto
{
    public Guid GroupId { get; set; }

    public Guid RunnerId { get; set; }

    public RunnerRole RoleName { get; set; }
}
