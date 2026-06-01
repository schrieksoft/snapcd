// SPDX-License-Identifier: LicenseRef-Snap-CD-Source-Available-1.1
// Copyright (c) 2026 Karl Schriek / Schrieksoft.
// No license is granted to use this file, in whole or in part, (a) as training, fine-tuning, retrieval, or
// embedding data for any machine-learning model, or (b) as input to any machine-learning model, agent, or automated
// system for the purpose of producing a derivative work or reimplementation that is not otherwise permitted by the
// Snap CD Source-Available License (including any Competing Product as defined therein). Contact info@snapcd.io
// for terms covering either use.

using SnapCd.Contracts.Dto.SourceRefresherPreselections;
using SnapCd.Server.Core.Entities.Definition;

namespace SnapCd.Server.Core.Mappers;

public static class SourceRefresherPreselectionMapper
{
    public static SourceRefresherPreselection ToEntity(SourceRefresherPreselectionCreateDto dto, Guid organizationId)
    {
        return new SourceRefresherPreselection
        {
            Id = Guid.NewGuid(),
            OrganizationId = organizationId,
            RunnerId = dto.RunnerId,
            RunnerInstanceName = dto.RunnerInstanceName,
            SourceUrl = dto.SourceUrl
        };
    }

    public static SourceRefresherPreselectionReadDto ToDto(SourceRefresherPreselection entity)
    {
        return new SourceRefresherPreselectionReadDto
        {
            Id = entity.Id,
            RunnerId = entity.RunnerId,
            RunnerInstanceName = entity.RunnerInstanceName,
            SourceUrl = entity.SourceUrl
        };
    }

    public static void UpdateEntity(SourceRefresherPreselection entity, SourceRefresherPreselectionUpdateDto dto)
    {
        entity.RunnerId = dto.RunnerId;
        entity.RunnerInstanceName = dto.RunnerInstanceName;
        entity.SourceUrl = dto.SourceUrl;
    }
}