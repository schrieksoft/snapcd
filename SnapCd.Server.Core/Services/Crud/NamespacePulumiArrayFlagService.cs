using SnapCd.Contracts.Dto.NamespacePulumiArrayFlags;
using SnapCd.Server.Core.Entities.Definition;
using SnapCd.Server.Core.Events.Repository.Organization;
using SnapCd.Server.Core.Mappers;
using SnapCd.Server.Core.Repositories.Organizations.Nonsecured;
using SnapCd.Server.Core.Repositories.Organizations.Secured;
using SnapCd.Server.Core.Services.Crud.Generic;
using SnapCd.Server.Core.Settings.Repositories;

namespace SnapCd.Server.Core.Services.Crud;

public class NamespacePulumiArrayFlagService : GenericCrudService<NamespacePulumiArrayFlag, NamespacePulumiArrayFlagCreateDto, NamespacePulumiArrayFlagUpdateDto, NamespacePulumiArrayFlagReadDto, NamespacePulumiArrayFlagSecuredRepository, NamespacePulumiArrayFlagRepository,
    NamespacePulumiArrayFlagCreatedEvent, NamespacePulumiArrayFlagUpdatedEvent, NamespacePulumiArrayFlagDeletedEvent, NamespacePulumiArrayFlagRepositorySettings>
{
    public NamespacePulumiArrayFlagService(
        NamespacePulumiArrayFlagSecuredRepository securedRepository
    ) : base(securedRepository)
    {
    }

    protected override NamespacePulumiArrayFlag MapToEntity(NamespacePulumiArrayFlagCreateDto dto, Guid organizationId)
    {
        return NamespacePulumiArrayFlagMapper.ToEntity(dto, organizationId);
    }

    protected override NamespacePulumiArrayFlagReadDto MapToDto(NamespacePulumiArrayFlag entity)
    {
        return NamespacePulumiArrayFlagMapper.ToDto(entity);
    }

    protected override void UpdateEntityFromDto(NamespacePulumiArrayFlag entity, NamespacePulumiArrayFlagUpdateDto dto)
    {
        NamespacePulumiArrayFlagMapper.UpdateEntity(entity, dto);
    }
}
