using SnapCd.Contracts.Dto.NamespaceTerraformArrayFlags;
using SnapCd.Server.Core.Entities.Definition;
using SnapCd.Server.Core.Events.Repository.Organization;
using SnapCd.Server.Core.Mappers;
using SnapCd.Server.Core.Repositories.Organizations.Nonsecured;
using SnapCd.Server.Core.Repositories.Organizations.Secured;
using SnapCd.Server.Core.Services.Crud.Generic;
using SnapCd.Server.Core.Settings.Repositories;

namespace SnapCd.Server.Core.Services.Crud;

public class NamespaceTerraformArrayFlagService : GenericCrudService<NamespaceTerraformArrayFlag, NamespaceTerraformArrayFlagCreateDto, NamespaceTerraformArrayFlagUpdateDto, NamespaceTerraformArrayFlagReadDto, NamespaceTerraformArrayFlagSecuredRepository, NamespaceTerraformArrayFlagRepository,
    NamespaceTerraformArrayFlagCreatedEvent, NamespaceTerraformArrayFlagUpdatedEvent, NamespaceTerraformArrayFlagDeletedEvent, NamespaceTerraformArrayFlagRepositorySettings>
{
    public NamespaceTerraformArrayFlagService(
        NamespaceTerraformArrayFlagSecuredRepository securedRepository
    ) : base(securedRepository)
    {
    }

    protected override NamespaceTerraformArrayFlag MapToEntity(NamespaceTerraformArrayFlagCreateDto dto, Guid organizationId)
    {
        return NamespaceTerraformArrayFlagMapper.ToEntity(dto, organizationId);
    }

    protected override NamespaceTerraformArrayFlagReadDto MapToDto(NamespaceTerraformArrayFlag entity)
    {
        return NamespaceTerraformArrayFlagMapper.ToDto(entity);
    }

    protected override void UpdateEntityFromDto(NamespaceTerraformArrayFlag entity, NamespaceTerraformArrayFlagUpdateDto dto)
    {
        NamespaceTerraformArrayFlagMapper.UpdateEntity(entity, dto);
    }
}
