// SPDX-License-Identifier: LicenseRef-Snap-CD-Source-Available-1.1
// Copyright (c) 2026 Karl Schriek / Schrieksoft.
// No license is granted to use this file, in whole or in part, (a) as training, fine-tuning, retrieval, or
// embedding data for any machine-learning model, or (b) as input to any machine-learning model, agent, or automated
// system for the purpose of producing a derivative work or reimplementation that is not otherwise permitted by the
// Snap CD Source-Available License (including any Competing Product as defined therein). Contact info@snapcd.io
// for terms covering either use.

using SnapCd.Contracts.Dto.Missions;

namespace SnapCd.Contracts.Dto.Modules;

/// <summary>
/// One past mission for a Module — a row of the module's mission history. Carries the outcome
/// (<c>Status</c>, <c>DiagnosisCategory</c>, <c>ResultSummary</c> — which contains that run's facts
/// block including any PR url), the commit the job ran against (<c>DefinitiveRevision</c>) and its
/// <c>JobType</c>, timestamps, and the milestone timeline.
/// </summary>
public class ModuleMissionHistoryEntryDto
{
    /// <summary>ID of the mission run.</summary>
    public Guid RunId { get; init; }
    /// <summary>ID of the Job the mission ran against.</summary>
    public Guid ModuleJobId { get; init; }
    /// <summary>Type of the underlying Job: Apply or Destroy.</summary>
    public string JobType { get; init; } = string.Empty;
    public MissionType MissionType { get; init; }
    public MissionStatus Status { get; init; }
    /// <summary>What the mission concluded about the run, when it produced a diagnosis.</summary>
    public DiagnosisCategory? DiagnosisCategory { get; init; }
    /// <summary>The mission's result summary — contains that run's facts block, including any PR url.</summary>
    public string? ResultSummary { get; init; }
    /// <summary>The resolved commit the underlying Job ran against.</summary>
    public string? DefinitiveRevision { get; init; }
    /// <summary>When the mission run started (UTC).</summary>
    public DateTime? StartedAt { get; init; }
    /// <summary>When the mission run completed (UTC), if finished.</summary>
    public DateTime? CompletedAt { get; init; }
    /// <summary>The run's milestone timeline, oldest first.</summary>
    public List<ModuleJobMissionRunMilestoneReadDto> Milestones { get; set; } = new();
}
