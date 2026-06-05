// SPDX-License-Identifier: LicenseRef-Snap-CD-Source-Available-1.1
// Copyright (c) 2026 Karl Schriek / Schrieksoft.
// No license is granted to use this file, in whole or in part, (a) as training, fine-tuning, retrieval, or
// embedding data for any machine-learning model, or (b) as input to any machine-learning model, agent, or automated
// system for the purpose of producing a derivative work or reimplementation that is not otherwise permitted by the
// Snap CD Source-Available License (including any Competing Product as defined therein). Contact info@snapcd.io
// for terms covering either use.

using SnapCd.Runner.Utils;

namespace SnapCd.Runner.Settings;

/// <summary>
/// Filesystem locations the Runner uses for fetched Module source and ephemeral state. Both paths
/// support leading ~ expansion to the host user's home directory.
/// </summary>
public class WorkingDirectorySettings
{
    private string _workingDirectory = string.Empty;
    private string _tempDirectory = string.Empty;

    /// <summary>
    /// Root directory under which the Runner persists fetched Module source, engine state and
    /// per-Job outputs. Must be writable by the Runner process. Typically ~/.snapcd/runner.
    /// </summary>
    public string WorkingDirectory
    {
        get => _workingDirectory;
        set => _workingDirectory = PathUtils.ExpandTilde(value);
    }

    /// <summary>
    /// Directory for ephemeral per-Job scratch space. Cleaned between Jobs. Typically
    /// ~/.snapcd/runner/.temp.
    /// </summary>
    public string TempDirectory
    {
        get => _tempDirectory;
        set => _tempDirectory = PathUtils.ExpandTilde(value);
    }
}