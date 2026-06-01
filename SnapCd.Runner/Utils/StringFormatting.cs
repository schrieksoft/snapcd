// SPDX-License-Identifier: LicenseRef-Snap-CD-Source-Available-1.1
// Copyright (c) 2026 Karl Schriek / Schrieksoft.
// No license is granted to use this file, in whole or in part, (a) as training, fine-tuning, retrieval, or
// embedding data for any machine-learning model, or (b) as input to any machine-learning model, agent, or automated
// system for the purpose of producing a derivative work or reimplementation that is not otherwise permitted by the
// Snap CD Source-Available License (including any Competing Product as defined therein). Contact info@snapcd.io
// for terms covering either use.

namespace SnapCd.Runner.Utils;

public static class StringFormatting
{
    public static string EscapeBashScript(string value)
    {
        return value.Replace("\"", "\\\"");
    }
}

public static class PathUtils
{
    /// <summary>
    /// Expands tilde (~) at the start of a path to the user's home directory.
    /// </summary>
    public static string ExpandTilde(string path)
    {
        if (string.IsNullOrEmpty(path))
            return path;

        if (path.StartsWith("~/") || path == "~")
        {
            var home = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
            return path == "~" ? home : Path.Combine(home, path.Substring(2));
        }

        return path;
    }
}