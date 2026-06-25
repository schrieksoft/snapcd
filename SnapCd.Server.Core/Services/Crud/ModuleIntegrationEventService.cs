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

public class ModuleIntegrationEventService : GenericCrudService<
    ModuleIntegrationEvent,
    ModuleIntegrationEventCreateDto,
    ModuleIntegrationEventUpdateDto,
    ModuleIntegrationEventReadDto,
    ModuleIntegrationEventSecuredRepository,
    ModuleIntegrationEventRepository,
    ModuleIntegrationEventCreatedEvent,
    ModuleIntegrationEventUpdatedEvent,
    ModuleIntegrationEventDeletedEvent,
    ModuleIntegrationEventRepositorySettings>
{
    public ModuleIntegrationEventService(
        ModuleIntegrationEventSecuredRepository securedRepository
    ) : base(securedRepository)
    {
    }

    protected override ModuleIntegrationEvent MapToEntity(ModuleIntegrationEventCreateDto dto, Guid organizationId)
    {
        return ModuleIntegrationEventMapper.ToEntity(dto, organizationId);
    }

    protected override ModuleIntegrationEventReadDto MapToDto(ModuleIntegrationEvent entity)
    {
        return ModuleIntegrationEventMapper.ToDto(entity);
    }

    protected override void UpdateEntityFromDto(ModuleIntegrationEvent entity, ModuleIntegrationEventUpdateDto dto)
    {
        ModuleIntegrationEventMapper.UpdateEntity(entity, dto);
    }

    public async Task<List<ModuleIntegrationEventReadDto>> ListByIntegration(Guid integrationId, Guid organizationId)
    {
        var entities = await SecuredRepository.ListByIntegration(integrationId, organizationId);
        return entities.Select(MapToDto).ToList();
    }

    public async Task<List<ModuleIntegrationEventReadDto>> ListByModule(Guid moduleId, Guid organizationId)
    {
        var entities = await SecuredRepository.ListByModule(moduleId, organizationId);
        return entities.Select(MapToDto).ToList();
    }
}
