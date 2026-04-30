using SnapCd.Contracts.Dto.NamespacePulumiFlags;
using SnapCd.Server.Core.Entities.Definition;
using SnapCd.Server.Core.Events.Repository.Organization;
using SnapCd.Server.Core.Mappers;
using SnapCd.Server.Core.Repositories.Organizations.Nonsecured;
using SnapCd.Server.Core.Repositories.Organizations.Secured;
using SnapCd.Server.Core.Services.Crud.Generic;
using SnapCd.Server.Core.Settings.Repositories;

namespace SnapCd.Server.Core.Services.Crud;

public class NamespacePulumiFlagService : GenericCrudService<NamespacePulumiFlag, NamespacePulumiFlagCreateDto, NamespacePulumiFlagUpdateDto, NamespacePulumiFlagReadDto, NamespacePulumiFlagSecuredRepository, NamespacePulumiFlagRepository,
    NamespacePulumiFlagCreatedEvent, NamespacePulumiFlagUpdatedEvent, NamespacePulumiFlagDeletedEvent, NamespacePulumiFlagRepositorySettings>
{
    public NamespacePulumiFlagService(
        NamespacePulumiFlagSecuredRepository securedRepository
    ) : base(securedRepository)
    {
    }

    protected override NamespacePulumiFlag MapToEntity(NamespacePulumiFlagCreateDto dto, Guid organizationId)
    {
        return NamespacePulumiFlagMapper.ToEntity(dto, organizationId);
    }

    protected override NamespacePulumiFlagReadDto MapToDto(NamespacePulumiFlag entity)
    {
        return NamespacePulumiFlagMapper.ToDto(entity);
    }

    protected override void UpdateEntityFromDto(NamespacePulumiFlag entity, NamespacePulumiFlagUpdateDto dto)
    {
        NamespacePulumiFlagMapper.UpdateEntity(entity, dto);
    }
}
