// SPDX-License-Identifier: LicenseRef-Snap-CD-Source-Available-1.1
// Copyright (c) 2026 Karl Schriek / Schrieksoft.
// No license is granted to use this file, in whole or in part, (a) as training, fine-tuning, retrieval, or
// embedding data for any machine-learning model, or (b) as input to any machine-learning model, agent, or automated
// system for the purpose of producing a derivative work or reimplementation that is not otherwise permitted by the
// Snap CD Source-Available License (including any Competing Product as defined therein). Contact info@snapcd.io
// for terms covering either use.

using SnapCd.Contracts.Dto.ModuleTerraformRemotePolicies;
using SnapCd.Server.Core.Entities.Definition;

namespace SnapCd.Server.Core.Mappers;

public static class ModuleTerraformRemotePolicyMapper
{
    public static ModuleTerraformRemotePolicy ToEntity(ModuleTerraformRemotePolicyCreateDto dto, Guid organizationId)
    {
        return new ModuleTerraformRemotePolicy
        {
            Id = Guid.NewGuid(),
            OrganizationId = organizationId,
            ModuleId = dto.ModuleId,
            Name = dto.Name,
            RepoUrl = dto.RepoUrl,
            Revision = dto.Revision,
            Path = dto.Path,
            Enabled = dto.Enabled,
            EvaluateOn = dto.EvaluateOn
        };
    }

    public static ModuleTerraformRemotePolicyReadDto ToDto(ModuleTerraformRemotePolicy entity)
    {
        return new ModuleTerraformRemotePolicyReadDto
        {
            Id = entity.Id,
            ModuleId = entity.ModuleId,
            Name = entity.Name,
            RepoUrl = entity.RepoUrl,
            Revision = entity.Revision,
            Path = entity.Path,
            Enabled = entity.Enabled,
            EvaluateOn = entity.EvaluateOn
        };
    }

    public static void UpdateEntity(ModuleTerraformRemotePolicy entity, ModuleTerraformRemotePolicyUpdateDto dto)
    {
        entity.ModuleId = dto.ModuleId;
        entity.Name = dto.Name;
        entity.RepoUrl = dto.RepoUrl;
        entity.Revision = dto.Revision;
        entity.Path = dto.Path;
        entity.Enabled = dto.Enabled;
        entity.EvaluateOn = dto.EvaluateOn;
    }
}
