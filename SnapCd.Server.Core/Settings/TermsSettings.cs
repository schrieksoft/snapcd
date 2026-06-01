// SPDX-License-Identifier: LicenseRef-Snap-CD-Source-Available-1.1
// Copyright (c) 2026 Karl Schriek / Schrieksoft.
// No license is granted to use this file, in whole or in part, (a) as training, fine-tuning, retrieval, or
// embedding data for any machine-learning model, or (b) as input to any machine-learning model, agent, or automated
// system for the purpose of producing a derivative work or reimplementation that is not otherwise permitted by the
// Snap CD Source-Available License (including any Competing Product as defined therein). Contact info@snapcd.io
// for terms covering either use.

namespace SnapCd.Server.Core.Settings;

/// <summary>
/// Configuration for Terms of Service acceptance tracking.
/// </summary>
public class TermsSettings
{
    public const string SectionName = "Terms";

    /// <summary>
    /// Current version of the Terms of Service (format: YYYY-MM-DD).
    /// This should be updated whenever the terms are materially changed.
    /// </summary>
    public string CurrentVersion { get; set; } = "2026-01-28";

    /// <summary>
    /// URL to the Terms of Service page.
    /// </summary>
    public string TermsUrl { get; set; } = "/terms";

    /// <summary>
    /// URL to the Privacy Policy page.
    /// </summary>
    public string PrivacyUrl { get; set; } = "/privacy";

    /// <summary>
    /// Whether to require users to re-accept terms when a new version is published.
    /// If true, users who haven't accepted the current version will be prompted.
    /// </summary>
    public bool RequireReacceptanceOnUpdate { get; set; } = false;

    /// <summary>
    /// Grace period in days before blocking users who haven't accepted updated terms.
    /// Only applies if RequireReacceptanceOnUpdate is true.
    /// </summary>
    public int ReacceptanceGracePeriodDays { get; set; } = 30;
}
