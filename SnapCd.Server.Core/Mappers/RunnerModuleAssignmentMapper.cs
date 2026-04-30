using SnapCd.Contracts.Dto.RunnerModuleAssignments;
using SnapCd.Server.Core.Entities.Definition.RunnerAssignments;

namespace SnapCd.Server.Core.Mappers;

public static class RunnerModuleAssignmentMapper
{
    public static RunnerModuleAssignment ToEntity(RunnerModuleAssignmentCreateDto dto, Guid organizationId)
    {
        return new RunnerModuleAssignment
        {
            Id = Guid.NewGuid(),
            OrganizationId = organizationId,
            ModuleId = dto.ModuleId,
            RunnerId = dto.RunnerId
        };
    }

    public static RunnerModuleAssignmentReadDto ToDto(RunnerModuleAssignment entity)
    {
        return new RunnerModuleAssignmentReadDto
        {
            Id = entity.Id,
            ModuleId = entity.ModuleId,
            RunnerId = entity.RunnerId
        };
    }

    public static void UpdateEntity(RunnerModuleAssignment entity, RunnerModuleAssignmentUpdateDto dto)
    {
        entity.ModuleId = dto.ModuleId;
        entity.RunnerId = dto.RunnerId;
    }
}