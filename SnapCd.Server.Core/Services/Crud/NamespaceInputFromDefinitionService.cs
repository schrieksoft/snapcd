using SnapCd.Contracts.Dto.NamespaceInputs;
using SnapCd.Server.Core.Entities.Interfaces;
using SnapCd.Server.Core.Events.Repository.Organization;
using SnapCd.Server.Core.Mappers;
using SnapCd.Server.Core.Repositories.Organizations.Nonsecured;
using SnapCd.Server.Core.Repositories.Organizations.Secured;
using SnapCd.Server.Core.Services.Crud.Generic;
using SnapCd.Server.Core.Services.Crud.Interfaces;
using SnapCd.Server.Core.Settings.Repositories;

namespace SnapCd.Server.Core.Services.Crud;

public class NamespaceInputFromDefinitionService<TEntity> : GenericCrudService<
    TEntity,
    NamespaceInputFromDefinitionCreateDto,
    NamespaceInputFromDefinitionUpdateDto,
    NamespaceInputFromDefinitionReadDto,
    NamespaceInputFromDefinitionSecuredRepository<TEntity>,
    NamespaceInputFromDefinitionRepository<TEntity>,
    NamespaceInputFromDefinitionCreatedEvent,
    NamespaceInputFromDefinitionUpdatedEvent,
    NamespaceInputFromDefinitionDeletedEvent,
    NamespaceInputFromDefinitionRepositorySettings>, INamespaceInputFromDefinitionService
    where TEntity : Entities.Definition.Base.NamespaceInput, INamespaceInputFromDefinition, new()
{
    public NamespaceInputFromDefinitionService(
        NamespaceInputFromDefinitionSecuredRepository<TEntity> securedRepository
    ) : base(securedRepository)
    {
    }

    protected override TEntity MapToEntity(NamespaceInputFromDefinitionCreateDto dto, Guid organizationId)
    {
        return NamespaceInputFromDefinitionMapper.ToEntity<TEntity>(dto, organizationId);
    }

    protected override NamespaceInputFromDefinitionReadDto MapToDto(TEntity entity)
    {
        return NamespaceInputFromDefinitionMapper.ToDto(entity);
    }

    protected override void UpdateEntityFromDto(TEntity entity, NamespaceInputFromDefinitionUpdateDto dto)
    {
        NamespaceInputFromDefinitionMapper.UpdateEntity(entity, dto);
    }

    public async Task<NamespaceInputFromDefinitionReadDto> Get(Guid namespaceId, string name, Guid organizationId)
    {
        return await GetByCriteria(repo => repo.Get(namespaceId, name, organizationId));
    }
}