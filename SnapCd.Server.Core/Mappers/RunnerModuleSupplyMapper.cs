// SPDX-License-Identifier: LicenseRef-Snap-CD-Source-Available-1.1
// Copyright (c) 2026 Karl Schriek / Schrieksoft.
// No license is granted to use this file, in whole or in part, (a) as training, fine-tuning, retrieval, or
// embedding data for any machine-learning model, or (b) as input to any machine-learning model, agent, or automated
// system for the purpose of producing a derivative work or reimplementation that is not otherwise permitted by the
// Snap CD Source-Available License (including any Competing Product as defined therein). Contact info@snapcd.io
// for terms covering either use.

using SnapCd.Contracts.Dto.RunnerModuleSupplies;
using SnapCd.Server.Core.Entities.Definition.RunnerSupplies;

namespace SnapCd.Server.Core.Mappers;

public static class RunnerModuleSupplyMapper
{
    public static RunnerModuleSupply ToEntity(RunnerModuleSupplyCreateDto dto, Guid organizationId)
    {
        return new RunnerModuleSupply
        {
            Id = Guid.NewGuid(),
            OrganizationId = organizationId,
            ModuleId = dto.ModuleId,
            RunnerId = dto.RunnerId
        };
    }

    public static RunnerModuleSupplyReadDto ToDto(RunnerModuleSupply entity)
    {
        return new RunnerModuleSupplyReadDto
        {
            Id = entity.Id,
            ModuleId = entity.ModuleId,
            RunnerId = entity.RunnerId
        };
    }

    public static void UpdateEntity(RunnerModuleSupply entity, RunnerModuleSupplyUpdateDto dto)
    {
        entity.ModuleId = dto.ModuleId;
        entity.RunnerId = dto.RunnerId;
    }
}