// SPDX-License-Identifier: LicenseRef-Snap-CD-Source-Available-1.1
// Copyright (c) 2026 Karl Schriek / Schrieksoft.
// No license is granted to use this file, in whole or in part, (a) as training, fine-tuning, retrieval, or
// embedding data for any machine-learning model, or (b) as input to any machine-learning model, agent, or automated
// system for the purpose of producing a derivative work or reimplementation that is not otherwise permitted by the
// Snap CD Source-Available License (including any Competing Product as defined therein). Contact info@snapcd.io
// for terms covering either use.

namespace SnapCd.Server.Core.Events.Missions;

/// <summary>
/// Bus messages from Layer 1 (the competing match consumers) to Layer 2 (the per-mission
/// dispatch consumers under <c>Consumers/Missions</c>). One per mission type. Layer 1
/// directed-Sends these to the per-instance queue of the server that owns the target agent's
/// SignalR connection, so Layer 2 runs co-located with that connection.
/// </summary>
public abstract class MissionRequestedBase
{
    public Guid InvocationId { get; set; }
    public Guid OrganizationId { get; set; }
    public Guid AgentId { get; set; }
    public Guid MissionId { get; set; }

    /// <summary>The <c>ModuleJobMissionRun</c> claimed by Layer 1 for this dispatch.</summary>
    public Guid RunId { get; set; }

    /// <summary>SignalR connection id the Layer-2 consumer invokes the mission endpoint on.</summary>
    public string AgentConnectionId { get; set; } = null!;

    /// <summary>
    /// Sidecar selector denormalized from <c>ModuleJobMission.SidecarName</c> at dispatch.
    /// <c>null</c> means "let the agent pick its only registered sidecar" — wire-semantics
    /// match <c>MissionRequestBase.SidecarName</c>.
    /// </summary>
    public string? SidecarName { get; set; }
}

public class AutoDiagnoseMissionRequested : MissionRequestedBase
{
    public Guid JobId { get; set; }
    public Guid ModuleId { get; set; }
}

public class ApprovalRecommendMissionRequested : MissionRequestedBase
{
    public Guid JobId { get; set; }
    public Guid ModuleId { get; set; }
}

public class SummarizeJobMissionRequested : MissionRequestedBase
{
    public Guid JobId { get; set; }
    public Guid ModuleId { get; set; }
}
