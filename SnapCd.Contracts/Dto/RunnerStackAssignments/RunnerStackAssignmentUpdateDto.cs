using SnapCd.Contracts.Interfaces;

namespace SnapCd.Contracts.Dto.RunnerStackAssignments;

public class RunnerStackAssignmentUpdateDto : RunnerStackAssignmentCreateDto, IUpdateDto
{
    public Guid Id { get; set; }
}
