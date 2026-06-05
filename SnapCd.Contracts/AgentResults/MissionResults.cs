// SPDX-License-Identifier: LicenseRef-Snap-CD-Source-Available-1.1
// Copyright (c) 2026 Karl Schriek / Schrieksoft.
// No license is granted to use this file, in whole or in part, (a) as training, fine-tuning, retrieval, or
// embedding data for any machine-learning model, or (b) as input to any machine-learning model, agent, or automated
// system for the purpose of producing a derivative work or reimplementation that is not otherwise permitted by the
// Snap CD Source-Available License (including any Competing Product as defined therein). Contact info@snapcd.io
// for terms covering either use.

using System.Text.Json.Serialization;

namespace SnapCd.Contracts.AgentResults;

/// <summary>One streamed agent log line, forwarded by the orchestrator to <c>AgentHub.AddMissionLogs</c>.</summary>
public class MissionLogLineDto
{
    public DateTimeOffset Timestamp { get; set; }
    public string Level { get; set; } = "info";
    public string Message { get; set; } = null!;
}

/// <summary>Final mission outcome, reported by the orchestrator to <c>AgentHub.MissionCompleted</c>.</summary>
public class MissionResultDto
{
    public bool Success { get; set; }
    public string? Summary { get; set; }
    public string? Error { get; set; }
    public string? Detail { get; set; }
    public double DurationSeconds { get; set; }
    public string? ToolCallsJson { get; set; }
    public string? TokensJson { get; set; }
    public string? SessionId { get; set; }

    /// <summary>
    /// For AutoDiagnose / AutoFix: the structured verdict the agent committed via the
    /// <c>report_diagnosis_category</c> MCP tool. Null on missions that don't classify (e.g. SummarizeJob),
    /// or if the agent terminated without calling the tool.
    /// </summary>
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public DiagnosisCategory? DiagnosisCategory { get; set; }
}
