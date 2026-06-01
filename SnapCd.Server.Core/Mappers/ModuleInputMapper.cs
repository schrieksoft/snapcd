// SPDX-License-Identifier: LicenseRef-Snap-CD-Source-Available-1.1
// Copyright (c) 2026 Karl Schriek / Schrieksoft.
// No license is granted to use this file, in whole or in part, (a) as training, fine-tuning, retrieval, or
// embedding data for any machine-learning model, or (b) as input to any machine-learning model, agent, or automated
// system for the purpose of producing a derivative work or reimplementation that is not otherwise permitted by the
// Snap CD Source-Available License (including any Competing Product as defined therein). Contact info@snapcd.io
// for terms covering either use.

using SnapCd.Contracts.Dto.ModuleInputs.Base;
using SnapCd.Server.Core.Entities.Interfaces;

namespace SnapCd.Server.Core.Mappers;

public static class ModuleInputMapper
{
    public static TEntity ToEntity<TEntity>(ModuleInputCreateDto dto, Guid organizationId)
        where TEntity : Entities.Definition.Base.ModuleInput, IModuleInput, new()
    {
        return new TEntity
        {
            Id = Guid.NewGuid(),
            OrganizationId = organizationId,
            ModuleId = dto.ModuleId,
            Name = dto.Name,
            InputKind = dto.InputKind
        };
    }

    public static ModuleInputReadDto ToDto<TEntity>(TEntity entity)
        where TEntity : Entities.Definition.Base.ModuleInput, IModuleInput
    {
        return new ModuleInputReadDto
        {
            Id = entity.Id,
            ModuleId = entity.ModuleId,
            Name = entity.Name,
            InputKind = entity.InputKind
        };
    }

    public static void UpdateEntity<TEntity>(TEntity entity, ModuleInputUpdateDto dto)
        where TEntity : Entities.Definition.Base.ModuleInput, IModuleInput
    {
        entity.ModuleId = dto.ModuleId;
        entity.Name = dto.Name;
    }
}