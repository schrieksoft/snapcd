using SnapCd.Contracts.Dto.Runners;
using SnapCd.Server.Core.Entities.Definition;
using SnapCd.Server.Core.Events.Repository.Organization;
using SnapCd.Server.Core.Mappers;
using SnapCd.Server.Core.Repositories.Organizations.Nonsecured;
using SnapCd.Server.Core.Repositories.Organizations.Secured;
using SnapCd.Server.Core.Services.Crud.Generic;
using SnapCd.Server.Core.Settings.Repositories;

namespace SnapCd.Server.Core.Services.Crud;

public class RunnerService : GenericCrudService<
    Runner,
    RunnerCreateDto,
    RunnerUpdateDto,
    RunnerReadDto,
    RunnerSecuredRepository,
    RunnerRepository,
    RunnerCreatedEvent,
    RunnerUpdatedEvent,
    RunnerDeletedEvent,
    RunnerRepositorySettings>
{
    public RunnerService(
        RunnerSecuredRepository securedRepository
    ) : base(securedRepository)
    {
    }

    protected override Runner MapToEntity(RunnerCreateDto dto, Guid organizationId)
    {
        return RunnerMapper.ToEntity(dto, organizationId);
    }

    protected override RunnerReadDto MapToDto(Runner entity)
    {
        return RunnerMapper.ToDto(entity);
    }

    protected override void UpdateEntityFromDto(Runner entity, RunnerUpdateDto dto)
    {
        RunnerMapper.UpdateEntity(entity, dto);
    }

    public async Task<RunnerReadDto> GetByName(string name, Guid organizationId)
    {
        return await GetByCriteria(repo => repo.GetByName(name, organizationId));
    }
}