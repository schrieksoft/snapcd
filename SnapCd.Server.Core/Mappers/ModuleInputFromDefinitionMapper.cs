// SPDX-License-Identifier: LicenseRef-Snap-CD-Source-Available-1.1
// Copyright (c) 2026 Karl Schriek / Schrieksoft.
// No license is granted to use this file, in whole or in part, (a) as training, fine-tuning, retrieval, or
// embedding data for any machine-learning model, or (b) as input to any machine-learning model, agent, or automated
// system for the purpose of producing a derivative work or reimplementation that is not otherwise permitted by the
// Snap CD Source-Available License (including any Competing Product as defined therein). Contact info@snapcd.io
// for terms covering either use.

using SnapCd.Contracts.Dto.ModuleInputs;
using SnapCd.Server.Core.Entities.Interfaces;

namespace SnapCd.Server.Core.Mappers;

public static class ModuleInputFromDefinitionMapper
{
    public static TEntity ToEntity<TEntity>(ModuleInputFromDefinitionCreateDto dto, Guid organizationId)
        where TEntity : Entities.Definition.Base.ModuleInput, IModuleInputFromDefinition, new()
    {
        return new TEntity
        {
            Id = Guid.NewGuid(),
            OrganizationId = organizationId,
            ModuleId = dto.ModuleId,
            Name = dto.Name,
            DefinitionName = dto.DefinitionName
        };
    }

    public static ModuleInputFromDefinitionReadDto ToDto<TEntity>(TEntity entity)
        where TEntity : Entities.Definition.Base.ModuleInput, IModuleInputFromDefinition
    {
        return new ModuleInputFromDefinitionReadDto
        {
            Id = entity.Id,
            ModuleId = entity.ModuleId,
            Name = entity.Name,
            InputKind = entity.InputKind,
            DefinitionName = entity.DefinitionName
        };
    }

    public static void UpdateEntity<TEntity>(TEntity entity, ModuleInputFromDefinitionUpdateDto dto)
        where TEntity : Entities.Definition.Base.ModuleInput, IModuleInputFromDefinition
    {
        entity.ModuleId = dto.ModuleId;
        entity.Name = dto.Name;
        entity.DefinitionName = dto.DefinitionName;
    }
}