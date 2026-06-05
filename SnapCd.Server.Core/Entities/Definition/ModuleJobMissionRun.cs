// SPDX-License-Identifier: LicenseRef-Snap-CD-Source-Available-1.1
// Copyright (c) 2026 Karl Schriek / Schrieksoft.
// No license is granted to use this file, in whole or in part, (a) as training, fine-tuning, retrieval, or
// embedding data for any machine-learning model, or (b) as input to any machine-learning model, agent, or automated
// system for the purpose of producing a derivative work or reimplementation that is not otherwise permitted by the
// Snap CD Source-Available License (including any Competing Product as defined therein). Contact info@snapcd.io
// for terms covering either use.

using System.Text.Json.Serialization;
using SnapCd.Contracts;
using SnapCd.Server.Core.Entities.Definition.Base;
using SnapCd.Server.Core.Entities.Interfaces;

namespace SnapCd.Server.Core.Entities.Definition;

/// <summary>
/// One execution attempt of a <see cref="ModuleJobMission"/>. The unit of run tracking, concurrency
/// locking, cancellation and retry. A filtered unique index over
/// <c>(ModuleJobId, MissionType, OrganizationId)</c> restricted to the non-terminal statuses enforces
/// <b>at most one active run per (job, type)</b> — the hard, race-proof lock (the DB is the arbiter).
/// </summary>
public class ModuleJobMissionRun : AuditBase, IEntity, ICreationTrackable
{
    public Guid Id { get; set; }
    public Guid OrganizationId { get; set; }

    public Guid ModuleJobMissionId { get; set; }

    /// <summary>Denormalised from the parent — the columns the filtered unique lock index covers.</summary>
    public Guid ModuleJobId { get; set; }
    public MissionType MissionType { get; set; }

    public Guid AgentId { get; set; }

    /// <summary>Correlation id minted per attempt; the agent reports results/logs/heartbeats against it.</summary>
    public Guid InvocationId { get; set; }

    public int AttemptNumber { get; set; }

    public MissionStatus Status { get; set; }

    /// <summary>Heartbeat / reconnect watchdog deadline. A lapsed deadline (no heartbeat) triggers recovery.</summary>
    public DateTime DeadlineAt { get; set; }

    /// <summary>Timestamp of the last sidecar stream event — drives the no-progress timeout.</summary>
    public DateTime? LastEventAt { get; set; }

    public DateTime? CancelRequestedAt { get; set; }
    public DateTime? StartedAt { get; set; }
    public DateTime? CompletedAt { get; set; }

    // ---- Owning connection (set at dispatch) — lets OnDisconnectedAsync find this connection's runs. ----
    public Guid? AgentConnectionId { get; set; }
    public Guid? ServerInstanceId { get; set; }
    public string? SignalRConnectionId { get; set; }

    /// <summary>This attempt's streamed agent log (dedicated — never the runner's <see cref="ModuleJob.Logs"/>).</summary>
    public string? Logs { get; set; }

    public string? ResultSummary { get; set; }
    public string? Error { get; set; }
    public string? ToolCallsJson { get; set; }
    public string? TokensJson { get; set; }
    public double? DurationSeconds { get; set; }

    /// <summary>
    /// Structured verdict reported via the <c>report_diagnosis_category</c> MCP tool (AutoDiagnose /
    /// AutoFix only). Null when the mission doesn't classify or the agent terminated without calling
    /// the tool. Stored as string for safe enum-evolution.
    /// </summary>
    public DiagnosisCategory? DiagnosisCategory { get; set; }

    [JsonIgnore] public virtual ModuleJobMission ModuleJobMission { get; set; } = null!;
    [JsonIgnore] public virtual Organization Organization { get; set; } = null!;

    public Guid ParentId()
    {
        return ModuleJobMissionId;
    }
}
