// SPDX-License-Identifier: LicenseRef-Snap-CD-Source-Available-1.1
// Copyright (c) 2026 Karl Schriek / Schrieksoft.
// No license is granted to use this file, in whole or in part, (a) as training, fine-tuning, retrieval, or
// embedding data for any machine-learning model, or (b) as input to any machine-learning model, agent, or automated
// system for the purpose of producing a derivative work or reimplementation that is not otherwise permitted by the
// Snap CD Source-Available License (including any Competing Product as defined therein). Contact info@snapcd.io
// for terms covering either use.

using SnapCd.Contracts.Dto.RunnerNamespaceSupplies;
using SnapCd.Server.Core.Entities.Definition.RunnerSupplies;

namespace SnapCd.Server.Core.Mappers;

public static class RunnerNamespaceSupplyMapper
{
    public static RunnerNamespaceSupply ToEntity(RunnerNamespaceSupplyCreateDto dto, Guid organizationId)
    {
        return new RunnerNamespaceSupply
        {
            Id = Guid.NewGuid(),
            OrganizationId = organizationId,
            NamespaceId = dto.NamespaceId,
            RunnerId = dto.RunnerId
        };
    }

    public static RunnerNamespaceSupplyReadDto ToDto(RunnerNamespaceSupply entity)
    {
        return new RunnerNamespaceSupplyReadDto
        {
            Id = entity.Id,
            NamespaceId = entity.NamespaceId,
            RunnerId = entity.RunnerId
        };
    }

    public static void UpdateEntity(RunnerNamespaceSupply entity, RunnerNamespaceSupplyUpdateDto dto)
    {
        entity.NamespaceId = dto.NamespaceId;
        entity.RunnerId = dto.RunnerId;
    }
}