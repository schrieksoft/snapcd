// SPDX-License-Identifier: LicenseRef-Snap-CD-Source-Available-1.1
// Copyright (c) 2026 Karl Schriek / Schrieksoft.
// No license is granted to use this file, in whole or in part, (a) as training, fine-tuning, retrieval, or
// embedding data for any machine-learning model, or (b) as input to any machine-learning model, agent, or automated
// system for the purpose of producing a derivative work or reimplementation that is not otherwise permitted by the
// Snap CD Source-Available License (including any Competing Product as defined therein). Contact info@snapcd.io
// for terms covering either use.

using SnapCd.Contracts.Dto.IntegrationEvents;
using SnapCd.Server.Core.Entities.Definition.IntegrationEvents;
using SnapCd.Server.Core.Events.Repository.Organization;
using SnapCd.Server.Core.Mappers;
using SnapCd.Server.Core.Repositories.Organizations.Nonsecured;
using SnapCd.Server.Core.Repositories.Organizations.Secured;
using SnapCd.Server.Core.Services.Crud.Generic;
using SnapCd.Server.Core.Settings.Repositories;

namespace SnapCd.Server.Core.Services.Crud;

public class StackIntegrationEventService : GenericCrudService<
    StackIntegrationEvent,
    StackIntegrationEventCreateDto,
    StackIntegrationEventUpdateDto,
    StackIntegrationEventReadDto,
    StackIntegrationEventSecuredRepository,
    StackIntegrationEventRepository,
    StackIntegrationEventCreatedEvent,
    StackIntegrationEventUpdatedEvent,
    StackIntegrationEventDeletedEvent,
    StackIntegrationEventRepositorySettings>
{
    public StackIntegrationEventService(
        StackIntegrationEventSecuredRepository securedRepository
    ) : base(securedRepository)
    {
    }

    protected override StackIntegrationEvent MapToEntity(StackIntegrationEventCreateDto dto, Guid organizationId)
    {
        return StackIntegrationEventMapper.ToEntity(dto, organizationId);
    }

    protected override StackIntegrationEventReadDto MapToDto(StackIntegrationEvent entity)
    {
        return StackIntegrationEventMapper.ToDto(entity);
    }

    protected override void UpdateEntityFromDto(StackIntegrationEvent entity, StackIntegrationEventUpdateDto dto)
    {
        StackIntegrationEventMapper.UpdateEntity(entity, dto);
    }

    public async Task<List<StackIntegrationEventReadDto>> ListByIntegration(Guid integrationId, Guid organizationId)
    {
        var entities = await SecuredRepository.ListByIntegration(integrationId, organizationId);
        return entities.Select(MapToDto).ToList();
    }

    public async Task<List<StackIntegrationEventReadDto>> ListByStack(Guid stackId, Guid organizationId)
    {
        var entities = await SecuredRepository.ListByStack(stackId, organizationId);
        return entities.Select(MapToDto).ToList();
    }
}
