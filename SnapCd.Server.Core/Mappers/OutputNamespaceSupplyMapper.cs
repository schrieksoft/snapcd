// SPDX-License-Identifier: LicenseRef-Snap-CD-Source-Available-1.1
// Copyright (c) 2026 Karl Schriek / Schrieksoft.
// No license is granted to use this file, in whole or in part, (a) as training, fine-tuning, retrieval, or
// embedding data for any machine-learning model, or (b) as input to any machine-learning model, agent, or automated
// system for the purpose of producing a derivative work or reimplementation that is not otherwise permitted by the
// Snap CD Source-Available License (including any Competing Product as defined therein). Contact info@snapcd.io
// for terms covering either use.

using SnapCd.Contracts.Dto.OutputNamespaceSupplies;
using SnapCd.Server.Core.Entities.Definition.OutputSupplies;
using SnapCd.Server.Core.Views.Metadata;

namespace SnapCd.Server.Core.Mappers;

public static class OutputNamespaceSupplyMapper
{
    public static OutputNamespaceSupply ToEntity(OutputNamespaceSupplyCreateDto dto, Guid organizationId)
    {
        return new OutputNamespaceSupply
        {
            Id = Guid.NewGuid(),
            OrganizationId = organizationId,
            ModuleId = dto.ModuleId,
            OutputName = dto.OutputName,
            ConsumingNamespaceId = dto.ConsumingNamespaceId
        };
    }

    public static OutputNamespaceSupplyReadDto ToDto(OutputNamespaceSupply entity)
    {
        return new OutputNamespaceSupplyReadDto
        {
            Id = entity.Id,
            ModuleId = entity.ModuleId,
            OutputName = entity.OutputName,
            ConsumingNamespaceId = entity.ConsumingNamespaceId
        };
    }

    public static void UpdateEntity(OutputNamespaceSupply entity, OutputNamespaceSupplyUpdateDto dto)
    {
        entity.ModuleId = dto.ModuleId;
        entity.OutputName = dto.OutputName;
        entity.ConsumingNamespaceId = dto.ConsumingNamespaceId;
    }

    public static OutputNamespaceSupplyMetadataReadDto ToMetadataDto(OutputNamespaceSupplyMetadata view)
    {
        return new OutputNamespaceSupplyMetadataReadDto
        {
            Id = view.Id,
            ModuleId = view.ModuleId,
        };
    }
}
