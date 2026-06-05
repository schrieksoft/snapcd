// SPDX-License-Identifier: LicenseRef-Snap-CD-Source-Available-1.1
// Copyright (c) 2026 Karl Schriek / Schrieksoft.
// No license is granted to use this file, in whole or in part, (a) as training, fine-tuning, retrieval, or
// embedding data for any machine-learning model, or (b) as input to any machine-learning model, agent, or automated
// system for the purpose of producing a derivative work or reimplementation that is not otherwise permitted by the
// Snap CD Source-Available License (including any Competing Product as defined therein). Contact info@snapcd.io
// for terms covering either use.

namespace SnapCd.Agent.Models;

/// <summary>
/// Request sent to a sidecar's <c>POST /invoke</c>. Matches the JSON contract in ai-agent.md.
/// </summary>
public sealed class InvokeRequest
{
    public string Mission { get; set; } = null!;
    public string Skill { get; set; } = null!;
    public Dictionary<string, string> Parameters { get; set; } = new();
    public SessionSpec Session { get; set; } = new();

    /// <summary>Agent-attributed bearer token the sidecar uses to call SnapCd's MCP endpoint.</summary>
    public string SnapcdMcpToken { get; set; } = null!;

    /// <summary>Event correlation id, carried through for idempotency / audit.</summary>
    public Guid CorrelationId { get; set; }
}

public sealed class SessionSpec
{
    public string Mode { get; set; } = "ephemeral";
    public string Key { get; set; } = null!;
    public Dictionary<string, string>? Rotation { get; set; }
}

public sealed class InvokeResponse
{
    public bool Success { get; set; }
    public string? Summary { get; set; }
    public string? Error { get; set; }
    public string? Detail { get; set; }
}
