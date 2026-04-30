namespace SnapCd.Contracts.Dto.RunnerStackAssignments;

public class RunnerStackAssignmentCreateDto
{
    public Guid StackId { get; set; }

    public Guid RunnerId { get; set; }
}
