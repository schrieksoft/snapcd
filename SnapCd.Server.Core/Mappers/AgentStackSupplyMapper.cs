// SPDX-License-Identifier: LicenseRef-Snap-CD-Source-Available-1.1
// Copyright (c) 2026 Karl Schriek / Schrieksoft.
// No license is granted to use this file, in whole or in part, (a) as training, fine-tuning, retrieval, or
// embedding data for any machine-learning model, or (b) as input to any machine-learning model, agent, or automated
// system for the purpose of producing a derivative work or reimplementation that is not otherwise permitted by the
// Snap CD Source-Available License (including any Competing Product as defined therein). Contact info@snapcd.io
// for terms covering either use.

using SnapCd.Contracts.Dto.AgentStackSupplies;
using SnapCd.Server.Core.Entities.Definition.AgentSupplies;

namespace SnapCd.Server.Core.Mappers;

public static class AgentStackSupplyMapper
{
    public static AgentStackSupply ToEntity(AgentStackSupplyCreateDto dto, Guid organizationId)
    {
        return new AgentStackSupply
        {
            Id = Guid.NewGuid(),
            OrganizationId = organizationId,
            StackId = dto.StackId,
            AgentId = dto.AgentId
        };
    }

    public static AgentStackSupplyReadDto ToDto(AgentStackSupply entity)
    {
        return new AgentStackSupplyReadDto
        {
            Id = entity.Id,
            StackId = entity.StackId,
            AgentId = entity.AgentId
        };
    }

    public static void UpdateEntity(AgentStackSupply entity, AgentStackSupplyUpdateDto dto)
    {
        entity.StackId = dto.StackId;
        entity.AgentId = dto.AgentId;
    }
}
