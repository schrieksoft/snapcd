namespace SnapCd.Contracts.Dto.RunnerNamespaceAssignments;

public class RunnerNamespaceAssignmentCreateDto
{
    public Guid NamespaceId { get; set; }

    public Guid RunnerId { get; set; }
}
