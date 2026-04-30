using SnapCd.Contracts.Dto.NamespaceBackendConfigs;
using SnapCd.Server.Core.Entities.Definition;
using SnapCd.Server.Core.Events.Repository.Organization;
using SnapCd.Server.Core.Mappers;
using SnapCd.Server.Core.Repositories.Organizations.Nonsecured;
using SnapCd.Server.Core.Repositories.Organizations.Secured;
using SnapCd.Server.Core.Services.Crud.Generic;
using SnapCd.Server.Core.Settings.Repositories;

namespace SnapCd.Server.Core.Services.Crud;

public class NamespaceBackendConfigService : GenericCrudService<
    NamespaceBackendConfig,
    NamespaceBackendConfigCreateDto,
    NamespaceBackendConfigUpdateDto,
    NamespaceBackendConfigReadDto,
    NamespaceBackendConfigSecuredRepository,
    NamespaceBackendConfigRepository,
    NamespaceBackendConfigCreatedEvent,
    NamespaceBackendConfigUpdatedEvent,
    NamespaceBackendConfigDeletedEvent,
    NamespaceBackendConfigRepositorySettings>
{
    public NamespaceBackendConfigService(
        NamespaceBackendConfigSecuredRepository securedRepository
    ) : base(securedRepository)
    {
    }

    protected override NamespaceBackendConfig MapToEntity(NamespaceBackendConfigCreateDto dto, Guid organizationId)
    {
        return NamespaceBackendConfigMapper.ToEntity(dto, organizationId);
    }

    protected override NamespaceBackendConfigReadDto MapToDto(NamespaceBackendConfig entity)
    {
        return NamespaceBackendConfigMapper.ToDto(entity);
    }

    protected override void UpdateEntityFromDto(NamespaceBackendConfig entity, NamespaceBackendConfigUpdateDto dto)
    {
        NamespaceBackendConfigMapper.UpdateEntity(entity, dto);
    }

    public async Task<NamespaceBackendConfigReadDto> Get(Guid namespaceId, string name, Guid organizationId)
    {
        var entity = await SecuredRepository.Get(namespaceId, name, organizationId);
        return NamespaceBackendConfigMapper.ToDto(entity);
    }
}