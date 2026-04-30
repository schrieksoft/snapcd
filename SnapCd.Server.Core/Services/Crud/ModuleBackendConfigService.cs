using SnapCd.Contracts.Dto.ModuleBackendConfigs;
using SnapCd.Server.Core.Entities.Definition;
using SnapCd.Server.Core.Events.Repository.Organization;
using SnapCd.Server.Core.Mappers;
using SnapCd.Server.Core.Repositories.Organizations.Nonsecured;
using SnapCd.Server.Core.Repositories.Organizations.Secured;
using SnapCd.Server.Core.Services.Crud.Generic;
using SnapCd.Server.Core.Settings.Repositories;

namespace SnapCd.Server.Core.Services.Crud;

public class ModuleBackendConfigService : GenericCrudService<ModuleBackendConfig, ModuleBackendConfigCreateDto, ModuleBackendConfigUpdateDto, ModuleBackendConfigReadDto, ModuleBackendConfigSecuredRepository, ModuleBackendConfigRepository,
    ModuleBackendConfigCreatedEvent, ModuleBackendConfigUpdatedEvent, ModuleBackendConfigDeletedEvent, ModuleBackendConfigRepositorySettings>
{
    public ModuleBackendConfigService(
        ModuleBackendConfigSecuredRepository securedRepository
    ) : base(securedRepository)
    {
    }

    protected override ModuleBackendConfig MapToEntity(ModuleBackendConfigCreateDto dto, Guid organizationId)
    {
        return ModuleBackendConfigMapper.ToEntity(dto, organizationId);
    }

    protected override ModuleBackendConfigReadDto MapToDto(ModuleBackendConfig entity)
    {
        return ModuleBackendConfigMapper.ToDto(entity);
    }

    protected override void UpdateEntityFromDto(ModuleBackendConfig entity, ModuleBackendConfigUpdateDto dto)
    {
        ModuleBackendConfigMapper.UpdateEntity(entity, dto);
    }

    public async Task<ModuleBackendConfigReadDto> Get(Guid moduleId, string name, Guid organizationId)
    {
        var entity = await SecuredRepository.Get(moduleId, name, organizationId);
        return ModuleBackendConfigMapper.ToDto(entity);
    }
}