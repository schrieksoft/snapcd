using SnapCd.Contracts.Dto.SourceRefresherPreselections;
using SnapCd.Server.Core.Entities.Definition;

namespace SnapCd.Server.Core.Mappers;

public static class SourceRefresherPreselectionMapper
{
    public static SourceRefresherPreselection ToEntity(SourceRefresherPreselectionCreateDto dto, Guid organizationId)
    {
        return new SourceRefresherPreselection
        {
            Id = Guid.NewGuid(),
            OrganizationId = organizationId,
            RunnerId = dto.RunnerId,
            RunnerInstanceName = dto.RunnerInstanceName,
            SourceUrl = dto.SourceUrl
        };
    }

    public static SourceRefresherPreselectionReadDto ToDto(SourceRefresherPreselection entity)
    {
        return new SourceRefresherPreselectionReadDto
        {
            Id = entity.Id,
            RunnerId = entity.RunnerId,
            RunnerInstanceName = entity.RunnerInstanceName,
            SourceUrl = entity.SourceUrl
        };
    }

    public static void UpdateEntity(SourceRefresherPreselection entity, SourceRefresherPreselectionUpdateDto dto)
    {
        entity.RunnerId = dto.RunnerId;
        entity.RunnerInstanceName = dto.RunnerInstanceName;
        entity.SourceUrl = dto.SourceUrl;
    }
}