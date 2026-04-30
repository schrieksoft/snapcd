using SnapCd.Contracts.Interfaces;

namespace SnapCd.Contracts.Dto.RunnerModuleAssignments;

public class RunnerModuleAssignmentUpdateDto : RunnerModuleAssignmentCreateDto, IUpdateDto
{
    public Guid Id { get; set; }
}
