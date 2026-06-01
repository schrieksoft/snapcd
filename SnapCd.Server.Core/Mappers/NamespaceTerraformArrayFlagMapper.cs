// SPDX-License-Identifier: LicenseRef-Snap-CD-Source-Available-1.1
// Copyright (c) 2026 Karl Schriek / Schrieksoft.
// No license is granted to use this file, in whole or in part, (a) as training, fine-tuning, retrieval, or
// embedding data for any machine-learning model, or (b) as input to any machine-learning model, agent, or automated
// system for the purpose of producing a derivative work or reimplementation that is not otherwise permitted by the
// Snap CD Source-Available License (including any Competing Product as defined therein). Contact info@snapcd.io
// for terms covering either use.

using SnapCd.Contracts.Dto.NamespaceTerraformArrayFlags;
using SnapCd.Server.Core.Entities.Definition;

namespace SnapCd.Server.Core.Mappers;

public static class NamespaceTerraformArrayFlagMapper
{
    public static NamespaceTerraformArrayFlag ToEntity(NamespaceTerraformArrayFlagCreateDto dto, Guid organizationId)
    {
        return new NamespaceTerraformArrayFlag
        {
            Id = Guid.NewGuid(),
            OrganizationId = organizationId,
            Task = dto.Task,
            Flag = dto.Flag,
            Value = dto.Value,
            NamespaceId = dto.NamespaceId
        };
    }

    public static NamespaceTerraformArrayFlagReadDto ToDto(NamespaceTerraformArrayFlag entity)
    {
        return new NamespaceTerraformArrayFlagReadDto
        {
            Id = entity.Id,
            Task = entity.Task,
            Flag = entity.Flag,
            Value = entity.Value,
            NamespaceId = entity.NamespaceId
        };
    }

    public static void UpdateEntity(NamespaceTerraformArrayFlag entity, NamespaceTerraformArrayFlagUpdateDto dto)
    {
        entity.Task = dto.Task;
        entity.Flag = dto.Flag;
        entity.Value = dto.Value;
        entity.NamespaceId = dto.NamespaceId;
    }
}
