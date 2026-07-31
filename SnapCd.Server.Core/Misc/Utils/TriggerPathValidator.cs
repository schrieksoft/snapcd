// SPDX-License-Identifier: LicenseRef-Snap-CD-Source-Available-1.1
// Copyright (c) 2026 Karl Schriek / Schrieksoft.
// No license is granted to use this file, in whole or in part, (a) as training, fine-tuning, retrieval, or
// embedding data for any machine-learning model, or (b) as input to any machine-learning model, agent, or automated
// system for the purpose of producing a derivative work or reimplementation that is not otherwise permitted by the
// Snap CD Source-Available License (including any Competing Product as defined therein). Contact info@snapcd.io
// for terms covering either use.

using SnapCd.Server.Core.Misc.Exceptions;

namespace SnapCd.Server.Core.Misc.Utils;

/// <summary>
/// Validates Additional Trigger Path values: normalized repo-root-relative directories that can never escape the
/// repository. Shared between the repositories and the dashboard so the UI and the API enforce one rule.
/// </summary>
public static class TriggerPathValidator
{
    private const int MaxLength = 255;

    /// <summary>
    /// Returns an error message when the path is empty, too long, absolute, backslashed, unnormalized (empty,
    /// '.' or '..' segments, trailing slash), or would escape the repository root; null when valid.
    /// </summary>
    public static string? Validate(string? path)
    {
        if (string.IsNullOrWhiteSpace(path))
            return "Trigger path must not be empty.";

        if (path.Length > MaxLength)
            return $"Trigger path must not exceed {MaxLength} characters.";

        if (path.Contains('\\'))
            return $"Trigger path '{path}' is invalid. Use forward slashes as separators.";

        if (path.StartsWith('/'))
            return $"Trigger path '{path}' is invalid. Paths are repo-root-relative and must not start with a slash.";

        if (path != path.Trim())
            return $"Trigger path '{path}' is invalid. Paths must not contain leading or trailing whitespace.";

        foreach (var segment in path.Split('/'))
            switch (segment)
            {
                case "":
                    return $"Trigger path '{path}' is invalid. Paths must not contain empty segments or end with a slash.";
                case ".":
                case "..":
                    return $"Trigger path '{path}' is invalid. Paths must be normalized and must not contain '.' or '..' segments.";
            }

        return null;
    }

    /// <summary>
    /// Throws <see cref="InvalidNameException"/> when <see cref="Validate"/> reports an error.
    /// </summary>
    public static void EnsureValid(string? path)
    {
        var error = Validate(path);
        if (error != null)
            throw new InvalidNameException(error);
    }
}
