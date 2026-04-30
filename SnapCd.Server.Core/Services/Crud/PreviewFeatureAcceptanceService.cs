using SnapCd.Server.Core.Dtos.PreviewFeatureAcceptances;
using SnapCd.Server.Core.Entities.Definition;
using SnapCd.Server.Core.Enums;
using SnapCd.Server.Core.Events.Repository.Organization;
using SnapCd.Server.Core.Mappers;
using SnapCd.Server.Core.Repositories.Organizations.Nonsecured;
using SnapCd.Server.Core.Repositories.Organizations.Secured;
using SnapCd.Server.Core.Services.Crud.Generic;
using SnapCd.Server.Core.Settings.Repositories;

namespace SnapCd.Server.Core.Services.Crud;

public class PreviewFeatureAcceptanceService : GenericCrudService<
    PreviewFeatureAcceptance,
    PreviewFeatureAcceptanceCreateDto,
    PreviewFeatureAcceptanceUpdateDto,
    PreviewFeatureAcceptanceReadDto,
    PreviewFeatureAcceptanceSecuredRepository,
    PreviewFeatureAcceptanceRepository,
    PreviewFeatureAcceptanceCreatedEvent,
    PreviewFeatureAcceptanceUpdatedEvent,
    PreviewFeatureAcceptanceDeletedEvent,
    PreviewFeatureAcceptanceRepositorySettings>
{
    public PreviewFeatureAcceptanceService(
        PreviewFeatureAcceptanceSecuredRepository securedRepository
    ) : base(securedRepository)
    {
    }

    protected override PreviewFeatureAcceptance MapToEntity(PreviewFeatureAcceptanceCreateDto dto, Guid organizationId)
    {
        return PreviewFeatureAcceptanceMapper.ToEntity(dto, organizationId);
    }

    protected override PreviewFeatureAcceptanceReadDto MapToDto(PreviewFeatureAcceptance entity)
    {
        return PreviewFeatureAcceptanceMapper.ToDto(entity);
    }

    protected override void UpdateEntityFromDto(PreviewFeatureAcceptance entity, PreviewFeatureAcceptanceUpdateDto dto)
    {
        PreviewFeatureAcceptanceMapper.UpdateEntity(entity, dto);
    }

    public async Task<bool> HasAccepted(PreviewFeature feature, Guid organizationId)
    {
        var entity = await SecuredRepository.GetByFeature(feature, organizationId);
        return entity != null;
    }

    public async Task<List<PreviewFeatureAcceptanceReadDto>> ListAccepted(Guid organizationId)
    {
        var entities = await SecuredRepository.ListByOrganization(organizationId);
        return entities.Select(MapToDto).ToList();
    }
}
