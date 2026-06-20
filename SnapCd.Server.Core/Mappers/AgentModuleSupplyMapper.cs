// SPDX-License-Identifier: LicenseRef-Snap-CD-Source-Available-1.1
// Copyright (c) 2026 Karl Schriek / Schrieksoft.
// No license is granted to use this file, in whole or in part, (a) as training, fine-tuning, retrieval, or
// embedding data for any machine-learning model, or (b) as input to any machine-learning model, agent, or automated
// system for the purpose of producing a derivative work or reimplementation that is not otherwise permitted by the
// Snap CD Source-Available License (including any Competing Product as defined therein). Contact info@snapcd.io
// for terms covering either use.

using SnapCd.Contracts.Dto.AgentModuleSupplies;
using SnapCd.Server.Core.Entities.Definition.AgentSupplies;

namespace SnapCd.Server.Core.Mappers;

public static class AgentModuleSupplyMapper
{
    public static AgentModuleSupply ToEntity(AgentModuleSupplyCreateDto dto, Guid organizationId)
    {
        return new AgentModuleSupply
        {
            Id = Guid.NewGuid(),
            OrganizationId = organizationId,
            ModuleId = dto.ModuleId,
            AgentId = dto.AgentId
        };
    }

    public static AgentModuleSupplyReadDto ToDto(AgentModuleSupply entity)
    {
        return new AgentModuleSupplyReadDto
        {
            Id = entity.Id,
            ModuleId = entity.ModuleId,
            AgentId = entity.AgentId
        };
    }

    public static void UpdateEntity(AgentModuleSupply entity, AgentModuleSupplyUpdateDto dto)
    {
        entity.ModuleId = dto.ModuleId;
        entity.AgentId = dto.AgentId;
    }
}
