// SPDX-License-Identifier: LicenseRef-Snap-CD-Source-Available-1.1
// Copyright (c) 2026 Karl Schriek / Schrieksoft.
// No license is granted to use this file, in whole or in part, (a) as training, fine-tuning, retrieval, or
// embedding data for any machine-learning model, or (b) as input to any machine-learning model, agent, or automated
// system for the purpose of producing a derivative work or reimplementation that is not otherwise permitted by the
// Snap CD Source-Available License (including any Competing Product as defined therein). Contact info@snapcd.io
// for terms covering either use.

using SnapCd.Contracts.Dto.RunnerStackAssignments;
using SnapCd.Server.Core.Entities.Definition.RunnerAssignments;

namespace SnapCd.Server.Core.Mappers;

public static class RunnerStackAssignmentMapper
{
    public static RunnerStackAssignment ToEntity(RunnerStackAssignmentCreateDto dto, Guid organizationId)
    {
        return new RunnerStackAssignment
        {
            Id = Guid.NewGuid(),
            OrganizationId = organizationId,
            StackId = dto.StackId,
            RunnerId = dto.RunnerId
        };
    }

    public static RunnerStackAssignmentReadDto ToDto(RunnerStackAssignment entity)
    {
        return new RunnerStackAssignmentReadDto
        {
            Id = entity.Id,
            StackId = entity.StackId,
            RunnerId = entity.RunnerId
        };
    }

    public static void UpdateEntity(RunnerStackAssignment entity, RunnerStackAssignmentUpdateDto dto)
    {
        entity.StackId = dto.StackId;
        entity.RunnerId = dto.RunnerId;
    }
}