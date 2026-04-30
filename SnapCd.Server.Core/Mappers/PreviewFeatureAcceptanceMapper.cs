using SnapCd.Server.Core.Dtos.PreviewFeatureAcceptances;
using SnapCd.Server.Core.Entities.Definition;

namespace SnapCd.Server.Core.Mappers;

public static class PreviewFeatureAcceptanceMapper
{
    public static PreviewFeatureAcceptance ToEntity(PreviewFeatureAcceptanceCreateDto dto, Guid organizationId)
    {
        return new PreviewFeatureAcceptance
        {
            Id = Guid.NewGuid(),
            OrganizationId = organizationId,
            PreviewFeature = dto.PreviewFeature
        };
    }

    public static PreviewFeatureAcceptanceReadDto ToDto(PreviewFeatureAcceptance entity)
    {
        return new PreviewFeatureAcceptanceReadDto
        {
            Id = entity.Id,
            PreviewFeature = entity.PreviewFeature
        };
    }

    public static void UpdateEntity(PreviewFeatureAcceptance entity, PreviewFeatureAcceptanceUpdateDto dto)
    {
        entity.PreviewFeature = dto.PreviewFeature;
    }
}
