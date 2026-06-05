// SPDX-License-Identifier: LicenseRef-Snap-CD-Source-Available-1.1
// Copyright (c) 2026 Karl Schriek / Schrieksoft.
// No license is granted to use this file, in whole or in part, (a) as training, fine-tuning, retrieval, or
// embedding data for any machine-learning model, or (b) as input to any machine-learning model, agent, or automated
// system for the purpose of producing a derivative work or reimplementation that is not otherwise permitted by the
// Snap CD Source-Available License (including any Competing Product as defined therein). Contact info@snapcd.io
// for terms covering either use.

namespace SnapCd.Runner.Settings;

/// <summary>
/// Tunables for the per-Job log shipping pipeline that streams engine output back to the Server
/// over SignalR. Defaults are sensible for typical workloads; tune BatchSizeLimit and
/// PeriodSeconds together if you need lower per-log latency at the cost of more frequent
/// network round-trips.
/// </summary>
public class JobLogStreamSettings
{
    /// <summary>
    /// Maximum number of log events to ship in a single batch. The PeriodicBatchingSink will
    /// flush early when this size is reached even before <see cref="PeriodSeconds"/> elapses.
    /// </summary>
    public int BatchSizeLimit { get; set; } = 50;

    /// <summary>
    /// Maximum wall-clock interval, in seconds, between batch flushes. A batch ships whenever
    /// either <see cref="BatchSizeLimit"/> or this period is reached.
    /// </summary>
    public int PeriodSeconds { get; set; } = 5;

    /// <summary>
    /// When true, the first event in a fresh batch is emitted immediately rather than waiting
    /// for the period or size threshold. Keeps initial job output responsive.
    /// </summary>
    public bool EagerlyEmitFirstEvent { get; set; } = true;
}
