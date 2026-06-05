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

public class OrganizationMissionService : GenericCrudService<
    OrganizationMission,
    OrganizationMissionCreateDto,
    OrganizationMissionUpdateDto,
    OrganizationMissionReadDto,
    OrganizationMissionSecuredRepository,
    OrganizationMissionRepository,
    OrganizationMissionCreatedEvent,
    OrganizationMissionUpdatedEvent,
    OrganizationMissionDeletedEvent,
    OrganizationMissionRepositorySettings>
{
    public OrganizationMissionService(
        OrganizationMissionSecuredRepository securedRepository
    ) : base(securedRepository)
    {
    }

    protected override OrganizationMission MapToEntity(OrganizationMissionCreateDto dto, Guid organizationId)
    {
        return OrganizationMissionMapper.ToEntity(dto, organizationId);
    }

    protected override OrganizationMissionReadDto MapToDto(OrganizationMission entity)
    {
        return OrganizationMissionMapper.ToDto(entity);
    }

    protected override void UpdateEntityFromDto(OrganizationMission entity, OrganizationMissionUpdateDto dto)
    {
        OrganizationMissionMapper.UpdateEntity(entity, dto);
    }

    public async Task<List<OrganizationMissionReadDto>> ListByAgent(Guid agentId, Guid organizationId)
    {
        var entities = await SecuredRepository.ListByAgent(agentId, organizationId);
        return entities.Select(MapToDto).ToList();
    }
}
