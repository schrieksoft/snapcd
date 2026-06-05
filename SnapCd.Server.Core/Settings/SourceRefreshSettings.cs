// SPDX-License-Identifier: LicenseRef-Snap-CD-Source-Available-1.1
// Copyright (c) 2026 Karl Schriek / Schrieksoft.
// No license is granted to use this file, in whole or in part, (a) as training, fine-tuning, retrieval, or
// embedding data for any machine-learning model, or (b) as input to any machine-learning model, agent, or automated
// system for the purpose of producing a derivative work or reimplementation that is not otherwise permitted by the
// Snap CD Source-Available License (including any Competing Product as defined therein). Contact info@snapcd.io
// for terms covering either use.

namespace SnapCd.Server.Core.Settings;

/// <summary>
/// Cadence and timeout for the background job that polls Module source repositories for new
/// commits. Defaults are tuned for a moderate-sized organization with a handful of source
/// repositories; large fleets with many Modules per repository may want to lengthen the interval
/// to reduce SCM-API load.
/// </summary>
public class SourceRefreshSettings
{
    /// <summary>
    /// Quartz cron expression for the source-refresh job. Defaults to every 5 minutes — fast
    /// enough that operators see new commits reflected quickly, slow enough not to hammer the
    /// SCM provider's rate limits.
    /// </summary>
    public string RefreshIntervalCronExpression { get; set; } = "*/5 * * * *"; // Every 5 minutes

    /// <summary>
    /// Per-repository timeout for the refresh job, in seconds. Defaults to 120 — generous for a
    /// healthy clone but short enough that one wedged repository can't stall the rest of the run.
    /// </summary>
    public int TimeoutSeconds { get; set; } = 120; // two minutes
}
