using SnapCd.Contracts.Dto.ModulePulumiFlags;
using SnapCd.Server.Core.Entities.Definition;
using SnapCd.Server.Core.Events.Repository.Organization;
using SnapCd.Server.Core.Mappers;
using SnapCd.Server.Core.Repositories.Organizations.Nonsecured;
using SnapCd.Server.Core.Repositories.Organizations.Secured;
using SnapCd.Server.Core.Services.Crud.Generic;
using SnapCd.Server.Core.Settings.Repositories;

namespace SnapCd.Server.Core.Services.Crud;

public class ModulePulumiFlagService : GenericCrudService<ModulePulumiFlag, ModulePulumiFlagCreateDto, ModulePulumiFlagUpdateDto, ModulePulumiFlagReadDto, ModulePulumiFlagSecuredRepository, ModulePulumiFlagRepository,
    ModulePulumiFlagCreatedEvent, ModulePulumiFlagUpdatedEvent, ModulePulumiFlagDeletedEvent, ModulePulumiFlagRepositorySettings>
{
    public ModulePulumiFlagService(
        ModulePulumiFlagSecuredRepository securedRepository
    ) : base(securedRepository)
    {
    }

    protected override ModulePulumiFlag MapToEntity(ModulePulumiFlagCreateDto dto, Guid organizationId)
    {
        return ModulePulumiFlagMapper.ToEntity(dto, organizationId);
    }

    protected override ModulePulumiFlagReadDto MapToDto(ModulePulumiFlag entity)
    {
        return ModulePulumiFlagMapper.ToDto(entity);
    }

    protected override void UpdateEntityFromDto(ModulePulumiFlag entity, ModulePulumiFlagUpdateDto dto)
    {
        ModulePulumiFlagMapper.UpdateEntity(entity, dto);
    }
}
