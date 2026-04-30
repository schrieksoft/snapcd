namespace SnapCd.Contracts.Dto.RunnerModuleAssignments;

public class RunnerModuleAssignmentCreateDto
{
    public Guid ModuleId { get; set; }

    public Guid RunnerId { get; set; }
}
