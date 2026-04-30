using SnapCd.Contracts.Dto.ModulePulumiArrayFlags;
using SnapCd.Server.Core.Entities.Definition;
using SnapCd.Server.Core.Events.Repository.Organization;
using SnapCd.Server.Core.Mappers;
using SnapCd.Server.Core.Repositories.Organizations.Nonsecured;
using SnapCd.Server.Core.Repositories.Organizations.Secured;
using SnapCd.Server.Core.Services.Crud.Generic;
using SnapCd.Server.Core.Settings.Repositories;

namespace SnapCd.Server.Core.Services.Crud;

public class ModulePulumiArrayFlagService : GenericCrudService<ModulePulumiArrayFlag, ModulePulumiArrayFlagCreateDto, ModulePulumiArrayFlagUpdateDto, ModulePulumiArrayFlagReadDto, ModulePulumiArrayFlagSecuredRepository, ModulePulumiArrayFlagRepository,
    ModulePulumiArrayFlagCreatedEvent, ModulePulumiArrayFlagUpdatedEvent, ModulePulumiArrayFlagDeletedEvent, ModulePulumiArrayFlagRepositorySettings>
{
    public ModulePulumiArrayFlagService(
        ModulePulumiArrayFlagSecuredRepository securedRepository
    ) : base(securedRepository)
    {
    }

    protected override ModulePulumiArrayFlag MapToEntity(ModulePulumiArrayFlagCreateDto dto, Guid organizationId)
    {
        return ModulePulumiArrayFlagMapper.ToEntity(dto, organizationId);
    }

    protected override ModulePulumiArrayFlagReadDto MapToDto(ModulePulumiArrayFlag entity)
    {
        return ModulePulumiArrayFlagMapper.ToDto(entity);
    }

    protected override void UpdateEntityFromDto(ModulePulumiArrayFlag entity, ModulePulumiArrayFlagUpdateDto dto)
    {
        ModulePulumiArrayFlagMapper.UpdateEntity(entity, dto);
    }
}
