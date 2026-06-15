// SPDX-License-Identifier: LicenseRef-Snap-CD-Source-Available-1.1
// Copyright (c) 2026 Karl Schriek / Schrieksoft.
// No license is granted to use this file, in whole or in part, (a) as training, fine-tuning, retrieval, or
// embedding data for any machine-learning model, or (b) as input to any machine-learning model, agent, or automated
// system for the purpose of producing a derivative work or reimplementation that is not otherwise permitted by the
// Snap CD Source-Available License (including any Competing Product as defined therein). Contact info@snapcd.io
// for terms covering either use.

namespace SnapCd.Contracts.AgentRequests;

/// <summary>
/// Base for the per-mission invocation DTOs the server pushes to a connected agent over the
/// <see cref="Constants.AgentEndpoints"/> methods. One concrete type per mission (no shared
/// envelope), mirroring the per-task request DTOs under <c>SnapCd.Contracts.RunnerRequests</c>.
/// </summary>
public abstract class MissionRequestBase
{
    public Guid InvocationId { get; set; }

    /// <summary>The <c>ModuleJobMissionRun</c> this invocation executes (the run-tracking unit).</summary>
    public Guid RunId { get; set; }

    public Guid MissionId { get; set; }

    /// <summary>The org the mission runs in — passed through to the skill so it can build
    /// org-scoped MCP resource URIs (e.g. <c>snapcd://orgs/{organizationId}/jobs/{jobId}/logs</c>).</summary>
    public Guid OrganizationId { get; set; }

    /// <summary>
    /// Optional sidecar selector. <c>null</c> means "use the agent's only registered sidecar"
    /// — fails with <c>NoDefaultSidecar</c> if the agent has zero or more than one. When set,
    /// the named sidecar must exist or the run fails with <c>UnknownSidecar</c>.
    /// </summary>
    public string? SidecarName { get; set; }
}

public class AutoDiagnoseRequest : MissionRequestBase
{
    public Guid JobId { get; set; }
    public Guid ModuleId { get; set; }
}

public class ApprovalRecommendRequest : MissionRequestBase
{
    public Guid JobId { get; set; }
    public Guid ModuleId { get; set; }
}

public class SummarizeJobRequest : MissionRequestBase
{
    public Guid JobId { get; set; }
    public Guid ModuleId { get; set; }
}

public class AutoFixRequest : MissionRequestBase
{
    public Guid JobId { get; set; }
    public Guid ModuleId { get; set; }
}

/// <summary>
/// Server → agent: cancel the in-flight run identified by <see cref="InvocationId"/>. Pushed on
/// <see cref="Constants.AgentEndpoints.CancelMission"/>; the orchestrator cancels that run's token.
/// Not a <see cref="MissionRequestBase"/> — it starts no work, it stops it.
/// </summary>
public class CancelMissionRequest
{
    public Guid InvocationId { get; set; }
    public Guid RunId { get; set; }
}
