using SnapCd.Contracts.Interfaces;

namespace SnapCd.Contracts.Dto.RunnerModuleAssignments;

public class RunnerModuleAssignmentReadDto : RunnerModuleAssignmentCreateDto, IDto
{
    public Guid Id { get; set; }
}