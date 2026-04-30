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
