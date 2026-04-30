using SnapCd.Contracts.Dto.ModuleTerraformFlags;
using SnapCd.Server.Core.Entities.Definition;
using SnapCd.Server.Core.Events.Repository.Organization;
using SnapCd.Server.Core.Mappers;
using SnapCd.Server.Core.Repositories.Organizations.Nonsecured;
using SnapCd.Server.Core.Repositories.Organizations.Secured;
using SnapCd.Server.Core.Services.Crud.Generic;
using SnapCd.Server.Core.Settings.Repositories;

namespace SnapCd.Server.Core.Services.Crud;

public class ModuleTerraformFlagService : GenericCrudService<ModuleTerraformFlag, ModuleTerraformFlagCreateDto, ModuleTerraformFlagUpdateDto, ModuleTerraformFlagReadDto, ModuleTerraformFlagSecuredRepository, ModuleTerraformFlagRepository,
    ModuleTerraformFlagCreatedEvent, ModuleTerraformFlagUpdatedEvent, ModuleTerraformFlagDeletedEvent, ModuleTerraformFlagRepositorySettings>
{
    public ModuleTerraformFlagService(
        ModuleTerraformFlagSecuredRepository securedRepository
    ) : base(securedRepository)
    {
    }

    protected override ModuleTerraformFlag MapToEntity(ModuleTerraformFlagCreateDto dto, Guid organizationId)
    {
        return ModuleTerraformFlagMapper.ToEntity(dto, organizationId);
    }

    protected override ModuleTerraformFlagReadDto MapToDto(ModuleTerraformFlag entity)
    {
        return ModuleTerraformFlagMapper.ToDto(entity);
    }

    protected override void UpdateEntityFromDto(ModuleTerraformFlag entity, ModuleTerraformFlagUpdateDto dto)
    {
        ModuleTerraformFlagMapper.UpdateEntity(entity, dto);
    }
}
