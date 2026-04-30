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

public class ModuleInputFromNamespaceService<TEntity> : GenericCrudService<
    TEntity,
    ModuleInputFromNamespaceCreateDto,
    ModuleInputFromNamespaceUpdateDto,
    ModuleInputFromNamespaceReadDto,
    ModuleInputFromNamespaceSecuredRepository<TEntity>,
    ModuleInputFromNamespaceRepository<TEntity>,
    ModuleInputFromNamespaceCreatedEvent,
    ModuleInputFromNamespaceUpdatedEvent,
    ModuleInputFromNamespaceDeletedEvent,
    ModuleInputFromNamespaceRepositorySettings>, IModuleInputFromNamespaceService
    where TEntity : Entities.Definition.Base.ModuleInput, IModuleInputFromNamespace, new()
{
    public ModuleInputFromNamespaceService(
        ModuleInputFromNamespaceSecuredRepository<TEntity> securedRepository
    ) : base(securedRepository)
    {
    }

    protected override TEntity MapToEntity(ModuleInputFromNamespaceCreateDto dto, Guid organizationId)
    {
        return ModuleInputFromNamespaceMapper.ToEntity<TEntity>(dto, organizationId);
    }

    protected override ModuleInputFromNamespaceReadDto MapToDto(TEntity entity)
    {
        return ModuleInputFromNamespaceMapper.ToDto(entity);
    }

    protected override void UpdateEntityFromDto(TEntity entity, ModuleInputFromNamespaceUpdateDto dto)
    {
        ModuleInputFromNamespaceMapper.UpdateEntity(entity, dto);
    }

    public async Task<ModuleInputFromNamespaceReadDto> Get(Guid moduleId, string name, Guid organizationId)
    {
        return await GetByCriteria(repo => repo.Get(moduleId, name, organizationId));
    }
}