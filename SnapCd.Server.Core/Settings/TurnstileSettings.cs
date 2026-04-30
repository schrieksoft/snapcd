namespace SnapCd.Server.Core.Settings;

public class TurnstileSettings
{
    public const string SectionName = "Turnstile";

    /// <summary>
    /// Enable or disable Turnstile verification globally.
    /// </summary>
    public bool IsEnabled { get; set; } = false;

    /// <summary>
    /// Cloudflare Turnstile site key (public, used in widget).
    /// </summary>
    public string SiteKey { get; set; } = string.Empty;

    /// <summary>
    /// Cloudflare Turnstile secret key (private, used for server-side verification).
    /// </summary>
    public string SecretKey { get; set; } = string.Empty;

    /// <summary>
    /// Widget theme: "light", "dark", or "auto" (matches app theme).
    /// </summary>
    public string Theme { get; set; } = "auto";
}
