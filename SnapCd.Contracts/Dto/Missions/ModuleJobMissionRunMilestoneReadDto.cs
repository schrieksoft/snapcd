// SPDX-License-Identifier: LicenseRef-Snap-CD-Source-Available-1.1
// Copyright (c) 2026 Karl Schriek / Schrieksoft.
// No license is granted to use this file, in whole or in part, (a) as training, fine-tuning, retrieval, or
// embedding data for any machine-learning model, or (b) as input to any machine-learning model, agent, or automated
// system for the purpose of producing a derivative work or reimplementation that is not otherwise permitted by the
// Snap CD Source-Available License (including any Competing Product as defined therein). Contact info@snapcd.io
// for terms covering either use.

using SnapCd.Contracts.Interfaces;

namespace SnapCd.Contracts.Dto.Missions;

/// <summary>Read projection of a <c>ModuleJobMissionRunMilestone</c> — one progress checkpoint on a mission run's timeline.</summary>
public class ModuleJobMissionRunMilestoneReadDto : IDto
{
    /// <summary>Unique ID of the milestone.</summary>
    public Guid Id { get; set; }
    /// <summary>ID of the mission run the milestone belongs to.</summary>
    public Guid ModuleJobMissionRunId { get; set; }
    /// <summary>Milestone kind reported by the agent (e.g. started, diagnosis, result).</summary>
    public string? Kind { get; set; }
    /// <summary>Human-readable progress message.</summary>
    public string Message { get; set; } = null!;
    /// <summary>When the milestone was reported (UTC).</summary>
    public DateTime ReportedAt { get; set; }
}
