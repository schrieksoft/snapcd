// SPDX-License-Identifier: LicenseRef-Snap-CD-Source-Available-1.1
// Copyright (c) 2026 Karl Schriek / Schrieksoft.
// No license is granted to use this file, in whole or in part, (a) as training, fine-tuning, retrieval, or
// embedding data for any machine-learning model, or (b) as input to any machine-learning model, agent, or automated
// system for the purpose of producing a derivative work or reimplementation that is not otherwise permitted by the
// Snap CD Source-Available License (including any Competing Product as defined therein). Contact info@snapcd.io
// for terms covering either use.

using SnapCd.Contracts.Dto.IntegrationEvents;
using SnapCd.Server.Core.Entities.Definition.IntegrationEvents;

namespace SnapCd.Server.Core.Mappers;

public static class NamespaceIntegrationEventMapper
{
    public static NamespaceIntegrationEvent ToEntity(NamespaceIntegrationEventCreateDto dto, Guid organizationId)
    {
        return new NamespaceIntegrationEvent
        {
            Id = Guid.NewGuid(),
            OrganizationId = organizationId,
            IntegrationId = dto.IntegrationId,
            NamespaceId = dto.NamespaceId,
            Trigger = dto.Trigger,
            Template = dto.Template,
            Filter = dto.Filter,
            IsDisabled = dto.IsDisabled
        };
    }

    public static NamespaceIntegrationEventReadDto ToDto(NamespaceIntegrationEvent entity)
    {
        return new NamespaceIntegrationEventReadDto
        {
            Id = entity.Id,
            IntegrationId = entity.IntegrationId,
            NamespaceId = entity.NamespaceId,
            Trigger = entity.Trigger,
            Template = entity.Template,
            Filter = entity.Filter,
            IsDisabled = entity.IsDisabled
        };
    }

    public static void UpdateEntity(NamespaceIntegrationEvent entity, NamespaceIntegrationEventUpdateDto dto)
    {
        entity.IntegrationId = dto.IntegrationId;
        entity.NamespaceId = dto.NamespaceId;
        entity.Trigger = dto.Trigger;
        entity.Template = dto.Template;
        entity.Filter = dto.Filter;
        entity.IsDisabled = dto.IsDisabled;
    }
}
