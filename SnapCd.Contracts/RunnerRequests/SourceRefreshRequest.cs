// SPDX-License-Identifier: LicenseRef-Snap-CD-Source-Available-1.1
// Copyright (c) 2026 Karl Schriek / Schrieksoft.
// No license is granted to use this file, in whole or in part, (a) as training, fine-tuning, retrieval, or
// embedding data for any machine-learning model, or (b) as input to any machine-learning model, agent, or automated
// system for the purpose of producing a derivative work or reimplementation that is not otherwise permitted by the
// Snap CD Source-Available License (including any Competing Product as defined therein). Contact info@snapcd.io
// for terms covering either use.

namespace SnapCd.Contracts.RunnerRequests;

/// <summary>
/// Request sent from server to runner via SignalR to refresh source information (resolve git references to commit SHAs).
/// This is a stateless operation - no JobId or correlation needed.
/// </summary>
public class SourceRefreshRequest
{
    public Guid OrganizationId { get; set; }
    public required string SourceUrl { get; set; }
    public required string SourceRevision { get; set; }
    public SourceType SourceType { get; set; } = SourceType.Git;
    public SourceRevisionType SourceRevisionType { get; set; } = SourceRevisionType.Default;

    /// <summary>
    /// Repo-root-relative directories the server wants tree hashes for, deduplicated across all modules in the
    /// refresh group. Empty means head-only refresh (today's behaviour); the runner then answers via the legacy
    /// completion and never touches a clone.
    /// </summary>
    public List<string> WatchedPaths { get; set; } = new();

    /// <summary>
    /// True when this refresh was dispatched by a SourceChanged notification rather than the recurring refresh
    /// schedule. Echoed back in the result so the server evaluates notification-triggered Modules for this
    /// refresh only — a notification-only Module must never be triggered by the polling schedule.
    /// </summary>
    public bool TriggeredByNotification { get; set; }
}
