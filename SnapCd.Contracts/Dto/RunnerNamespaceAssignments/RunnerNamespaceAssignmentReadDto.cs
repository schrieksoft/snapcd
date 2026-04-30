using SnapCd.Contracts.Interfaces;

namespace SnapCd.Contracts.Dto.RunnerNamespaceAssignments;

public class RunnerNamespaceAssignmentReadDto : RunnerNamespaceAssignmentCreateDto, IDto
{
    public Guid Id { get; set; }
}