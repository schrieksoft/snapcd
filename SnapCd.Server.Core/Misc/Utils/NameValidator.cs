// SPDX-License-Identifier: LicenseRef-Snap-CD-Source-Available-1.1
// Copyright (c) 2026 Karl Schriek / Schrieksoft.
// No license is granted to use this file, in whole or in part, (a) as training, fine-tuning, retrieval, or
// embedding data for any machine-learning model, or (b) as input to any machine-learning model, agent, or automated
// system for the purpose of producing a derivative work or reimplementation that is not otherwise permitted by the
// Snap CD Source-Available License (including any Competing Product as defined therein). Contact info@snapcd.io
// for terms covering either use.

using System.Text.RegularExpressions;
using SnapCd.Server.Core.Misc.Exceptions;

namespace SnapCd.Server.Core.Misc.Utils;

/// <summary>
/// Validates entity names that end up in URL path segments (/Stack/{name},
/// /Namespace/{stack}/{name}, /Module/{stack}/{ns}/{name}) and in Terraform state paths
/// (/api/state/{store}/{moduleName}). Characters like '/', '?', '#' or '%' would break
/// routing and links, so names are restricted to letters, digits, dots, hyphens and
/// underscores, starting and ending with a letter or digit.
/// </summary>
public static partial class NameValidator
{
    private const int MaxLength = 255;

    [GeneratedRegex("^[a-zA-Z0-9]([a-zA-Z0-9._-]*[a-zA-Z0-9])?$")]
    private static partial Regex ValidNamePattern();

    /// <summary>
    /// Returns an error message if the name is null, empty, too long, or contains characters
    /// outside [a-zA-Z0-9._-] (or starts/ends with punctuation); null when valid. Used directly
    /// as MudBlazor field validation so the UI and the repositories share one rule.
    /// </summary>
    public static string? Validate(string? name, string entityKind)
    {
        if (string.IsNullOrWhiteSpace(name))
            return $"{entityKind} name must not be empty.";

        if (name.Length > MaxLength)
            return $"{entityKind} name must not exceed {MaxLength} characters.";

        if (!ValidNamePattern().IsMatch(name))
            return $"{entityKind} name '{name}' is invalid. Names may only contain letters, digits, dots, hyphens and underscores, and must start and end with a letter or digit.";

        return null;
    }

    /// <summary>
    /// Throws <see cref="InvalidNameException"/> when <see cref="Validate"/> reports an error.
    /// </summary>
    public static void EnsureValid(string? name, string entityKind)
    {
        var error = Validate(name, entityKind);
        if (error != null)
            throw new InvalidNameException(error);
    }
}
