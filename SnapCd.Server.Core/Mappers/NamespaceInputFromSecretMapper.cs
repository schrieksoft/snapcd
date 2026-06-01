// SPDX-License-Identifier: LicenseRef-Snap-CD-Source-Available-1.1
// Copyright (c) 2026 Karl Schriek / Schrieksoft.
// No license is granted to use this file, in whole or in part, (a) as training, fine-tuning, retrieval, or
// embedding data for any machine-learning model, or (b) as input to any machine-learning model, agent, or automated
// system for the purpose of producing a derivative work or reimplementation that is not otherwise permitted by the
// Snap CD Source-Available License (including any Competing Product as defined therein). Contact info@snapcd.io
// for terms covering either use.

using SnapCd.Contracts.Dto.NamespaceInputs;
using SnapCd.Server.Core.Entities.Definition.Base;
using SnapCd.Server.Core.Entities.Interfaces;

namespace SnapCd.Server.Core.Mappers;

public static class NamespaceInputFromSecretMapper
{
    public static TEntity ToEntity<TEntity>(NamespaceInputFromSecretCreateDto dto, Guid organizationId)
        where TEntity : NamespaceInputWithType, INamespaceInputFromSecret, new()
    {
        return new TEntity
        {
            Id = Guid.NewGuid(),
            OrganizationId = organizationId,
            NamespaceId = dto.NamespaceId,
            Name = dto.Name,
            UsageMode = dto.UsageMode,
            Type = dto.Type,
            SecretId = dto.SecretId
        };
    }

    public static NamespaceInputFromSecretReadDto ToDto<TEntity>(TEntity entity)
        where TEntity : NamespaceInputWithType, INamespaceInputFromSecret
    {
        return new NamespaceInputFromSecretReadDto
        {
            Id = entity.Id,
            NamespaceId = entity.NamespaceId,
            Name = entity.Name,
            UsageMode = entity.UsageMode,
            InputKind = entity.InputKind,
            Type = entity.Type,
            SecretId = entity.SecretId
        };
    }

    public static void UpdateEntity<TEntity>(TEntity entity, NamespaceInputFromSecretUpdateDto dto)
        where TEntity : NamespaceInputWithType, INamespaceInputFromSecret
    {
        entity.NamespaceId = dto.NamespaceId;
        entity.Name = dto.Name;
        entity.UsageMode = dto.UsageMode;
        entity.Type = dto.Type;
        entity.SecretId = dto.SecretId;
    }
}