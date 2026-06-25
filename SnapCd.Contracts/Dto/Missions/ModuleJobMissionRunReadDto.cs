// SPDX-License-Identifier: LicenseRef-Snap-CD-Source-Available-1.1
// Copyright (c) 2026 Karl Schriek / Schrieksoft.
// No license is granted to use this file, in whole or in part, (a) as training, fine-tuning, retrieval, or
// embedding data for any machine-learning model, or (b) as input to any machine-learning model, agent, or automated
// system for the purpose of producing a derivative work or reimplementation that is not otherwise permitted by the
// Snap CD Source-Available License (including any Competing Product as defined therein). Contact info@snapcd.io
// for terms covering either use.

using SnapCd.Contracts.Interfaces;

namespace SnapCd.Contracts.Dto.Missions;

/// <summary>
/// Read projection of a <c>ModuleJobMissionRun</c> — one execution attempt of a mission. Omits the
/// connection-binding internals (SignalR/server-instance ids, deadline/heartbeat fields) and the raw
/// streamed <c>Logs</c> blob; those aren't part of the entity's public shape.
/// </summary>
public class ModuleJobMissionRunReadDto : IDto
{
    public Guid Id { get; set; }
    public Guid ModuleJobMissionId { get; set; }
    public Guid ModuleJobId { get; set; }
    public MissionType MissionType { get; set; }
    public Guid AgentId { get; set; }
    public Guid InvocationId { get; set; }
    public int AttemptNumber { get; set; }
    public MissionStatus Status { get; set; }
    public DateTime? StartedAt { get; set; }
    public DateTime? CompletedAt { get; set; }
    public string? ResultSummary { get; set; }
    public string? Error { get; set; }
    public string? ToolCallsJson { get; set; }
    public string? TokensJson { get; set; }
    public double? DurationSeconds { get; set; }
    public DiagnosisCategory? DiagnosisCategory { get; set; }
}
