namespace SnapCd.Contracts.Dto.RunnerServicePrincipalAssignments;

public class RunnerServicePrincipalAssignmentCreateDto
{
    public Guid ServicePrincipalId { get; set; }

    public Guid RunnerId { get; set; }
}
