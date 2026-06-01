// SPDX-License-Identifier: LicenseRef-Snap-CD-Source-Available-1.1
// Copyright (c) 2026 Karl Schriek / Schrieksoft.
// No license is granted to use this file, in whole or in part, (a) as training, fine-tuning, retrieval, or
// embedding data for any machine-learning model, or (b) as input to any machine-learning model, agent, or automated
// system for the purpose of producing a derivative work or reimplementation that is not otherwise permitted by the
// Snap CD Source-Available License (including any Competing Product as defined therein). Contact info@snapcd.io
// for terms covering either use.

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
