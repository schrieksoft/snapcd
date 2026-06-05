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
/// Optional content-based allowlist for Hook scripts the Runner is permitted to execute. When
/// enabled, every Hook a Job tries to run must match (by SHA256) a file in the allowlist
/// directory or it is refused. Intended for security-sensitive deployments where the set of
/// shippable Hooks must be reviewed out-of-band.
/// </summary>
public class HooksPreapprovalSettings
{
    private string _preapprovedHooksDirectory = string.Empty;

    /// <summary>
    /// Enable or disable hook pre-approval validation.
    /// When enabled, all incoming hooks must match a pre-approved hook from the PreapprovedHooksDirectory.
    /// </summary>
    public bool Enabled { get; set; }

    /// <summary>
    /// Directory containing pre-approved hook scripts.
    /// Each file in this directory is considered a pre-approved hook.
    /// File names don't matter - only file content is used for validation.
    /// </summary>
    public string PreapprovedHooksDirectory
    {
        get => _preapprovedHooksDirectory;
        set => _preapprovedHooksDirectory = PathUtils.ExpandTilde(value);
    }
}
