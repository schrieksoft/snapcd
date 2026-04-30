using SnapCd.Contracts.Dto.ModuleHooks;
using SnapCd.Server.Core.Entities.Definition;
using SnapCd.Server.Core.Events.Repository.Organization;
using SnapCd.Server.Core.Mappers;
using SnapCd.Server.Core.Repositories.Organizations.Nonsecured;
using SnapCd.Server.Core.Repositories.Organizations.Secured;
using SnapCd.Server.Core.Services.Crud.Generic;
using SnapCd.Server.Core.Settings.Repositories;

namespace SnapCd.Server.Core.Services.Crud;

public class ModuleHookService : GenericCrudService<ModuleHook, ModuleHookCreateDto, ModuleHookUpdateDto, ModuleHookReadDto, ModuleHookSecuredRepository, ModuleHookRepository,
    ModuleHookCreatedEvent, ModuleHookUpdatedEvent, ModuleHookDeletedEvent, ModuleHookRepositorySettings>
{
    public ModuleHookService(
        ModuleHookSecuredRepository securedRepository
    ) : base(securedRepository)
    {
    }

    protected override ModuleHook MapToEntity(ModuleHookCreateDto dto, Guid organizationId)
    {
        return ModuleHookMapper.ToEntity(dto, organizationId);
    }

    protected override ModuleHookReadDto MapToDto(ModuleHook entity)
    {
        return ModuleHookMapper.ToDto(entity);
    }

    protected override void UpdateEntityFromDto(ModuleHook entity, ModuleHookUpdateDto dto)
    {
        ModuleHookMapper.UpdateEntity(entity, dto);
    }
}
