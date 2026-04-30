using SnapCd.Contracts.Dto.ModuleExtraFiles;
using SnapCd.Server.Core.Entities.Definition;
using SnapCd.Server.Core.Events.Repository.Organization;
using SnapCd.Server.Core.Mappers;
using SnapCd.Server.Core.Repositories.Organizations.Nonsecured;
using SnapCd.Server.Core.Repositories.Organizations.Secured;
using SnapCd.Server.Core.Services.Crud.Generic;
using SnapCd.Server.Core.Settings.Repositories;

namespace SnapCd.Server.Core.Services.Crud;

public class ModuleExtraFileService : GenericCrudService<ModuleExtraFile, ModuleExtraFileCreateDto, ModuleExtraFileUpdateDto, ModuleExtraFileReadDto, ModuleExtraFileSecuredRepository, ModuleExtraFileRepository, ModuleExtraFileCreatedEvent,
    ModuleExtraFileUpdatedEvent, ModuleExtraFileDeletedEvent, ModuleExtraFileRepositorySettings>
{
    public ModuleExtraFileService(
        ModuleExtraFileSecuredRepository securedRepository
    ) : base(securedRepository)
    {
    }

    protected override ModuleExtraFile MapToEntity(ModuleExtraFileCreateDto dto, Guid organizationId)
    {
        return ModuleExtraFileMapper.ToEntity(dto, organizationId);
    }

    protected override ModuleExtraFileReadDto MapToDto(ModuleExtraFile entity)
    {
        return ModuleExtraFileMapper.ToDto(entity);
    }

    protected override void UpdateEntityFromDto(ModuleExtraFile entity, ModuleExtraFileUpdateDto dto)
    {
        ModuleExtraFileMapper.UpdateEntity(entity, dto);
    }

    public async Task<ModuleExtraFileReadDto> Get(Guid moduleId, string name, Guid organizationId)
    {
        var entity = await SecuredRepository.Get(moduleId, name, organizationId);
        return ModuleExtraFileMapper.ToDto(entity);
    }
}