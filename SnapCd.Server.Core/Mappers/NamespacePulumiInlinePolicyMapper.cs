// SPDX-License-Identifier: LicenseRef-Snap-CD-Source-Available-1.1
// Copyright (c) 2026 Karl Schriek / Schrieksoft.
// No license is granted to use this file, in whole or in part, (a) as training, fine-tuning, retrieval, or
// embedding data for any machine-learning model, or (b) as input to any machine-learning model, agent, or automated
// system for the purpose of producing a derivative work or reimplementation that is not otherwise permitted by the
// Snap CD Source-Available License (including any Competing Product as defined therein). Contact info@snapcd.io
// for terms covering either use.

using SnapCd.Contracts.Dto.NamespacePulumiInlinePolicies;
using SnapCd.Server.Core.Entities.Definition;

namespace SnapCd.Server.Core.Mappers;

public static class NamespacePulumiInlinePolicyMapper
{
    public static NamespacePulumiInlinePolicy ToEntity(NamespacePulumiInlinePolicyCreateDto dto, Guid organizationId)
    {
        return new NamespacePulumiInlinePolicy
        {
            Id = Guid.NewGuid(),
            OrganizationId = organizationId,
            NamespaceId = dto.NamespaceId,
            Name = dto.Name,
            PolicyContent = dto.PolicyContent,
            Runtime = dto.Runtime,
            AdditionalDependencies = dto.AdditionalDependencies,
            Enabled = dto.Enabled,
            EvaluateOn = dto.EvaluateOn
        };
    }

    public static NamespacePulumiInlinePolicyReadDto ToDto(NamespacePulumiInlinePolicy entity)
    {
        return new NamespacePulumiInlinePolicyReadDto
        {
            Id = entity.Id,
            NamespaceId = entity.NamespaceId,
            Name = entity.Name,
            PolicyContent = entity.PolicyContent,
            Runtime = entity.Runtime,
            AdditionalDependencies = entity.AdditionalDependencies,
            Enabled = entity.Enabled,
            EvaluateOn = entity.EvaluateOn
        };
    }

    public static void UpdateEntity(NamespacePulumiInlinePolicy entity, NamespacePulumiInlinePolicyUpdateDto dto)
    {
        entity.NamespaceId = dto.NamespaceId;
        entity.Name = dto.Name;
        entity.PolicyContent = dto.PolicyContent;
        entity.Runtime = dto.Runtime;
        entity.AdditionalDependencies = dto.AdditionalDependencies;
        entity.Enabled = dto.Enabled;
        entity.EvaluateOn = dto.EvaluateOn;
    }
}
