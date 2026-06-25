// SPDX-License-Identifier: LicenseRef-Snap-CD-Source-Available-1.1
// Copyright (c) 2026 Karl Schriek / Schrieksoft.
// No license is granted to use this file, in whole or in part, (a) as training, fine-tuning, retrieval, or
// embedding data for any machine-learning model, or (b) as input to any machine-learning model, agent, or automated
// system for the purpose of producing a derivative work or reimplementation that is not otherwise permitted by the
// Snap CD Source-Available License (including any Competing Product as defined therein). Contact info@snapcd.io
// for terms covering either use.

using SnapCd.Contracts.AgentResults;

namespace SnapCd.Agent.Models;

/// <summary>One event parsed from the sidecar's SSE <c>/invoke</c> stream: a log line, a progress
/// milestone, or the final result.</summary>
public sealed class SidecarStreamEvent
{
    public bool IsResult { get; init; }
    public bool IsMilestone { get; init; }
    public string Level { get; init; } = "info";

    /// <summary>Set on milestone events: the optional checkpoint label.</summary>
    public string? MilestoneKind { get; init; }

    public string? Message { get; init; }
    public MissionResultDto? Result { get; init; }
}
