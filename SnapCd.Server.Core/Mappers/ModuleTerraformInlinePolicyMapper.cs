// SPDX-License-Identifier: LicenseRef-Snap-CD-Source-Available-1.1
// Copyright (c) 2026 Karl Schriek / Schrieksoft.
// No license is granted to use this file, in whole or in part, (a) as training, fine-tuning, retrieval, or
// embedding data for any machine-learning model, or (b) as input to any machine-learning model, agent, or automated
// system for the purpose of producing a derivative work or reimplementation that is not otherwise permitted by the
// Snap CD Source-Available License (including any Competing Product as defined therein). Contact info@snapcd.io
// for terms covering either use.

using SnapCd.Contracts.Dto.ModuleTerraformInlinePolicies;
using SnapCd.Server.Core.Entities.Definition;

namespace SnapCd.Server.Core.Mappers;

public static class ModuleTerraformInlinePolicyMapper
{
    public static ModuleTerraformInlinePolicy ToEntity(ModuleTerraformInlinePolicyCreateDto dto, Guid organizationId)
    {
        return new ModuleTerraformInlinePolicy
        {
            Id = Guid.NewGuid(),
            OrganizationId = organizationId,
            ModuleId = dto.ModuleId,
            Name = dto.Name,
            PolicyContent = dto.PolicyContent,
            Enabled = dto.Enabled,
            EvaluateOn = dto.EvaluateOn
        };
    }

    public static ModuleTerraformInlinePolicyReadDto ToDto(ModuleTerraformInlinePolicy entity)
    {
        return new ModuleTerraformInlinePolicyReadDto
        {
            Id = entity.Id,
            ModuleId = entity.ModuleId,
            Name = entity.Name,
            PolicyContent = entity.PolicyContent,
            Enabled = entity.Enabled,
            EvaluateOn = entity.EvaluateOn
        };
    }

    public static void UpdateEntity(ModuleTerraformInlinePolicy entity, ModuleTerraformInlinePolicyUpdateDto dto)
    {
        entity.ModuleId = dto.ModuleId;
        entity.Name = dto.Name;
        entity.PolicyContent = dto.PolicyContent;
        entity.Enabled = dto.Enabled;
        entity.EvaluateOn = dto.EvaluateOn;
    }
}
