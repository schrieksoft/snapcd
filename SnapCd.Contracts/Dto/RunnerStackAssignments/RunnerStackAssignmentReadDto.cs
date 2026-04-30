using SnapCd.Contracts.Interfaces;

namespace SnapCd.Contracts.Dto.RunnerStackAssignments;

public class RunnerStackAssignmentReadDto : RunnerStackAssignmentCreateDto, IDto
{
    public Guid Id { get; set; }
}