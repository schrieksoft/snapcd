// SPDX-License-Identifier: LicenseRef-Snap-CD-Source-Available-1.1
// Copyright (c) 2026 Karl Schriek / Schrieksoft.
// No license is granted to use this file, in whole or in part, (a) as training, fine-tuning, retrieval, or
// embedding data for any machine-learning model, or (b) as input to any machine-learning model, agent, or automated
// system for the purpose of producing a derivative work or reimplementation that is not otherwise permitted by the
// Snap CD Source-Available License (including any Competing Product as defined therein). Contact info@snapcd.io
// for terms covering either use.

using SnapCd.Contracts.Dto.IntegrationNamespaceSupplies;
using SnapCd.Server.Core.Entities.Definition.IntegrationSupplies;

namespace SnapCd.Server.Core.Mappers;

public static class IntegrationNamespaceSupplyMapper
{
    public static IntegrationNamespaceSupply ToEntity(IntegrationNamespaceSupplyCreateDto dto, Guid organizationId)
    {
        return new IntegrationNamespaceSupply
        {
            Id = Guid.NewGuid(),
            OrganizationId = organizationId,
            NamespaceId = dto.NamespaceId,
            IntegrationId = dto.IntegrationId
        };
    }

    public static IntegrationNamespaceSupplyReadDto ToDto(IntegrationNamespaceSupply entity)
    {
        return new IntegrationNamespaceSupplyReadDto
        {
            Id = entity.Id,
            NamespaceId = entity.NamespaceId,
            IntegrationId = entity.IntegrationId
        };
    }

    public static void UpdateEntity(IntegrationNamespaceSupply entity, IntegrationNamespaceSupplyUpdateDto dto)
    {
        entity.NamespaceId = dto.NamespaceId;
        entity.IntegrationId = dto.IntegrationId;
    }
}
