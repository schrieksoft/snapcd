// SPDX-License-Identifier: LicenseRef-Snap-CD-Source-Available-1.1
// Copyright (c) 2026 Karl Schriek / Schrieksoft.
// No license is granted to use this file, in whole or in part, (a) as training, fine-tuning, retrieval, or
// embedding data for any machine-learning model, or (b) as input to any machine-learning model, agent, or automated
// system for the purpose of producing a derivative work or reimplementation that is not otherwise permitted by the
// Snap CD Source-Available License (including any Competing Product as defined therein). Contact info@snapcd.io
// for terms covering either use.

using SnapCd.Contracts.Dto.ModulePulumiLocalPolicies;
using SnapCd.Server.Core.Entities.Definition;

namespace SnapCd.Server.Core.Mappers;

public static class ModulePulumiLocalPolicyMapper
{
    public static ModulePulumiLocalPolicy ToEntity(ModulePulumiLocalPolicyCreateDto dto, Guid organizationId)
    {
        return new ModulePulumiLocalPolicy
        {
            Id = Guid.NewGuid(),
            OrganizationId = organizationId,
            ModuleId = dto.ModuleId,
            Name = dto.Name,
            Path = dto.Path,
            Enabled = dto.Enabled,
            EvaluateOn = dto.EvaluateOn
        };
    }

    public static ModulePulumiLocalPolicyReadDto ToDto(ModulePulumiLocalPolicy entity)
    {
        return new ModulePulumiLocalPolicyReadDto
        {
            Id = entity.Id,
            ModuleId = entity.ModuleId,
            Name = entity.Name,
            Path = entity.Path,
            Enabled = entity.Enabled,
            EvaluateOn = entity.EvaluateOn
        };
    }

    public static void UpdateEntity(ModulePulumiLocalPolicy entity, ModulePulumiLocalPolicyUpdateDto dto)
    {
        entity.ModuleId = dto.ModuleId;
        entity.Name = dto.Name;
        entity.Path = dto.Path;
        entity.Enabled = dto.Enabled;
        entity.EvaluateOn = dto.EvaluateOn;
    }
}
