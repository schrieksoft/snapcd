using SnapCd.Contracts.Interfaces;

namespace SnapCd.Contracts.Dto.RunnerServicePrincipalAssignments;

public class RunnerServicePrincipalAssignmentUpdateDto : RunnerServicePrincipalAssignmentCreateDto, IUpdateDto
{
    public Guid Id { get; set; }
}
