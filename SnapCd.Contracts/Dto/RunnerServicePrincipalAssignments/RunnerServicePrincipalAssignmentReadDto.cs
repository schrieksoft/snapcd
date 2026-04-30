using SnapCd.Contracts.Interfaces;

namespace SnapCd.Contracts.Dto.RunnerServicePrincipalAssignments;

public class RunnerServicePrincipalAssignmentReadDto : RunnerServicePrincipalAssignmentCreateDto, IDto
{
    public Guid Id { get; set; }
}