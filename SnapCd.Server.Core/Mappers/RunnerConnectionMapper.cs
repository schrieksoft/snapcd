using SnapCd.Server.Core.Dtos;
using SnapCd.Server.Core.Entities.Definition;

namespace SnapCd.Server.Core.Mappers;

/// <summary>
/// Mapper for RunnerConnection entity to DTO conversions.
/// </summary>
public static class RunnerConnectionMapper
{
    public static RunnerConnectionReadDto ToDto(RunnerConnection entity)
    {
        return new RunnerConnectionReadDto
        {
            Id = entity.Id,
            OrganizationId = entity.OrganizationId,
            RunnerId = entity.RunnerId,
            InstanceName = entity.InstanceName,
            ConnectionId = entity.SignalRConnectionId,
            ServerInstanceId = entity.ServerInstanceId,
            CreatedDateTime = entity.CreatedDateTime,
            ModifiedDateTime = entity.ModifiedDateTime
        };
    }
}
