using SnapCd.Contracts.Dto.RunnerStackAssignments;
using SnapCd.Server.Core.Entities.Definition.RunnerAssignments;

namespace SnapCd.Server.Core.Mappers;

public static class RunnerStackAssignmentMapper
{
    public static RunnerStackAssignment ToEntity(RunnerStackAssignmentCreateDto dto, Guid organizationId)
    {
        return new RunnerStackAssignment
        {
            Id = Guid.NewGuid(),
            OrganizationId = organizationId,
            StackId = dto.StackId,
            RunnerId = dto.RunnerId
        };
    }

    public static RunnerStackAssignmentReadDto ToDto(RunnerStackAssignment entity)
    {
        return new RunnerStackAssignmentReadDto
        {
            Id = entity.Id,
            StackId = entity.StackId,
            RunnerId = entity.RunnerId
        };
    }

    public static void UpdateEntity(RunnerStackAssignment entity, RunnerStackAssignmentUpdateDto dto)
    {
        entity.StackId = dto.StackId;
        entity.RunnerId = dto.RunnerId;
    }
}