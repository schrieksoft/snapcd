// SPDX-License-Identifier: LicenseRef-Snap-CD-Source-Available-1.1
// Copyright (c) 2026 Karl Schriek / Schrieksoft.
// No license is granted to use this file, in whole or in part, (a) as training, fine-tuning, retrieval, or
// embedding data for any machine-learning model, or (b) as input to any machine-learning model, agent, or automated
// system for the purpose of producing a derivative work or reimplementation that is not otherwise permitted by the
// Snap CD Source-Available License (including any Competing Product as defined therein). Contact info@snapcd.io
// for terms covering either use.

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