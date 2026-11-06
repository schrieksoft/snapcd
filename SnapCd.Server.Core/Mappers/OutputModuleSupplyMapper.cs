// SPDX-License-Identifier: LicenseRef-Snap-CD-Source-Available-1.1
// Copyright (c) 2026 Karl Schriek / Schrieksoft.
// No license is granted to use this file, in whole or in part, (a) as training, fine-tuning, retrieval, or
// embedding data for any machine-learning model, or (b) as input to any machine-learning model, agent, or automated
// system for the purpose of producing a derivative work or reimplementation that is not otherwise permitted by the
// Snap CD Source-Available License (including any Competing Product as defined therein). Contact info@snapcd.io
// for terms covering either use.

using SnapCd.Contracts.Dto.OutputModuleSupplies;
using SnapCd.Server.Core.Entities.Definition.OutputSupplies;
using SnapCd.Server.Core.Views.Metadata;

namespace SnapCd.Server.Core.Mappers;

public static class OutputModuleSupplyMapper
{
    public static OutputModuleSupply ToEntity(OutputModuleSupplyCreateDto dto, Guid organizationId)
    {
        return new OutputModuleSupply
        {
            Id = Guid.NewGuid(),
            OrganizationId = organizationId,
            ModuleId = dto.ModuleId,
            OutputName = dto.OutputName,
            ConsumingModuleId = dto.ConsumingModuleId
        };
    }

    public static OutputModuleSupplyReadDto ToDto(OutputModuleSupply entity)
    {
        return new OutputModuleSupplyReadDto
        {
            Id = entity.Id,
            ModuleId = entity.ModuleId,
            OutputName = entity.OutputName,
            ConsumingModuleId = entity.ConsumingModuleId
        };
    }

    public static void UpdateEntity(OutputModuleSupply entity, OutputModuleSupplyUpdateDto dto)
    {
        entity.ModuleId = dto.ModuleId;
        entity.OutputName = dto.OutputName;
        entity.ConsumingModuleId = dto.ConsumingModuleId;
    }

    public static OutputModuleSupplyMetadataReadDto ToMetadataDto(OutputModuleSupplyMetadata view)
    {
        return new OutputModuleSupplyMetadataReadDto
        {
            Id = view.Id,
            ModuleId = view.ModuleId,
        };
    }
}
