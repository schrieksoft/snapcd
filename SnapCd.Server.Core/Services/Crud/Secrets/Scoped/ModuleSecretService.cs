using SnapCd.Contracts.Dto.Secrets.Scoped;
using SnapCd.Server.Core.Entities.Definition.Secrets.Scoped;
using SnapCd.Server.Core.Events.Repository.Organization;
using SnapCd.Server.Core.Mappers;
using SnapCd.Server.Core.Repositories.Organizations.Nonsecured.Secrets;
using SnapCd.Server.Core.Repositories.Organizations.Secured.Secrets.Scoped;
using SnapCd.Server.Core.Services.Crud.Generic;
using SnapCd.Server.Core.Settings.Repositories;

namespace SnapCd.Server.Core.Services.Crud.Secrets.Scoped;

public class ModuleSecretService : GenericCrudService<ModuleSecret, ModuleSecretDto, ModuleSecretDto, ModuleSecretDto, ModuleSecretSecuredRepository, ModuleSecretRepository, ModuleSecretCreatedEvent, ModuleSecretUpdatedEvent,
    ModuleSecretDeletedEvent, ModuleSecretRepositorySettings>
{
    public ModuleSecretService(
        ModuleSecretSecuredRepository securedRepository
    ) : base(securedRepository)
    {
    }

    protected override ModuleSecret MapToEntity(ModuleSecretDto dto, Guid organizationId)
    {
        return ModuleSecretMapper.ToEntity(dto, organizationId);
    }

    protected override ModuleSecretDto MapToDto(ModuleSecret entity)
    {
        return ModuleSecretMapper.ToDto(entity);
    }

    protected override void UpdateEntityFromDto(ModuleSecret entity, ModuleSecretDto dto)
    {
        ModuleSecretMapper.UpdateEntity(entity, dto);
    }

    public async Task<ModuleSecretDto> GetByName(string name, Guid organizationId)
    {
        return await GetByCriteria(repo => repo.GetByName(name, organizationId, null));
    }
}