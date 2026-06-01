// SPDX-License-Identifier: LicenseRef-Snap-CD-Source-Available-1.1
// Copyright (c) 2026 Karl Schriek / Schrieksoft.
// No license is granted to use this file, in whole or in part, (a) as training, fine-tuning, retrieval, or
// embedding data for any machine-learning model, or (b) as input to any machine-learning model, agent, or automated
// system for the purpose of producing a derivative work or reimplementation that is not otherwise permitted by the
// Snap CD Source-Available License (including any Competing Product as defined therein). Contact info@snapcd.io
// for terms covering either use.

using MassTransit;
using SnapCd.Contracts.Dto.Variables;
using SnapCd.Server.Core.Entities.Definition;

namespace SnapCd.Server.Core.Mappers;

public static class VariableMapper
{
    public static Variable ToEntity(VariableCreateDto dto, Guid organizationId)
    {

        throw new NotImplementedByDesignException();
    }

    public static VariableReadDto ToDto(Variable entity)
    {
        return new VariableReadDto
        {
            Id = entity.Id,
            VariableSetId = entity.VariableSetId,
            Name = entity.Name,
            Type = entity.Type,
            Description = entity.Description,
            Sensitive = entity.Sensitive,
            Nullable = entity.Nullable,
            FromExtraFile = entity.FromExtraFile
        };
    }

    public static void UpdateEntity(Variable entity, VariableUpdateDto dto)
    {

        throw new NotImplementedByDesignException();
    }
}