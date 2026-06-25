// SPDX-License-Identifier: LicenseRef-Snap-CD-Source-Available-1.1
// Copyright (c) 2026 Karl Schriek / Schrieksoft.
// No license is granted to use this file, in whole or in part, (a) as training, fine-tuning, retrieval, or
// embedding data for any machine-learning model, or (b) as input to any machine-learning model, agent, or automated
// system for the purpose of producing a derivative work or reimplementation that is not otherwise permitted by the
// Snap CD Source-Available License (including any Competing Product as defined therein). Contact info@snapcd.io
// for terms covering either use.

using SnapCd.Contracts.Dto.IntegrationModuleSupplies;
using SnapCd.Server.Core.Entities.Definition.IntegrationSupplies;

namespace SnapCd.Server.Core.Mappers;

public static class IntegrationModuleSupplyMapper
{
    public static IntegrationModuleSupply ToEntity(IntegrationModuleSupplyCreateDto dto, Guid organizationId)
    {
        return new IntegrationModuleSupply
        {
            Id = Guid.NewGuid(),
            OrganizationId = organizationId,
            ModuleId = dto.ModuleId,
            IntegrationId = dto.IntegrationId
        };
    }

    public static IntegrationModuleSupplyReadDto ToDto(IntegrationModuleSupply entity)
    {
        return new IntegrationModuleSupplyReadDto
        {
            Id = entity.Id,
            ModuleId = entity.ModuleId,
            IntegrationId = entity.IntegrationId
        };
    }

    public static void UpdateEntity(IntegrationModuleSupply entity, IntegrationModuleSupplyUpdateDto dto)
    {
        entity.ModuleId = dto.ModuleId;
        entity.IntegrationId = dto.IntegrationId;
    }
}
