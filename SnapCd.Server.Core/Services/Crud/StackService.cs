using SnapCd.Contracts.Dto.Stacks;
using SnapCd.Server.Core.Entities.Definition;
using SnapCd.Server.Core.Events.Repository.Organization;
using SnapCd.Server.Core.Mappers;
using SnapCd.Server.Core.Repositories.Organizations.Nonsecured;
using SnapCd.Server.Core.Repositories.Organizations.Secured;
using SnapCd.Server.Core.Services.Crud.Generic;
using SnapCd.Server.Core.Settings.Repositories;

namespace SnapCd.Server.Core.Services.Crud;

public class StackService : GenericCrudService<
    Stack,
    StackCreateDto,
    StackUpdateDto,
    StackReadDto,
    StackSecuredRepository,
    StackRepository,
    StackCreatedEvent,
    StackUpdatedEvent,
    StackDeletedEvent,
    StackRepositorySettings>
{
    public StackService(
        StackSecuredRepository securedRepository
    ) : base(securedRepository)
    {
    }

    protected override Stack MapToEntity(StackCreateDto dto, Guid organizationId)
    {
        return StackMapper.ToEntity(dto, organizationId);
    }

    protected override StackReadDto MapToDto(Stack entity)
    {
        return StackMapper.ToDto(entity);
    }

    protected override void UpdateEntityFromDto(Stack entity, StackUpdateDto dto)
    {
        StackMapper.UpdateEntity(entity, dto);
    }

    public async Task<StackReadDto> GetByName(string name, Guid organizationId)
    {
        return await GetByCriteria(repo => repo.GetByName(name, organizationId));
    }
}