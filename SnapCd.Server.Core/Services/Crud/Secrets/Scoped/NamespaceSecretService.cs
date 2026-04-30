using SnapCd.Contracts.Dto.Secrets.Scoped;
using SnapCd.Server.Core.Entities.Definition.Secrets.Scoped;
using SnapCd.Server.Core.Events.Repository.Organization;
using SnapCd.Server.Core.Mappers;
using SnapCd.Server.Core.Repositories.Organizations.Nonsecured.Secrets;
using SnapCd.Server.Core.Repositories.Organizations.Secured.Secrets.Scoped;
using SnapCd.Server.Core.Services.Crud.Generic;
using SnapCd.Server.Core.Settings.Repositories;

namespace SnapCd.Server.Core.Services.Crud.Secrets.Scoped;

public class NamespaceSecretService : GenericCrudService<NamespaceSecret, NamespaceSecretDto, NamespaceSecretDto, NamespaceSecretDto, NamespaceSecretSecuredRepository, NamespaceSecretRepository, NamespaceSecretCreatedEvent,
    NamespaceSecretUpdatedEvent, NamespaceSecretDeletedEvent, NamespaceSecretRepositorySettings>
{
    public NamespaceSecretService(
        NamespaceSecretSecuredRepository securedRepository
    ) : base(securedRepository)
    {
    }

    protected override NamespaceSecret MapToEntity(NamespaceSecretDto dto, Guid organizationId)
    {
        return NamespaceSecretMapper.ToEntity(dto, organizationId);
    }

    protected override NamespaceSecretDto MapToDto(NamespaceSecret entity)
    {
        return NamespaceSecretMapper.ToDto(entity);
    }

    protected override void UpdateEntityFromDto(NamespaceSecret entity, NamespaceSecretDto dto)
    {
        NamespaceSecretMapper.UpdateEntity(entity, dto);
    }

    public async Task<NamespaceSecretDto> GetByName(string name, Guid organizationId)
    {
        return await GetByCriteria(repo => repo.GetByName(name, organizationId, null));
    }
}