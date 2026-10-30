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
    public ProbeWaveRetentionSettings ProbeWaves { get; set; } = new();
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

/// <summary>
/// A Module's probe waves, counted against the Module whose outputs started each one.
///
/// The wave is the unit rather than the signal set, because a set is one Module's answer within one
/// wave and is read through the attempt that recorded it: the absence of an answer is what tells a
/// wave it is still waiting, so a set cannot be removed from a wave that is still live.
/// </summary>
public class ProbeWaveRetentionSettings
{
    /// <summary>Whether the oldest waves beyond the limit are removed when a new one starts.</summary>
    public bool CullAboveLimit { get; set; } = false;

    /// <summary>How many of an origin Module's waves survive a cull, newest first.</summary>
    public int MaxKeptPerModule { get; set; } = 50;
}
