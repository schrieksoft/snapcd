using SnapCd.Contracts.Dto.NamespaceTerraformFlags;
using SnapCd.Server.Core.Entities.Definition;
using SnapCd.Server.Core.Events.Repository.Organization;
using SnapCd.Server.Core.Mappers;
using SnapCd.Server.Core.Repositories.Organizations.Nonsecured;
using SnapCd.Server.Core.Repositories.Organizations.Secured;
using SnapCd.Server.Core.Services.Crud.Generic;
using SnapCd.Server.Core.Settings.Repositories;

namespace SnapCd.Server.Core.Services.Crud;

public class NamespaceTerraformFlagService : GenericCrudService<NamespaceTerraformFlag, NamespaceTerraformFlagCreateDto, NamespaceTerraformFlagUpdateDto, NamespaceTerraformFlagReadDto, NamespaceTerraformFlagSecuredRepository, NamespaceTerraformFlagRepository,
    NamespaceTerraformFlagCreatedEvent, NamespaceTerraformFlagUpdatedEvent, NamespaceTerraformFlagDeletedEvent, NamespaceTerraformFlagRepositorySettings>
{
    public NamespaceTerraformFlagService(
        NamespaceTerraformFlagSecuredRepository securedRepository
    ) : base(securedRepository)
    {
    }

    protected override NamespaceTerraformFlag MapToEntity(NamespaceTerraformFlagCreateDto dto, Guid organizationId)
    {
        return NamespaceTerraformFlagMapper.ToEntity(dto, organizationId);
    }

    protected override NamespaceTerraformFlagReadDto MapToDto(NamespaceTerraformFlag entity)
    {
        return NamespaceTerraformFlagMapper.ToDto(entity);
    }

    protected override void UpdateEntityFromDto(NamespaceTerraformFlag entity, NamespaceTerraformFlagUpdateDto dto)
    {
        NamespaceTerraformFlagMapper.UpdateEntity(entity, dto);
    }
}
