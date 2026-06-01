// SPDX-License-Identifier: LicenseRef-Snap-CD-Source-Available-1.1
// Copyright (c) 2026 Karl Schriek / Schrieksoft.
// No license is granted to use this file, in whole or in part, (a) as training, fine-tuning, retrieval, or
// embedding data for any machine-learning model, or (b) as input to any machine-learning model, agent, or automated
// system for the purpose of producing a derivative work or reimplementation that is not otherwise permitted by the
// Snap CD Source-Available License (including any Competing Product as defined therein). Contact info@snapcd.io
// for terms covering either use.

using SnapCd.Contracts.Dto.ModuleHooks;
using SnapCd.Server.Core.Entities.Definition;

namespace SnapCd.Server.Core.Mappers;

public static class ModuleHookMapper
{
    public static ModuleHook ToEntity(ModuleHookCreateDto dto, Guid organizationId)
    {
        return new ModuleHook
        {
            Id = Guid.NewGuid(),
            OrganizationId = organizationId,
            Task = dto.Task,
            Phase = dto.Phase,
            Script = dto.Script,
            ModuleId = dto.ModuleId
        };
    }

    public static ModuleHookReadDto ToDto(ModuleHook entity)
    {
        return new ModuleHookReadDto
        {
            Id = entity.Id,
            Task = entity.Task,
            Phase = entity.Phase,
            Script = entity.Script,
            ModuleId = entity.ModuleId
        };
    }

    public static void UpdateEntity(ModuleHook entity, ModuleHookUpdateDto dto)
    {
        entity.Task = dto.Task;
        entity.Phase = dto.Phase;
        entity.Script = dto.Script;
        entity.ModuleId = dto.ModuleId;
    }
}
