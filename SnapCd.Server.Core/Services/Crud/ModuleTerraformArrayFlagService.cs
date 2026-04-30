using SnapCd.Contracts.Dto.ModuleTerraformArrayFlags;
using SnapCd.Server.Core.Entities.Definition;
using SnapCd.Server.Core.Events.Repository.Organization;
using SnapCd.Server.Core.Mappers;
using SnapCd.Server.Core.Repositories.Organizations.Nonsecured;
using SnapCd.Server.Core.Repositories.Organizations.Secured;
using SnapCd.Server.Core.Services.Crud.Generic;
using SnapCd.Server.Core.Settings.Repositories;

namespace SnapCd.Server.Core.Services.Crud;

public class ModuleTerraformArrayFlagService : GenericCrudService<ModuleTerraformArrayFlag, ModuleTerraformArrayFlagCreateDto, ModuleTerraformArrayFlagUpdateDto, ModuleTerraformArrayFlagReadDto, ModuleTerraformArrayFlagSecuredRepository, ModuleTerraformArrayFlagRepository,
    ModuleTerraformArrayFlagCreatedEvent, ModuleTerraformArrayFlagUpdatedEvent, ModuleTerraformArrayFlagDeletedEvent, ModuleTerraformArrayFlagRepositorySettings>
{
    public ModuleTerraformArrayFlagService(
        ModuleTerraformArrayFlagSecuredRepository securedRepository
    ) : base(securedRepository)
    {
    }

    protected override ModuleTerraformArrayFlag MapToEntity(ModuleTerraformArrayFlagCreateDto dto, Guid organizationId)
    {
        return ModuleTerraformArrayFlagMapper.ToEntity(dto, organizationId);
    }

    protected override ModuleTerraformArrayFlagReadDto MapToDto(ModuleTerraformArrayFlag entity)
    {
        return ModuleTerraformArrayFlagMapper.ToDto(entity);
    }

    protected override void UpdateEntityFromDto(ModuleTerraformArrayFlag entity, ModuleTerraformArrayFlagUpdateDto dto)
    {
        ModuleTerraformArrayFlagMapper.UpdateEntity(entity, dto);
    }
}
