// SPDX-License-Identifier: LicenseRef-Snap-CD-Source-Available-1.1
// Copyright (c) 2026 Karl Schriek / Schrieksoft.
// No license is granted to use this file, in whole or in part, (a) as training, fine-tuning, retrieval, or
// embedding data for any machine-learning model, or (b) as input to any machine-learning model, agent, or automated
// system for the purpose of producing a derivative work or reimplementation that is not otherwise permitted by the
// Snap CD Source-Available License (including any Competing Product as defined therein). Contact info@snapcd.io
// for terms covering either use.

using SnapCd.Contracts.Dto.Missions;
using SnapCd.Server.Core.Entities.Definition.Missions;
using SnapCd.Server.Core.Events.Repository.Organization;
using SnapCd.Server.Core.Mappers;
using SnapCd.Server.Core.Repositories.Organizations.Nonsecured;
using SnapCd.Server.Core.Repositories.Organizations.Secured;
using SnapCd.Server.Core.Services.Crud.Generic;
using SnapCd.Server.Core.Settings.Repositories;

namespace SnapCd.Server.Core.Services.Crud;

public class StackMissionService : GenericCrudService<
    StackMission,
    StackMissionCreateDto,
    StackMissionUpdateDto,
    StackMissionReadDto,
    StackMissionSecuredRepository,
    StackMissionRepository,
    StackMissionCreatedEvent,
    StackMissionUpdatedEvent,
    StackMissionDeletedEvent,
    StackMissionRepositorySettings>
{
    public StackMissionService(
        StackMissionSecuredRepository securedRepository
    ) : base(securedRepository)
    {
    }

    protected override StackMission MapToEntity(StackMissionCreateDto dto, Guid organizationId)
    {
        return StackMissionMapper.ToEntity(dto, organizationId);
    }

    protected override StackMissionReadDto MapToDto(StackMission entity)
    {
        return StackMissionMapper.ToDto(entity);
    }

    protected override void UpdateEntityFromDto(StackMission entity, StackMissionUpdateDto dto)
    {
        StackMissionMapper.UpdateEntity(entity, dto);
    }

    public async Task<List<StackMissionReadDto>> ListByAgent(Guid agentId, Guid organizationId)
    {
        var entities = await SecuredRepository.ListByAgent(agentId, organizationId);
        return entities.Select(MapToDto).ToList();
    }

    public async Task<List<StackMissionReadDto>> ListByStack(Guid stackId, Guid organizationId)
    {
        var entities = await SecuredRepository.ListByStack(stackId, organizationId);
        return entities.Select(MapToDto).ToList();
    }
}
