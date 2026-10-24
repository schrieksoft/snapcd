// SPDX-License-Identifier: LicenseRef-Snap-CD-Source-Available-1.1
// Copyright (c) 2026 Karl Schriek / Schrieksoft.
// No license is granted to use this file, in whole or in part, (a) as training, fine-tuning, retrieval, or
// embedding data for any machine-learning model, or (b) as input to any machine-learning model, agent, or automated
// system for the purpose of producing a derivative work or reimplementation that is not otherwise permitted by the
// Snap CD Source-Available License (including any Competing Product as defined therein). Contact info@snapcd.io
// for terms covering either use.

namespace SnapCd.Server.Core.Settings;

public class JobLogSettings
{
    /// <summary>How much of a job's log, uncompressed, the dashboard loads at a time; "Load more logs" fetches the next page.</summary>
    public int PageBytes { get; set; } = 1024 * 1024;

    /// <summary>A task whose log is larger than this shows a notice, and renders only when asked.</summary>
    public long LargeLogThresholdBytes { get; set; } = 5 * 1024 * 1024;
}
