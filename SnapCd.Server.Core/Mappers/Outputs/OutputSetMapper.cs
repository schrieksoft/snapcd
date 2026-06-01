// SPDX-License-Identifier: LicenseRef-Snap-CD-Source-Available-1.1
// Copyright (c) 2026 Karl Schriek / Schrieksoft.
// No license is granted to use this file, in whole or in part, (a) as training, fine-tuning, retrieval, or
// embedding data for any machine-learning model, or (b) as input to any machine-learning model, agent, or automated
// system for the purpose of producing a derivative work or reimplementation that is not otherwise permitted by the
// Snap CD Source-Available License (including any Competing Product as defined therein). Contact info@snapcd.io
// for terms covering either use.

using MassTransit;
using SnapCd.Contracts.Dto.Outputs;
using SnapCd.Contracts.Dto.OutputSets;
using SnapCd.Server.Core.Entities.Definition;

namespace SnapCd.Server.Core.Mappers.Outputs;

public static class OutputSetMapper
{
    public static OutputSet ToEntity(OutputSetCreateDto dto, Guid organizationId)
    {

        throw new NotImplementedByDesignException();
    }

    public static OutputSetReadDto ToDto(OutputSet entity)
    {
        var outputs = new List<OutputReadDto>();

        foreach (var output in entity.Outputs)
        {
            outputs.Add(OutputMapper.ToDto(output));
        }
        
        return new OutputSetReadDto
        {
            Id = entity.Id,
            ModuleId = entity.ModuleId,
            Timestamp = entity.Timestamp,
            Checksum = entity.Checksum,
            Outputs =  outputs
        };
    }

    public static void UpdateEntity(OutputSet entity, OutputSetUpdateDto dto)
    {

        throw new NotImplementedByDesignException();
    }
}