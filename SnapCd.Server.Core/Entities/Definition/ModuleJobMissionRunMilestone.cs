// SPDX-License-Identifier: LicenseRef-Snap-CD-Source-Available-1.1
// Copyright (c) 2026 Karl Schriek / Schrieksoft.
// No license is granted to use this file, in whole or in part, (a) as training, fine-tuning, retrieval, or
// embedding data for any machine-learning model, or (b) as input to any machine-learning model, agent, or automated
// system for the purpose of producing a derivative work or reimplementation that is not otherwise permitted by the
// Snap CD Source-Available License (including any Competing Product as defined therein). Contact info@snapcd.io
// for terms covering either use.

using System.Text.Json.Serialization;
using SnapCd.Server.Core.Entities.Definition.Base;
using SnapCd.Server.Core.Entities.Interfaces;

namespace SnapCd.Server.Core.Entities.Definition;

/// <summary>
/// One curated progress checkpoint a mission run reports while it executes (via the agent's
/// <c>report_milestone</c> MCP tool) — e.g. "investigating", "diagnosed", "opened PR". A child of
/// <see cref="ModuleJobMissionRun"/> (a milestone belongs to a specific execution attempt); everything
/// else — job, mission type, module — is reached through the run, so nothing is denormalised here.
/// Distinct from the run's raw <see cref="ModuleJobMissionRun.Logs"/>: milestones form the human-facing
/// timeline on the job/mission and are fanned out as <c>MissionMilestoneReported</c> domain events.
/// Append-only.
/// </summary>
public class ModuleJobMissionRunMilestone : AuditBase, IEntity, ICreationTrackable, IOrganizationChild
{
    public Guid Id { get; set; }
    public Guid OrganizationId { get; set; }

    /// <summary>The run this milestone belongs to (the sole parent reference).</summary>
    public Guid ModuleJobMissionRunId { get; set; }

    /// <summary>Optional short label for the checkpoint (e.g. investigating / diagnosed / pr_opened).</summary>
    public string? Kind { get; set; }

    public string Message { get; set; } = null!;

    /// <summary>When the agent reported the milestone (carried from the orchestrator).</summary>
    public DateTime ReportedAt { get; set; }

    [JsonIgnore] public virtual Organization Organization { get; set; } = null!;

    public Guid ParentId()
    {
        return ModuleJobMissionRunId;
    }
}
