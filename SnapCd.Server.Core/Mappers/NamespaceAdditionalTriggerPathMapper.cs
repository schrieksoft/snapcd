// SPDX-License-Identifier: LicenseRef-Snap-CD-Source-Available-1.1
// Copyright (c) 2026 Karl Schriek / Schrieksoft.
// No license is granted to use this file, in whole or in part, (a) as training, fine-tuning, retrieval, or
// embedding data for any machine-learning model, or (b) as input to any machine-learning model, agent, or automated
// system for the purpose of producing a derivative work or reimplementation that is not otherwise permitted by the
// Snap CD Source-Available License (including any Competing Product as defined therein). Contact info@snapcd.io
// for terms covering either use.

using SnapCd.Contracts.Dto.NamespaceAdditionalTriggerPaths;
using SnapCd.Server.Core.Entities.Definition;

namespace SnapCd.Server.Core.Mappers;

public static class NamespaceAdditionalTriggerPathMapper
{
    public static NamespaceAdditionalTriggerPath ToEntity(NamespaceAdditionalTriggerPathCreateDto dto, Guid organizationId)
    {
        return new NamespaceAdditionalTriggerPath
        {
            Id = Guid.NewGuid(),
            OrganizationId = organizationId,
            NamespaceId = dto.NamespaceId,
            Path = dto.Path
        };
    }

    public static NamespaceAdditionalTriggerPathReadDto ToDto(NamespaceAdditionalTriggerPath entity)
    {
        return new NamespaceAdditionalTriggerPathReadDto
        {
            Id = entity.Id,
            NamespaceId = entity.NamespaceId,
            Path = entity.Path
        };
    }

    public static void UpdateEntity(NamespaceAdditionalTriggerPath entity, NamespaceAdditionalTriggerPathUpdateDto dto)
    {
        entity.NamespaceId = dto.NamespaceId;
        entity.Path = dto.Path;
    }
}
