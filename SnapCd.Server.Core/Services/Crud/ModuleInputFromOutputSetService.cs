using SnapCd.Contracts.Dto.ModuleInputs;
using SnapCd.Server.Core.Entities.Interfaces;
using SnapCd.Server.Core.Events.Repository.Organization;
using SnapCd.Server.Core.Mappers;
using SnapCd.Server.Core.Repositories.Organizations.Nonsecured;
using SnapCd.Server.Core.Repositories.Organizations.Secured;
using SnapCd.Server.Core.Services.Crud.Generic;
using SnapCd.Server.Core.Services.Crud.Interfaces;
using SnapCd.Server.Core.Settings.Repositories;

namespace SnapCd.Server.Core.Services.Crud;

public class ModuleInputFromOutputSetService<TEntity> : GenericCrudService<
    TEntity,
    ModuleInputFromOutputSetCreateDto,
    ModuleInputFromOutputSetUpdateDto,
    ModuleInputFromOutputSetReadDto,
    ModuleInputFromOutputSetSecuredRepository<TEntity>,
    ModuleInputFromOutputSetRepository<TEntity>,
    ModuleInputFromOutputSetCreatedEvent,
    ModuleInputFromOutputSetUpdatedEvent,
    ModuleInputFromOutputSetDeletedEvent,
    ModuleInputFromOutputSetRepositorySettings>, IModuleInputFromOutputSetService
    where TEntity : Entities.Definition.Base.ModuleInput, IModuleInputFromOutputSet, new()
{
    public ModuleInputFromOutputSetService(
        ModuleInputFromOutputSetSecuredRepository<TEntity> securedRepository
    ) : base(securedRepository)
    {
    }

    protected override TEntity MapToEntity(ModuleInputFromOutputSetCreateDto dto, Guid organizationId)
    {
        return ModuleInputFromOutputSetMapper.ToEntity<TEntity>(dto, organizationId);
    }

    protected override ModuleInputFromOutputSetReadDto MapToDto(TEntity entity)
    {
        return ModuleInputFromOutputSetMapper.ToDto(entity);
    }

    protected override void UpdateEntityFromDto(TEntity entity, ModuleInputFromOutputSetUpdateDto dto)
    {
        ModuleInputFromOutputSetMapper.UpdateEntity(entity, dto);
    }

    public async Task<ModuleInputFromOutputSetReadDto> Get(Guid moduleId, string name, Guid organizationId)
    {
        return await GetByCriteria(repo => repo.Get(moduleId, name, organizationId));
    }
}