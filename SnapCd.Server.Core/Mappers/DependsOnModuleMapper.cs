// SPDX-License-Identifier: LicenseRef-Snap-CD-Source-Available-1.1
// Copyright (c) 2026 Karl Schriek / Schrieksoft.
// No license is granted to use this file, in whole or in part, (a) as training, fine-tuning, retrieval, or
// embedding data for any machine-learning model, or (b) as input to any machine-learning model, agent, or automated
// system for the purpose of producing a derivative work or reimplementation that is not otherwise permitted by the
// Snap CD Source-Available License (including any Competing Product as defined therein). Contact info@snapcd.io
// for terms covering either use.

using SnapCd.Contracts.Dto.DependsOnModules;
using SnapCd.Server.Core.Entities.Definition;

namespace SnapCd.Server.Core.Mappers;

public static class DependsOnModuleMapper
{
    public static DependsOnModule ToEntity(DependsOnModuleCreateDto dto, Guid organizationId)
    {
        return new DependsOnModule
        {
            Id = Guid.NewGuid(),
            OrganizationId = organizationId,
            ModuleId = dto.ModuleId,
            DependsOnModuleId = dto.DependsOnModuleId
        };
    }

    public static DependsOnModuleReadDto ToDto(DependsOnModule entity)
    {
        return new DependsOnModuleReadDto
        {
            Id = entity.Id,
            ModuleId = entity.ModuleId,
            DependsOnModuleId = entity.DependsOnModuleId
        };
    }

    public static void UpdateEntity(DependsOnModule entity, DependsOnModuleUpdateDto dto)
    {
        entity.ModuleId = dto.ModuleId;
        entity.DependsOnModuleId = dto.DependsOnModuleId;
    }
}