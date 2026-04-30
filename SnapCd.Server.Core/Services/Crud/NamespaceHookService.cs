using SnapCd.Contracts.Dto.NamespaceHooks;
using SnapCd.Server.Core.Entities.Definition;
using SnapCd.Server.Core.Events.Repository.Organization;
using SnapCd.Server.Core.Mappers;
using SnapCd.Server.Core.Repositories.Organizations.Nonsecured;
using SnapCd.Server.Core.Repositories.Organizations.Secured;
using SnapCd.Server.Core.Services.Crud.Generic;
using SnapCd.Server.Core.Settings.Repositories;

namespace SnapCd.Server.Core.Services.Crud;

public class NamespaceHookService : GenericCrudService<NamespaceHook, NamespaceHookCreateDto, NamespaceHookUpdateDto, NamespaceHookReadDto, NamespaceHookSecuredRepository, NamespaceHookRepository,
    NamespaceHookCreatedEvent, NamespaceHookUpdatedEvent, NamespaceHookDeletedEvent, NamespaceHookRepositorySettings>
{
    public NamespaceHookService(
        NamespaceHookSecuredRepository securedRepository
    ) : base(securedRepository)
    {
    }

    protected override NamespaceHook MapToEntity(NamespaceHookCreateDto dto, Guid organizationId)
    {
        return NamespaceHookMapper.ToEntity(dto, organizationId);
    }

    protected override NamespaceHookReadDto MapToDto(NamespaceHook entity)
    {
        return NamespaceHookMapper.ToDto(entity);
    }

    protected override void UpdateEntityFromDto(NamespaceHook entity, NamespaceHookUpdateDto dto)
    {
        NamespaceHookMapper.UpdateEntity(entity, dto);
    }
}
