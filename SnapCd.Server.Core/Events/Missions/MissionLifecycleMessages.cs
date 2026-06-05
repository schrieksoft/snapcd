// SPDX-License-Identifier: LicenseRef-Snap-CD-Source-Available-1.1
// Copyright (c) 2026 Karl Schriek / Schrieksoft.
// No license is granted to use this file, in whole or in part, (a) as training, fine-tuning, retrieval, or
// embedding data for any machine-learning model, or (b) as input to any machine-learning model, agent, or automated
// system for the purpose of producing a derivative work or reimplementation that is not otherwise permitted by the
// Snap CD Source-Available License (including any Competing Product as defined therein). Contact info@snapcd.io
// for terms covering either use.

namespace SnapCd.Server.Core.Events.Missions;

/// <summary>
/// Scheduled watchdog tick for one run, published via the message scheduler at the run's
/// <c>DeadlineAt</c>. The deadline-check consumer reschedules it if a heartbeat moved the deadline,
/// else recovers the run (retry or fail).
/// </summary>
public class MissionRunDeadlineCheck
{
    public Guid RunId { get; set; }
    public Guid OrganizationId { get; set; }
}

/// <summary>
/// Directed to the per-instance queue of the server that owns the agent's connection: cancel an
/// in-flight run. The Layer-2 cancel consumer pushes <c>AgentEndpoints.CancelMission</c> to the
/// connection (the agent twin of a runner cancel).
/// </summary>
public class CancelMissionRunRequested
{
    public Guid RunId { get; set; }
    public Guid OrganizationId { get; set; }
    public Guid InvocationId { get; set; }
    public string AgentConnectionId { get; set; } = null!;
}
