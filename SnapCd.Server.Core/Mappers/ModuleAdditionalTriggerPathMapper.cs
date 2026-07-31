// SPDX-License-Identifier: LicenseRef-Snap-CD-Source-Available-1.1
// Copyright (c) 2026 Karl Schriek / Schrieksoft.
// No license is granted to use this file, in whole or in part, (a) as training, fine-tuning, retrieval, or
// embedding data for any machine-learning model, or (b) as input to any machine-learning model, agent, or automated
// system for the purpose of producing a derivative work or reimplementation that is not otherwise permitted by the
// Snap CD Source-Available License (including any Competing Product as defined therein). Contact info@snapcd.io
// for terms covering either use.

using SnapCd.Contracts.Dto.ModuleAdditionalTriggerPaths;
using SnapCd.Server.Core.Entities.Definition;

namespace SnapCd.Server.Core.Mappers;

public static class ModuleAdditionalTriggerPathMapper
{
    public static ModuleAdditionalTriggerPath ToEntity(ModuleAdditionalTriggerPathCreateDto dto, Guid organizationId)
    {
        return new ModuleAdditionalTriggerPath
        {
            Id = Guid.NewGuid(),
            OrganizationId = organizationId,
            ModuleId = dto.ModuleId,
            Path = dto.Path
        };
    }

    public static ModuleAdditionalTriggerPathReadDto ToDto(ModuleAdditionalTriggerPath entity)
    {
        return new ModuleAdditionalTriggerPathReadDto
        {
            Id = entity.Id,
            ModuleId = entity.ModuleId,
            Path = entity.Path
        };
    }

    public static void UpdateEntity(ModuleAdditionalTriggerPath entity, ModuleAdditionalTriggerPathUpdateDto dto)
    {
        entity.ModuleId = dto.ModuleId;
        entity.Path = dto.Path;
    }
}
