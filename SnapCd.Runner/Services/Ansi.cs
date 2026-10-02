// SPDX-License-Identifier: LicenseRef-Snap-CD-Source-Available-1.1
// Copyright (c) 2026 Karl Schriek / Schrieksoft.
// No license is granted to use this file, in whole or in part, (a) as training, fine-tuning, retrieval, or
// embedding data for any machine-learning model, or (b) as input to any machine-learning model, agent, or automated
// system for the purpose of producing a derivative work or reimplementation that is not otherwise permitted by the
// Snap CD Source-Available License (including any Competing Product as defined therein). Contact info@snapcd.io
// for terms covering either use.

namespace SnapCd.Runner.Services;

/// <summary>
/// ANSI styling for the job log. The dashboard converts these codes to HTML, so they are how a
/// line's origin is shown: Snap CD's own narration is dim, the tool's output is left plain.
/// </summary>
public static class Ansi
{
    private const string DimCode = "\u001b[2m";
    private const string CyanCode = "\u001b[36m";
    private const string Reset = "\u001b[0m";

    public static string Dim(string text) => DimCode + text + Reset;

    /// <summary>The one object a line is about: a sha, a path, a source URL.</summary>
    public static string Emphasis(string text) => CyanCode + text + Reset;
}
