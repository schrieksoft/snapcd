// SPDX-License-Identifier: LicenseRef-Snap-CD-Source-Available-1.1
// Copyright (c) 2026 Karl Schriek / Schrieksoft.
// No license is granted to use this file, in whole or in part, (a) as training, fine-tuning, retrieval, or
// embedding data for any machine-learning model, or (b) as input to any machine-learning model, agent, or automated
// system for the purpose of producing a derivative work or reimplementation that is not otherwise permitted by the
// Snap CD Source-Available License (including any Competing Product as defined therein). Contact info@snapcd.io
// for terms covering either use.

using SnapCd.Contracts.Dto.NamespaceInputs.Base;
using SnapCd.Server.Core.Entities.Interfaces;

namespace SnapCd.Server.Core.Mappers;

public static class NamespaceInputMapper
{
    public static TEntity ToEntity<TEntity>(NamespaceInputCreateDto dto, Guid organizationId)
        where TEntity : Entities.Definition.Base.NamespaceInput, INamespaceInput, new()
    {
        return new TEntity
        {
            Id = Guid.NewGuid(),
            OrganizationId = organizationId,
            NamespaceId = dto.NamespaceId,
            Name = dto.Name,
            UsageMode = dto.UsageMode
        };
    }

    public static NamespaceInputReadDto ToDto<TEntity>(TEntity entity)
        where TEntity : Entities.Definition.Base.NamespaceInput, INamespaceInput
    {
        return new NamespaceInputReadDto
        {
            Id = entity.Id,
            NamespaceId = entity.NamespaceId,
            Name = entity.Name,
            UsageMode = entity.UsageMode,
            InputKind = entity.InputKind
        };
    }

    public static void UpdateEntity<TEntity>(TEntity entity, NamespaceInputUpdateDto dto)
        where TEntity : Entities.Definition.Base.NamespaceInput, INamespaceInput
    {
        entity.NamespaceId = dto.NamespaceId;
        entity.Name = dto.Name;
        entity.UsageMode = dto.UsageMode;
    }
}