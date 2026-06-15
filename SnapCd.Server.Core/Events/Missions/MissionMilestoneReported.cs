// SPDX-License-Identifier: LicenseRef-Snap-CD-Source-Available-1.1
// Copyright (c) 2026 Karl Schriek / Schrieksoft.
// No license is granted to use this file, in whole or in part, (a) as training, fine-tuning, retrieval, or
// embedding data for any machine-learning model, or (b) as input to any machine-learning model, agent, or automated
// system for the purpose of producing a derivative work or reimplementation that is not otherwise permitted by the
// Snap CD Source-Available License (including any Competing Product as defined therein). Contact info@snapcd.io
// for terms covering either use.

using SnapCd.Contracts;

namespace SnapCd.Server.Core.Events.Missions;

/// <summary>
/// Published by <c>AgentHub</c> each time a mission reports a progress milestone. This is the
/// platform's outward-facing "a milestone happened" domain event — the future Integrations feature
/// subscribes to it (as the <c>MissionMilestone</c> trigger) to fan milestones out to Slack and other
/// sinks. Distinct from <see cref="MissionRunModifiedEvent"/>, which is an internal UI-refresh signal.
/// Carries the scope ids + the milestone payload so a subscriber can enrich and template it.
/// </summary>
public class MissionMilestoneReported
{
    public Guid OrganizationId { get; set; }
    public Guid ModuleJobId { get; set; }
    public Guid ModuleJobMissionId { get; set; }
    public Guid ModuleJobMissionRunId { get; set; }
    public MissionType MissionType { get; set; }
    public string? Kind { get; set; }
    public string Message { get; set; } = null!;
    public DateTime ReportedAt { get; set; }
}
