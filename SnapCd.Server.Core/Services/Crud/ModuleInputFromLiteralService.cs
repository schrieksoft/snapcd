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

public class ModuleInputFromLiteralService<TEntity> : GenericCrudService<
    TEntity,
    ModuleInputFromLiteralCreateDto,
    ModuleInputFromLiteralUpdateDto,
    ModuleInputFromLiteralReadDto,
    ModuleInputFromLiteralSecuredRepository<TEntity>,
    ModuleInputFromLiteralRepository<TEntity>,
    ModuleInputFromLiteralCreatedEvent,
    ModuleInputFromLiteralUpdatedEvent,
    ModuleInputFromLiteralDeletedEvent,
    ModuleInputFromLiteralRepositorySettings>, IModuleInputFromLiteralService
    where TEntity : ModuleInputWithType, IModuleInputFromLiteral, new()
{
    public ModuleInputFromLiteralService(
        ModuleInputFromLiteralSecuredRepository<TEntity> securedRepository
    ) : base(securedRepository)
    {
    }

    protected override TEntity MapToEntity(ModuleInputFromLiteralCreateDto dto, Guid organizationId)
    {
        return ModuleInputFromLiteralMapper.ToEntity<TEntity>(dto, organizationId);
    }

    protected override ModuleInputFromLiteralReadDto MapToDto(TEntity entity)
    {
        return ModuleInputFromLiteralMapper.ToDto(entity);
    }

    protected override void UpdateEntityFromDto(TEntity entity, ModuleInputFromLiteralUpdateDto dto)
    {
        ModuleInputFromLiteralMapper.UpdateEntity(entity, dto);
    }

    public async Task<ModuleInputFromLiteralReadDto> Get(Guid moduleId, string name, Guid organizationId)
    {
        return await GetByCriteria(repo => repo.Get(moduleId, name, organizationId));
    }
}