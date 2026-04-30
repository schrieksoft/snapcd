using SnapCd.Contracts.Dto.NamespaceInputs;
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

public class NamespaceInputFromSecretService<TEntity> : GenericCrudService<
    TEntity,
     NamespaceInputFromSecretCreateDto,
    NamespaceInputFromSecretUpdateDto,
    NamespaceInputFromSecretReadDto,
    NamespaceInputFromSecretSecuredRepository<TEntity>,
    NamespaceInputFromSecretRepository<TEntity>,
    NamespaceInputFromSecretCreatedEvent,
    NamespaceInputFromSecretUpdatedEvent,
    NamespaceInputFromSecretDeletedEvent,
    NamespaceInputFromSecretRepositorySettings>, INamespaceInputFromSecretService
    where TEntity : NamespaceInputWithType, INamespaceInputFromSecret, new()
{
    public NamespaceInputFromSecretService(
        NamespaceInputFromSecretSecuredRepository<TEntity> securedRepository
    ) : base(securedRepository)
    {
    }

    protected override TEntity MapToEntity(NamespaceInputFromSecretCreateDto dto, Guid organizationId)
    {
        return NamespaceInputFromSecretMapper.ToEntity<TEntity>(dto, organizationId);
    }

    protected override NamespaceInputFromSecretReadDto MapToDto(TEntity entity)
    {
        return NamespaceInputFromSecretMapper.ToDto(entity);
    }

    protected override void UpdateEntityFromDto(TEntity entity, NamespaceInputFromSecretUpdateDto dto)
    {
        NamespaceInputFromSecretMapper.UpdateEntity(entity, dto);
    }

    public async Task<NamespaceInputFromSecretReadDto> Get(Guid namespaceId, string name, Guid organizationId)
    {
        return await GetByCriteria(repo => repo.Get(namespaceId, name, organizationId));
    }
}