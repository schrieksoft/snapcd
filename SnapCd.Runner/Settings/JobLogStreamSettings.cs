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
    /// Maximum number of log events to ship in a single batch. A job's queue ships early when
    /// this size is reached even before <see cref="PeriodSeconds"/> elapses.
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

    /// <summary>A batch also ships once its messages reach this many characters, so long lines go in fewer calls.</summary>
    public int BatchBytesLimit { get; set; } = 256 * 1024;

    /// <summary>Most lines one job may have waiting to ship; beyond it lines are dropped and the job's log says how many.</summary>
    public int QueueLimit { get; set; } = 200_000;

    /// <summary>A job with this many lines waiting to ship is logged as a warning on the runner.</summary>
    public int QueueWarningThreshold { get; set; } = 20_000;

    /// <summary>How long a step waits for its job's logs to ship before reporting its result.</summary>
    public int DrainTimeoutSeconds { get; set; } = 30;

    /// <summary>A job's queue is retired after this long with nothing to ship.</summary>
    public int IdleSeconds { get; set; } = 60;

    /// <summary>
    /// How long a batch that could not be sent is kept before it is discarded. The buffer exists
    /// for a server that is briefly unreachable, so a batch nothing has accepted by now counts as
    /// undeliverable whatever the reason.
    /// </summary>
    public int BufferRetentionMinutes { get; set; } = 10;
}
