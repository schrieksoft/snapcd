// SPDX-License-Identifier: LicenseRef-Snap-CD-Source-Available-1.1
// Copyright (c) 2026 Karl Schriek / Schrieksoft.
// No license is granted to use this file, in whole or in part, (a) as training, fine-tuning, retrieval, or
// embedding data for any machine-learning model, or (b) as input to any machine-learning model, agent, or automated
// system for the purpose of producing a derivative work or reimplementation that is not otherwise permitted by the
// Snap CD Source-Available License (including any Competing Product as defined therein). Contact info@snapcd.io
// for terms covering either use.

using SnapCd.Contracts.Dto.OutputSetModuleSupplies;
using SnapCd.Server.Core.Entities.Definition.OutputSupplies;
using SnapCd.Server.Core.Views.Metadata;

namespace SnapCd.Server.Core.Mappers;

public static class OutputSetModuleSupplyMapper
{
    public static OutputSetModuleSupply ToEntity(OutputSetModuleSupplyCreateDto dto, Guid organizationId)
    {
        return new OutputSetModuleSupply
        {
            Id = Guid.NewGuid(),
            OrganizationId = organizationId,
            ModuleId = dto.ModuleId,
            ConsumingModuleId = dto.ConsumingModuleId
        };
    }

    public static OutputSetModuleSupplyReadDto ToDto(OutputSetModuleSupply entity)
    {
        return new OutputSetModuleSupplyReadDto
        {
            Id = entity.Id,
            ModuleId = entity.ModuleId,
            ConsumingModuleId = entity.ConsumingModuleId
        };
    }

    public static void UpdateEntity(OutputSetModuleSupply entity, OutputSetModuleSupplyUpdateDto dto)
    {
        entity.ModuleId = dto.ModuleId;
        entity.ConsumingModuleId = dto.ConsumingModuleId;
    }

    public static OutputSetModuleSupplyMetadataReadDto ToMetadataDto(OutputSetModuleSupplyMetadata view)
    {
        return new OutputSetModuleSupplyMetadataReadDto
        {
            Id = view.Id,
            ModuleId = view.ModuleId,
        };
    }
}
