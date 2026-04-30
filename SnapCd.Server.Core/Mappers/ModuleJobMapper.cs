using SnapCd.Server.Core.Dtos.ModuleJobs;
using SnapCd.Server.Core.Entities.Definition;

namespace SnapCd.Server.Core.Mappers;

public static class ModuleJobMapper
{
    public static ModuleJobReadDto ToDto(ModuleJob entity)
    {
        return new ModuleJobReadDto
        {
            Id = entity.Id,
            ModuleId = entity.ModuleId,
            TimestampStart = entity.TimestampStart,
            TimestampEnd = entity.TimestampEnd,
            Status = entity.Status,
            JobType = entity.JobType,
            WaitingForApproval = entity.WaitingForApproval,
            IsCurrent = entity.IsCurrent,
            DefinitiveRevision = entity.DefinitiveRevision,
            ActualStateHeadline = entity.ActualStateHeadline
        };
    }
}