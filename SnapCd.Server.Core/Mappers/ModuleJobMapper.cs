// SPDX-License-Identifier: LicenseRef-Snap-CD-Source-Available-1.1
// Copyright (c) 2026 Karl Schriek / Schrieksoft.
// No license is granted to use this file, in whole or in part, (a) as training, fine-tuning, retrieval, or
// embedding data for any machine-learning model, or (b) as input to any machine-learning model, agent, or automated
// system for the purpose of producing a derivative work or reimplementation that is not otherwise permitted by the
// Snap CD Source-Available License (including any Competing Product as defined therein). Contact info@snapcd.io
// for terms covering either use.

using SnapCd.Server.Core.Dtos.ModuleJobs;
using SnapCd.Server.Core.Entities.Definition;

namespace SnapCd.Server.Core.Mappers;

public static class ModuleJobMapper
{
    public static ModuleJobReadDto ToDto(ModuleJob entity)
    {
        return new ModuleJobReadDto
        {
            Id = entity.Id,
            ModuleId = entity.ModuleId,
            JobNumber = entity.JobNumber,
            TimestampStart = entity.TimestampStart,
            TimestampEnd = entity.TimestampEnd,
            Status = entity.Status,
            JobType = entity.JobType,
            WaitingForApproval = entity.WaitingForApproval,
            IsCurrent = entity.IsCurrent,
            DefinitiveRevision = entity.DefinitiveRevision,
            ActualStateHeadline = entity.ActualStateHeadline
        };
    }
}