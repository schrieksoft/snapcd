using SnapCd.Contracts.Dto.ModuleInputs;
using SnapCd.Server.Core.Entities.Definition.Base;
using SnapCd.Server.Core.Entities.Interfaces;
using SnapCd.Server.Core.Events.Repository.Organization;
using SnapCd.Server.Core.Mappers;
using SnapCd.Server.Core.Repositories.Organizations.Nonsecured;
using SnapCd.Server.Core.Repositories.Organizations.Secured;
using SnapCd.Server.Core.Services.Crud.Generic;
using SnapCd.Server.Core.Services.Crud.Interfaces;
using SnapCd.Server.Core.Settings.Repositories;

namespace SnapCd.Server.Core.Services.Crud;

public class ModuleInputFromSecretService<TEntity> : GenericCrudService<
    TEntity,
    ModuleInputFromSecretCreateDto,
    ModuleInputFromSecretUpdateDto,
    ModuleInputFromSecretReadDto,
    ModuleInputFromSecretSecuredRepository<TEntity>,
    ModuleInputFromSecretRepository<TEntity>,
    ModuleInputFromSecretCreatedEvent,
    ModuleInputFromSecretUpdatedEvent,
    ModuleInputFromSecretDeletedEvent,
    ModuleInputFromSecretRepositorySettings>, IModuleInputFromSecretService
    where TEntity : ModuleInputWithType, IModuleInputFromSecret, new()
{
    public ModuleInputFromSecretService(
        ModuleInputFromSecretSecuredRepository<TEntity> securedRepository
    ) : base(securedRepository)
    {
    }

    protected override TEntity MapToEntity(ModuleInputFromSecretCreateDto dto, Guid organizationId)
    {
        return ModuleInputFromSecretMapper.ToEntity<TEntity>(dto, organizationId);
    }

    protected override ModuleInputFromSecretReadDto MapToDto(TEntity entity)
    {
        return ModuleInputFromSecretMapper.ToDto(entity);
    }

    protected override void UpdateEntityFromDto(TEntity entity, ModuleInputFromSecretUpdateDto dto)
    {
        ModuleInputFromSecretMapper.UpdateEntity(entity, dto);
    }

    public async Task<ModuleInputFromSecretReadDto> Get(Guid moduleId, string name, Guid organizationId)
    {
        return await GetByCriteria(repo => repo.Get(moduleId, name, organizationId));
    }
}