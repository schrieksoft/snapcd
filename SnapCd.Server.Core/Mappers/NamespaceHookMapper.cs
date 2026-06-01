// SPDX-License-Identifier: LicenseRef-Snap-CD-Source-Available-1.1
// Copyright (c) 2026 Karl Schriek / Schrieksoft.
// No license is granted to use this file, in whole or in part, (a) as training, fine-tuning, retrieval, or
// embedding data for any machine-learning model, or (b) as input to any machine-learning model, agent, or automated
// system for the purpose of producing a derivative work or reimplementation that is not otherwise permitted by the
// Snap CD Source-Available License (including any Competing Product as defined therein). Contact info@snapcd.io
// for terms covering either use.

using SnapCd.Contracts.Dto.NamespaceHooks;
using SnapCd.Server.Core.Entities.Definition;

namespace SnapCd.Server.Core.Mappers;

public static class NamespaceHookMapper
{
    public static NamespaceHook ToEntity(NamespaceHookCreateDto dto, Guid organizationId)
    {
        return new NamespaceHook
        {
            Id = Guid.NewGuid(),
            OrganizationId = organizationId,
            Task = dto.Task,
            Phase = dto.Phase,
            Script = dto.Script,
            NamespaceId = dto.NamespaceId
        };
    }

    public static NamespaceHookReadDto ToDto(NamespaceHook entity)
    {
        return new NamespaceHookReadDto
        {
            Id = entity.Id,
            Task = entity.Task,
            Phase = entity.Phase,
            Script = entity.Script,
            NamespaceId = entity.NamespaceId
        };
    }

    public static void UpdateEntity(NamespaceHook entity, NamespaceHookUpdateDto dto)
    {
        entity.Task = dto.Task;
        entity.Phase = dto.Phase;
        entity.Script = dto.Script;
        entity.NamespaceId = dto.NamespaceId;
    }
}
