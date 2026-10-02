// SPDX-License-Identifier: LicenseRef-Snap-CD-Source-Available-1.1
// Copyright (c) 2026 Karl Schriek / Schrieksoft.
// No license is granted to use this file, in whole or in part, (a) as training, fine-tuning, retrieval, or
// embedding data for any machine-learning model, or (b) as input to any machine-learning model, agent, or automated
// system for the purpose of producing a derivative work or reimplementation that is not otherwise permitted by the
// Snap CD Source-Available License (including any Competing Product as defined therein). Contact info@snapcd.io
// for terms covering either use.
using SnapCd.Contracts;
using SnapCd.Server.Core.Services.ResolvedConfiguration.HelperClasses;

namespace SnapCd.Server.Core.Misc.Utils;

/// <summary>
/// Runs one job against an explicit git ref instead of the Module's configured revision. The
/// override lives only in that job's declared module; the Module itself is never changed.
/// </summary>
public static class SourceRevisionOverride
{
    private const int MaxLength = 255;

    /// <summary>A ref the runner can hand to git as a positional argument without it being read as an option or a path trick.</summary>
    public static bool IsValidRef(string? value)
    {
        if (string.IsNullOrEmpty(value) || value.Length > MaxLength) return false;
        if (value[0] == '-' || value[0] == '/' || value[^1] == '/' || value[^1] == '.') return false;
        if (value.Contains("..") || value.Contains("@{") || value.Contains('\\') || value.EndsWith(".lock")) return false;
        return value.All(c => !char.IsWhiteSpace(c) && !char.IsControl(c) && c != '~' && c != '^' && c != ':' && c != '?' && c != '*' && c != '[');
    }

    public static ResolvedModule Apply(ResolvedModule declared, string sourceRevision)
    {
        if (!IsValidRef(sourceRevision))
            throw new ArgumentException($"'{sourceRevision}' is not a usable git ref.", nameof(sourceRevision));

        declared.SourceRevision = sourceRevision;
        declared.SourceRevisionType = SourceRevisionType.Default;
        return declared;
    }
}
