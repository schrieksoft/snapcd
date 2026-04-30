using SnapCd.Contracts.Dto.NamespaceExtraFiles;
using SnapCd.Server.Core.Entities.Definition;
using SnapCd.Server.Core.Events.Repository.Organization;
using SnapCd.Server.Core.Mappers;
using SnapCd.Server.Core.Repositories.Organizations.Nonsecured;
using SnapCd.Server.Core.Repositories.Organizations.Secured;
using SnapCd.Server.Core.Services.Crud.Generic;
using SnapCd.Server.Core.Settings.Repositories;

namespace SnapCd.Server.Core.Services.Crud;

public class NamespaceExtraFileService : GenericCrudService<NamespaceExtraFile, NamespaceExtraFileCreateDto, NamespaceExtraFileUpdateDto, NamespaceExtraFileReadDto, NamespaceExtraFileSecuredRepository, NamespaceExtraFileRepository, NamespaceExtraFileCreatedEvent
    , NamespaceExtraFileUpdatedEvent, NamespaceExtraFileDeletedEvent, NamespaceExtraFileRepositorySettings>
{
    public NamespaceExtraFileService(
        NamespaceExtraFileSecuredRepository securedRepository
    ) : base(securedRepository)
    {
    }

    protected override NamespaceExtraFile MapToEntity(NamespaceExtraFileCreateDto dto, Guid organizationId)
    {
        return NamespaceExtraFileMapper.ToEntity(dto, organizationId);
    }

    protected override NamespaceExtraFileReadDto MapToDto(NamespaceExtraFile entity)
    {
        return NamespaceExtraFileMapper.ToDto(entity);
    }

    protected override void UpdateEntityFromDto(NamespaceExtraFile entity, NamespaceExtraFileUpdateDto dto)
    {
        NamespaceExtraFileMapper.UpdateEntity(entity, dto);
    }

    public async Task<NamespaceExtraFileReadDto> Get(Guid namespaceId, string fileName, Guid organizationId)
    {
        var entity = await SecuredRepository.Get(namespaceId, fileName, organizationId);
        return NamespaceExtraFileMapper.ToDto(entity);
    }
}