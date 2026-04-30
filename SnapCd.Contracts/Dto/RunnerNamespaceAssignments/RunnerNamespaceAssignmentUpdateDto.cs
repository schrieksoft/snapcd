using SnapCd.Contracts.Interfaces;

namespace SnapCd.Contracts.Dto.RunnerNamespaceAssignments;

public class RunnerNamespaceAssignmentUpdateDto : RunnerNamespaceAssignmentCreateDto, IUpdateDto
{
    public Guid Id { get; set; }
}
