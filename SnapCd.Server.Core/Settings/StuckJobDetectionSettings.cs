// SPDX-License-Identifier: LicenseRef-Snap-CD-Source-Available-1.1
// Copyright (c) 2026 Karl Schriek / Schrieksoft.
// No license is granted to use this file, in whole or in part, (a) as training, fine-tuning, retrieval, or
// embedding data for any machine-learning model, or (b) as input to any machine-learning model, agent, or automated
// system for the purpose of producing a derivative work or reimplementation that is not otherwise permitted by the
// Snap CD Source-Available License (including any Competing Product as defined therein). Contact info@snapcd.io
// for terms covering either use.

namespace SnapCd.Server.Core.Settings;

public class StuckJobDetectionSettings
{
    public string CronExpression { get; set; } = "*/5 * * * *"; // Every 5 minutes

    /// <summary>A parked job whose runner has not returned within this window is flagged.</summary>
    public int RunnerWaitThresholdMinutes { get; set; } = 30;

    /// <summary>An approval wait beyond this is flagged; a configured approval timeout enforces itself.</summary>
    public int ApprovalWaitThresholdMinutes { get; set; } = 1440;

    /// <summary>Cancellations resolve in seconds; one still cancelling after this is stranded.</summary>
    public int CancellingThresholdMinutes { get; set; } = 10;
}
