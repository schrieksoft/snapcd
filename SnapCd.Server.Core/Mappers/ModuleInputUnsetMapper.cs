// SPDX-License-Identifier: LicenseRef-Snap-CD-Source-Available-1.1
// Copyright (c) 2026 Karl Schriek / Schrieksoft.
// No license is granted to use this file, in whole or in part, (a) as training, fine-tuning, retrieval, or
// embedding data for any machine-learning model, or (b) as input to any machine-learning model, agent, or automated
// system for the purpose of producing a derivative work or reimplementation that is not otherwise permitted by the
// Snap CD Source-Available License (including any Competing Product as defined therein). Contact info@snapcd.io
// for terms covering either use.

using SnapCd.Contracts.Dto.ModuleInputs;
using SnapCd.Server.Core.Entities.Interfaces;
using SnapCd.Server.Core.Views.Metadata;

namespace SnapCd.Server.Core.Mappers;

public static class ModuleInputUnsetMapper
{
    public static TEntity ToEntity<TEntity>(ModuleInputUnsetCreateDto dto, Guid organizationId)
        where TEntity : Entities.Definition.Base.ModuleInput, IModuleInputUnset, new()
    {
        return new TEntity
        {
            Id = Guid.NewGuid(),
            OrganizationId = organizationId,
            ModuleId = dto.ModuleId,
            Name = dto.Name,
        };
    }

    public static ModuleInputUnsetReadDto ToDto<TEntity>(TEntity entity)
        where TEntity : Entities.Definition.Base.ModuleInput, IModuleInputUnset
    {
        return new ModuleInputUnsetReadDto
        {
            Id = entity.Id,
            ModuleId = entity.ModuleId,
            Name = entity.Name,
            InputKind = entity.InputKind,
        };
    }

    public static void UpdateEntity<TEntity>(TEntity entity, ModuleInputUnsetUpdateDto dto)
        where TEntity : Entities.Definition.Base.ModuleInput, IModuleInputUnset
    {
        entity.ModuleId = dto.ModuleId;
        entity.Name = dto.Name;
    }

    public static ModuleInputUnsetMetadataReadDto ToMetadataDto(ModuleInputUnsetMetadata view)
    {
        return new ModuleInputUnsetMetadataReadDto
        {
            Id = view.Id,
            Name = view.Name,
            ModuleId = view.ModuleId,
        };
    }
}