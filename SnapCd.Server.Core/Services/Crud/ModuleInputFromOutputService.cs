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

public class ModuleInputFromOutputService<TEntity> : GenericCrudService<
    TEntity,
    ModuleInputFromOutputCreateDto,
    ModuleInputFromOutputUpdateDto,
    ModuleInputFromOutputDtoRead,
    ModuleInputFromOutputSecuredRepository<TEntity>,
    ModuleInputFromOutputRepository<TEntity>,
    ModuleInputFromOutputCreatedEvent,
    ModuleInputFromOutputUpdatedEvent,
    ModuleInputFromOutputDeletedEvent,
    ModuleInputFromOutputRepositorySettings>, IModuleInputFromOutputService
    where TEntity : Entities.Definition.Base.ModuleInput, IModuleInputFromOutput, new()
{
    public ModuleInputFromOutputService(
        ModuleInputFromOutputSecuredRepository<TEntity> securedRepository
    ) : base(securedRepository)
    {
    }

    protected override TEntity MapToEntity(ModuleInputFromOutputCreateDto dto, Guid organizationId)
    {
        return ModuleInputFromOutputMapper.ToEntity<TEntity>(dto, organizationId);
    }

    protected override ModuleInputFromOutputDtoRead MapToDto(TEntity entity)
    {
        return ModuleInputFromOutputMapper.ToDto(entity);
    }

    protected override void UpdateEntityFromDto(TEntity entity, ModuleInputFromOutputUpdateDto dto)
    {
        ModuleInputFromOutputMapper.UpdateEntity(entity, dto);
    }

    public async Task<ModuleInputFromOutputDtoRead> Get(Guid moduleId, string name, Guid organizationId)
    {
        return await GetByCriteria(repo => repo.Get(moduleId, name, organizationId));
    }
}