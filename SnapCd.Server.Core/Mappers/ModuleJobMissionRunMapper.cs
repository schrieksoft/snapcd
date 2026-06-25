// SPDX-License-Identifier: LicenseRef-Snap-CD-Source-Available-1.1
// Copyright (c) 2026 Karl Schriek / Schrieksoft.
// No license is granted to use this file, in whole or in part, (a) as training, fine-tuning, retrieval, or
// embedding data for any machine-learning model, or (b) as input to any machine-learning model, agent, or automated
// system for the purpose of producing a derivative work or reimplementation that is not otherwise permitted by the
// Snap CD Source-Available License (including any Competing Product as defined therein). Contact info@snapcd.io
// for terms covering either use.

using SnapCd.Contracts.Dto.Missions;
using SnapCd.Server.Core.Entities.Definition;

namespace SnapCd.Server.Core.Mappers;

public static class ModuleJobMissionRunMapper
{
    public static ModuleJobMissionRunReadDto ToDto(ModuleJobMissionRun entity)
    {
        return new ModuleJobMissionRunReadDto
        {
            Id = entity.Id,
            ModuleJobMissionId = entity.ModuleJobMissionId,
            ModuleJobId = entity.ModuleJobId,
            MissionType = entity.MissionType,
            AgentId = entity.AgentId,
            InvocationId = entity.InvocationId,
            AttemptNumber = entity.AttemptNumber,
            Status = entity.Status,
            StartedAt = entity.StartedAt,
            CompletedAt = entity.CompletedAt,
            ResultSummary = entity.ResultSummary,
            Error = entity.Error,
            ToolCallsJson = entity.ToolCallsJson,
            TokensJson = entity.TokensJson,
            DurationSeconds = entity.DurationSeconds,
            DiagnosisCategory = entity.DiagnosisCategory
        };
    }
}
