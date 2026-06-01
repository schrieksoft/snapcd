// SPDX-License-Identifier: LicenseRef-Snap-CD-Source-Available-1.1
// Copyright (c) 2026 Karl Schriek / Schrieksoft.
// No license is granted to use this file, in whole or in part, (a) as training, fine-tuning, retrieval, or
// embedding data for any machine-learning model, or (b) as input to any machine-learning model, agent, or automated
// system for the purpose of producing a derivative work or reimplementation that is not otherwise permitted by the
// Snap CD Source-Available License (including any Competing Product as defined therein). Contact info@snapcd.io
// for terms covering either use.

using SnapCd.Contracts.Dto.ModuleTerraformArrayFlags;
using SnapCd.Server.Core.Entities.Definition;

namespace SnapCd.Server.Core.Mappers;

public static class ModuleTerraformArrayFlagMapper
{
    public static ModuleTerraformArrayFlag ToEntity(ModuleTerraformArrayFlagCreateDto dto, Guid organizationId)
    {
        return new ModuleTerraformArrayFlag
        {
            Id = Guid.NewGuid(),
            OrganizationId = organizationId,
            Task = dto.Task,
            Flag = dto.Flag,
            Value = dto.Value,
            ModuleId = dto.ModuleId
        };
    }

    public static ModuleTerraformArrayFlagReadDto ToDto(ModuleTerraformArrayFlag entity)
    {
        return new ModuleTerraformArrayFlagReadDto
        {
            Id = entity.Id,
            Task = entity.Task,
            Flag = entity.Flag,
            Value = entity.Value,
            ModuleId = entity.ModuleId
        };
    }

    public static void UpdateEntity(ModuleTerraformArrayFlag entity, ModuleTerraformArrayFlagUpdateDto dto)
    {
        entity.Task = dto.Task;
        entity.Flag = dto.Flag;
        entity.Value = dto.Value;
        entity.ModuleId = dto.ModuleId;
    }
}
