// SPDX-License-Identifier: LicenseRef-Snap-CD-Source-Available-1.1
// Copyright (c) 2026 Karl Schriek / Schrieksoft.
// No license is granted to use this file, in whole or in part, (a) as training, fine-tuning, retrieval, or
// embedding data for any machine-learning model, or (b) as input to any machine-learning model, agent, or automated
// system for the purpose of producing a derivative work or reimplementation that is not otherwise permitted by the
// Snap CD Source-Available License (including any Competing Product as defined therein). Contact info@snapcd.io
// for terms covering either use.

namespace SnapCd.Server.Core.Events.Missions;

/// <summary>
/// Published by <c>AgentHub</c> when a <c>ModuleJobMissionRun</c> changes status (started, completed,
/// faulted, cancelled). Fanned out so each server instance's local
/// <see cref="Services.Notification.MissionRunModifiedNotificationService"/> can push live updates to
/// subscribed Razor components (e.g. the Missions tab on a ModuleJob).
/// </summary>
public class MissionRunModifiedEvent
{
    public Guid OrganizationId { get; set; }
    public Guid ModuleJobId { get; set; }
    public Guid RunId { get; set; }
}
