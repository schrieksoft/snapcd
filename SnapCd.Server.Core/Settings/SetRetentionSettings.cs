// SPDX-License-Identifier: LicenseRef-Snap-CD-Source-Available-1.1
// Copyright (c) 2026 Karl Schriek / Schrieksoft.
// No license is granted to use this file, in whole or in part, (a) as training, fine-tuning, retrieval, or
// embedding data for any machine-learning model, or (b) as input to any machine-learning model, agent, or automated
// system for the purpose of producing a derivative work or reimplementation that is not otherwise permitted by the
// Snap CD Source-Available License (including any Competing Product as defined therein). Contact info@snapcd.io
// for terms covering either use.

namespace SnapCd.Server.Core.Settings;

/// <summary>
/// How many of a Module's sets are kept, and whether the oldest are removed as new ones arrive.
/// One section each under <c>SetRetention</c>.
/// </summary>
public class SetRetentionSettings
{
    public OutputSetRetentionSettings OutputSets { get; set; } = new();
}

/// <summary>
/// A Module's output sets. Nothing holds a reference to one: its Outputs cascade from it, and
/// downstream wiring resolves a Module and an output name against whichever set is latest.
/// </summary>
public class OutputSetRetentionSettings
{
    /// <summary>Whether the oldest sets beyond the limit are removed when a new one is created.</summary>
    public bool CullAboveLimit { get; set; } = false;

    /// <summary>How many of a Module's sets survive a cull, newest first.</summary>
    public int MaxKeptPerModule { get; set; } = 50;
}
