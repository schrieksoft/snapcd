// SPDX-License-Identifier: LicenseRef-Snap-CD-Source-Available-1.1
// Copyright (c) 2026 Karl Schriek / Schrieksoft.
// No license is granted to use this file, in whole or in part, (a) as training, fine-tuning, retrieval, or
// embedding data for any machine-learning model, or (b) as input to any machine-learning model, agent, or automated
// system for the purpose of producing a derivative work or reimplementation that is not otherwise permitted by the
// Snap CD Source-Available License (including any Competing Product as defined therein). Contact info@snapcd.io
// for terms covering either use.

using SnapCd.Contracts.Dto.Stacks;
using SnapCd.Server.Core.Entities.Definition;

namespace SnapCd.Server.Core.Mappers;

public static class StackMapper
{
    public static Stack ToEntity(StackCreateDto dto, Guid organizationId)
    {
        return new Stack
        {
            Id = Guid.NewGuid(),
            OrganizationId = organizationId,
            Name = dto.Name,
            TriggerBehaviourOnModified = dto.TriggerBehaviourOnModified
        };
    }

    public static StackReadDto ToDto(Stack entity)
    {
        return new StackReadDto
        {
            Id = entity.Id,
            Name = entity.Name,
            TriggerBehaviourOnModified = entity.TriggerBehaviourOnModified
        };
    }

    public static void UpdateEntity(Stack entity, StackUpdateDto dto)
    {
        entity.Name = dto.Name;
        entity.TriggerBehaviourOnModified = dto.TriggerBehaviourOnModified;
    }
}