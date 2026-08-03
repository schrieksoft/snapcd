// SPDX-License-Identifier: LicenseRef-Snap-CD-Source-Available-1.1
// Copyright (c) 2026 Karl Schriek / Schrieksoft.
// No license is granted to use this file, in whole or in part, (a) as training, fine-tuning, retrieval, or
// embedding data for any machine-learning model, or (b) as input to any machine-learning model, agent, or automated
// system for the purpose of producing a derivative work or reimplementation that is not otherwise permitted by the
// Snap CD Source-Available License (including any Competing Product as defined therein). Contact info@snapcd.io
// for terms covering either use.

using SnapCd.Contracts.Dto.NamespaceTerraformRemotePolicies;
using SnapCd.Server.Core.Entities.Definition;

namespace SnapCd.Server.Core.Mappers;

public static class NamespaceTerraformRemotePolicyMapper
{
    public static NamespaceTerraformRemotePolicy ToEntity(NamespaceTerraformRemotePolicyCreateDto dto, Guid organizationId)
    {
        return new NamespaceTerraformRemotePolicy
        {
            Id = Guid.NewGuid(),
            OrganizationId = organizationId,
            NamespaceId = dto.NamespaceId,
            Name = dto.Name,
            RepoUrl = dto.RepoUrl,
            Revision = dto.Revision,
            Path = dto.Path,
            Enabled = dto.Enabled,
            EvaluateOn = dto.EvaluateOn
        };
    }

    public static NamespaceTerraformRemotePolicyReadDto ToDto(NamespaceTerraformRemotePolicy entity)
    {
        return new NamespaceTerraformRemotePolicyReadDto
        {
            Id = entity.Id,
            NamespaceId = entity.NamespaceId,
            Name = entity.Name,
            RepoUrl = entity.RepoUrl,
            Revision = entity.Revision,
            Path = entity.Path,
            Enabled = entity.Enabled,
            EvaluateOn = entity.EvaluateOn
        };
    }

    public static void UpdateEntity(NamespaceTerraformRemotePolicy entity, NamespaceTerraformRemotePolicyUpdateDto dto)
    {
        entity.NamespaceId = dto.NamespaceId;
        entity.Name = dto.Name;
        entity.RepoUrl = dto.RepoUrl;
        entity.Revision = dto.Revision;
        entity.Path = dto.Path;
        entity.Enabled = dto.Enabled;
        entity.EvaluateOn = dto.EvaluateOn;
    }
}
