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

public class NamespaceIntegrationEventService : GenericCrudService<
    NamespaceIntegrationEvent,
    NamespaceIntegrationEventCreateDto,
    NamespaceIntegrationEventUpdateDto,
    NamespaceIntegrationEventReadDto,
    NamespaceIntegrationEventSecuredRepository,
    NamespaceIntegrationEventRepository,
    NamespaceIntegrationEventCreatedEvent,
    NamespaceIntegrationEventUpdatedEvent,
    NamespaceIntegrationEventDeletedEvent,
    NamespaceIntegrationEventRepositorySettings>
{
    public NamespaceIntegrationEventService(
        NamespaceIntegrationEventSecuredRepository securedRepository
    ) : base(securedRepository)
    {
    }

    protected override NamespaceIntegrationEvent MapToEntity(NamespaceIntegrationEventCreateDto dto, Guid organizationId)
    {
        return NamespaceIntegrationEventMapper.ToEntity(dto, organizationId);
    }

    protected override NamespaceIntegrationEventReadDto MapToDto(NamespaceIntegrationEvent entity)
    {
        return NamespaceIntegrationEventMapper.ToDto(entity);
    }

    protected override void UpdateEntityFromDto(NamespaceIntegrationEvent entity, NamespaceIntegrationEventUpdateDto dto)
    {
        NamespaceIntegrationEventMapper.UpdateEntity(entity, dto);
    }

    public async Task<List<NamespaceIntegrationEventReadDto>> ListByIntegration(Guid integrationId, Guid organizationId)
    {
        var entities = await SecuredRepository.ListByIntegration(integrationId, organizationId);
        return entities.Select(MapToDto).ToList();
    }

    public async Task<List<NamespaceIntegrationEventReadDto>> ListByNamespace(Guid namespaceId, Guid organizationId)
    {
        var entities = await SecuredRepository.ListByNamespace(namespaceId, organizationId);
        return entities.Select(MapToDto).ToList();
    }
}
