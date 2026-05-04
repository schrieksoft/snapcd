using SnapCd.Contracts.Dto.Groups;
using SnapCd.Server.Core.Entities.Definition;
using SnapCd.Server.Core.Events.Repository.Organization;
using SnapCd.Server.Core.Mappers;
using SnapCd.Server.Core.Repositories.Organizations.Nonsecured;
using SnapCd.Server.Core.Repositories.Organizations.Secured;
using SnapCd.Server.Core.Services.Crud.Generic;
using SnapCd.Server.Core.Settings.Repositories;

namespace SnapCd.Server.Core.Services.Crud;

public class GroupService : GenericCrudService<
    Group,
    GroupCreateDto,
    GroupUpdateDto,
    GroupReadDto,
    GroupSecuredRepository,
    GroupRepository,
    GroupCreatedEvent,
    GroupUpdatedEvent,
    GroupDeletedEvent,
    GroupRepositorySettings>
{
    public GroupService(
        GroupSecuredRepository securedRepository
    ) : base(securedRepository)
    {
    }

    protected override Group MapToEntity(GroupCreateDto dto, Guid organizationId)
    {
        return GroupMapper.ToEntity(dto, organizationId);
    }

    protected override GroupReadDto MapToDto(Group entity)
    {
        return GroupMapper.ToDto(entity);
    }

    protected override void UpdateEntityFromDto(Group entity, GroupUpdateDto dto)
    {
        GroupMapper.UpdateEntity(entity, dto);
    }

    public async Task<GroupReadDto> GetByName(string name, Guid organizationId)
    {
        return await GetByCriteria(async repo =>
            await repo.GetByName(name, organizationId)
            ?? throw new KeyNotFoundException($"Group '{name}' not found in organization {organizationId}."));
    }
}