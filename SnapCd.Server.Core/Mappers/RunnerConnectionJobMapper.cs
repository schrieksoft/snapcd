using SnapCd.Server.Core.Dtos;
using SnapCd.Server.Core.Entities.Definition;

namespace SnapCd.Server.Core.Mappers;

/// <summary>
/// Mapper for RunnerConnectionJob entity to DTO conversions.
/// </summary>
public static class RunnerConnectionJobMapper
{
    public static RunnerConnectionJobReadDto ToDto(RunnerConnectionJob entity)
    {
        return new RunnerConnectionJobReadDto
        {
            Id = entity.Id,
            OrganizationId = entity.OrganizationId,
            RunnerConnectionId = entity.RunnerConnectionId,
            ModuleJobId = entity.ModuleJobId,
            TaskName = entity.TaskName,
            CreatedDateTime = entity.CreatedDateTime,
            ModifiedDateTime = entity.ModifiedDateTime
        };
    }
}