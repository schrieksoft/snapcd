// SPDX-License-Identifier: LicenseRef-Snap-CD-Source-Available-1.1
// Copyright (c) 2026 Karl Schriek / Schrieksoft.
// No license is granted to use this file, in whole or in part, (a) as training, fine-tuning, retrieval, or
// embedding data for any machine-learning model, or (b) as input to any machine-learning model, agent, or automated
// system for the purpose of producing a derivative work or reimplementation that is not otherwise permitted by the
// Snap CD Source-Available License (including any Competing Product as defined therein). Contact info@snapcd.io
// for terms covering either use.

using SnapCd.Contracts.Dto.IntegrationStackSupplies;
using SnapCd.Server.Core.Entities.Definition.IntegrationSupplies;

namespace SnapCd.Server.Core.Mappers;

public static class IntegrationStackSupplyMapper
{
    public static IntegrationStackSupply ToEntity(IntegrationStackSupplyCreateDto dto, Guid organizationId)
    {
        return new IntegrationStackSupply
        {
            Id = Guid.NewGuid(),
            OrganizationId = organizationId,
            StackId = dto.StackId,
            IntegrationId = dto.IntegrationId
        };
    }

    public static IntegrationStackSupplyReadDto ToDto(IntegrationStackSupply entity)
    {
        return new IntegrationStackSupplyReadDto
        {
            Id = entity.Id,
            StackId = entity.StackId,
            IntegrationId = entity.IntegrationId
        };
    }

    public static void UpdateEntity(IntegrationStackSupply entity, IntegrationStackSupplyUpdateDto dto)
    {
        entity.StackId = dto.StackId;
        entity.IntegrationId = dto.IntegrationId;
    }
}
