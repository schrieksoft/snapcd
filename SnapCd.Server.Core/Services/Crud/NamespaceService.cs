using SnapCd.Contracts.Dto.Namespaces;
using SnapCd.Server.Core.Events.Repository.Organization;
using SnapCd.Server.Core.Mappers;
using SnapCd.Server.Core.Repositories.Organizations.Nonsecured;
using SnapCd.Server.Core.Repositories.Organizations.Secured;
using SnapCd.Server.Core.Services.Crud.Generic;
using SnapCd.Server.Core.Settings.Repositories;

namespace SnapCd.Server.Core.Services.Crud;

public class
    NamespaceService : GenericCrudService<
    Entities.Definition.Namespace, 
    NamespaceCreateDto, NamespaceUpdateDto, 
    NamespaceReadDto, 
    NamespaceSecuredRepository, 
    NamespaceRepository, 
    NamespaceCreatedEvent, 
    NamespaceUpdatedEvent,
    NamespaceDeletedEvent, 
    NamespaceRepositorySettings>
{
    public NamespaceService(
        NamespaceSecuredRepository securedRepository
    ) : base(securedRepository)
    {
    }

    protected override Entities.Definition.Namespace MapToEntity(NamespaceCreateDto dto, Guid organizationId)
    {
        return NamespaceMapper.ToEntity(dto, organizationId);
    }

    protected override NamespaceReadDto MapToDto(Entities.Definition.Namespace entity)
    {
        return NamespaceMapper.ToDto(entity);
    }

    protected override void UpdateEntityFromDto(Entities.Definition.Namespace entity, NamespaceUpdateDto dto)
    {
        NamespaceMapper.UpdateEntity(entity, dto);
    }

    public async Task<NamespaceReadDto> Get(Guid stackId, string name, Guid organizationId)
    {
        return await GetByCriteria(repo => repo.Get(stackId, name, organizationId));
    }
}