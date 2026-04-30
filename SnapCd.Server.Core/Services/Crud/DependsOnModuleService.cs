using SnapCd.Contracts.Dto.DependsOnModules;
using SnapCd.Server.Core.Entities.Definition;
using SnapCd.Server.Core.Events.Repository.Organization;
using SnapCd.Server.Core.Mappers;
using SnapCd.Server.Core.Repositories.Organizations.Nonsecured;
using SnapCd.Server.Core.Repositories.Organizations.Secured;
using SnapCd.Server.Core.Services.Crud.Generic;
using SnapCd.Server.Core.Settings.Repositories;

namespace SnapCd.Server.Core.Services.Crud;

public class DependsOnModuleService : GenericCrudService<
    DependsOnModule,
    DependsOnModuleCreateDto,
    DependsOnModuleUpdateDto,
    DependsOnModuleReadDto,
    DependsOnModuleSecuredRepository,
    DependsOnModuleRepository,
    DependsOnModuleCreatedEvent,
    DependsOnModuleUpdatedEvent,
    DependsOnModuleDeletedEvent,
    DependsOnModuleRepositorySettings>
{
    public DependsOnModuleService(
        DependsOnModuleSecuredRepository securedRepository
    ) : base(securedRepository)
    {
    }

    protected override DependsOnModule MapToEntity(DependsOnModuleCreateDto dto, Guid organizationId)
    {
        return DependsOnModuleMapper.ToEntity(dto, organizationId);
    }

    protected override DependsOnModuleReadDto MapToDto(DependsOnModule entity)
    {
        return DependsOnModuleMapper.ToDto(entity);
    }

    protected override void UpdateEntityFromDto(DependsOnModule entity, DependsOnModuleUpdateDto dto)
    {
        DependsOnModuleMapper.UpdateEntity(entity, dto);
    }

    public async Task<DependsOnModuleReadDto> Get(Guid moduleId, Guid dependsOnModuleId, Guid organizationId)
    {
        var entity = await SecuredRepository.Get(moduleId, dependsOnModuleId);
        return DependsOnModuleMapper.ToDto(entity);
    }
}