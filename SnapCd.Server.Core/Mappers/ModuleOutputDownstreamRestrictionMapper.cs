// SPDX-License-Identifier: LicenseRef-Snap-CD-Source-Available-1.1
// Copyright (c) 2026 Karl Schriek / Schrieksoft.
// No license is granted to use this file, in whole or in part, (a) as training, fine-tuning, retrieval, or
// embedding data for any machine-learning model, or (b) as input to any machine-learning model, agent, or automated
// system for the purpose of producing a derivative work or reimplementation that is not otherwise permitted by the
// Snap CD Source-Available License (including any Competing Product as defined therein). Contact info@snapcd.io
// for terms covering either use.

using SnapCd.Contracts.Dto.ModuleOutputDownstreamRestrictions;
using SnapCd.Server.Core.Entities.Definition;
using SnapCd.Server.Core.Views.Metadata;

namespace SnapCd.Server.Core.Mappers;

public static class ModuleOutputDownstreamRestrictionMapper
{
    public static ModuleOutputDownstreamRestriction ToEntity(ModuleOutputDownstreamRestrictionCreateDto dto, Guid organizationId)
    {
        return new ModuleOutputDownstreamRestriction
        {
            Id = Guid.NewGuid(),
            OrganizationId = organizationId,
            ModuleId = dto.ModuleId,
            OutputName = dto.OutputName
        };
    }

    public static ModuleOutputDownstreamRestrictionReadDto ToDto(ModuleOutputDownstreamRestriction entity)
    {
        return new ModuleOutputDownstreamRestrictionReadDto
        {
            Id = entity.Id,
            ModuleId = entity.ModuleId,
            OutputName = entity.OutputName
        };
    }

    public static void UpdateEntity(ModuleOutputDownstreamRestriction entity, ModuleOutputDownstreamRestrictionUpdateDto dto)
    {
        entity.ModuleId = dto.ModuleId;
        entity.OutputName = dto.OutputName;
    }

    public static ModuleOutputDownstreamRestrictionMetadataReadDto ToMetadataDto(ModuleOutputDownstreamRestrictionMetadata view)
    {
        return new ModuleOutputDownstreamRestrictionMetadataReadDto
        {
            Id = view.Id,
            ModuleId = view.ModuleId,
        };
    }
}
