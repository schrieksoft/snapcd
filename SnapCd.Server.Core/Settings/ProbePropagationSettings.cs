// SPDX-License-Identifier: LicenseRef-Snap-CD-Source-Available-1.1
// Copyright (c) 2026 Karl Schriek / Schrieksoft.
// No license is granted to use this file, in whole or in part, (a) as training, fine-tuning, retrieval, or
// embedding data for any machine-learning model, or (b) as input to any machine-learning model, agent, or automated
// system for the purpose of producing a derivative work or reimplementation that is not otherwise permitted by the
// Snap CD Source-Available License (including any Competing Product as defined therein). Contact info@snapcd.io
// for terms covering either use.

namespace SnapCd.Server.Core.Settings;

public class ProbePropagationSettings
{
    /// <summary>
    /// How far a wave may reach from its origin. A bound on runaway fan-out rather than something
    /// a Module's owner tunes, so it is one value for the deployment: a per-Module setting would
    /// raise the question of whose value wins along a path. Copied onto the wave at creation, so
    /// editing this does not change a wave already in flight.
    /// </summary>
    public int MaxDepth { get; set; } = 5;

    /// <summary>
    /// How often to look for Modules a wave is ready for. The interval is the delay between a
    /// Module being released and its Apply starting, so it bounds how long a wave takes rather
    /// than whether it works.
    /// </summary>
    public string SweepCronExpression { get; set; } = "*/1 * * * *";
}
