// SPDX-License-Identifier: LicenseRef-Snap-CD-Source-Available-1.1
// Copyright (c) 2026 Karl Schriek / Schrieksoft.
// No license is granted to use this file, in whole or in part, (a) as training, fine-tuning, retrieval, or
// embedding data for any machine-learning model, or (b) as input to any machine-learning model, agent, or automated
// system for the purpose of producing a derivative work or reimplementation that is not otherwise permitted by the
// Snap CD Source-Available License (including any Competing Product as defined therein). Contact info@snapcd.io
// for terms covering either use.

namespace SnapCd.Server.Core.Settings;

/// <summary>
/// Configures how the Server obtains, refreshes, and validates its license token against the
/// Snap CD Licensing Service. The endpoint is fixed; operators adjust only the refresh schedule.
/// </summary>
public class LicenseSettings
{
    /// <summary>
    /// Base URL of the Snap CD Licensing Service, used for license issue and refresh, the signing
    /// public key, and the usage beacon. Fixed to https://snapcd.io whenever no debugger is attached;
    /// the setting exists so local development can point at a local licensing service.
    /// </summary>
    public string LicenseServerBaseUrl { get; set; } = "https://snapcd.io";

    /// <summary>
    /// Quartz cron expression for the background license refresh job. Defaults to 03:00 daily,
    /// which is frequent enough to propagate Cloud-side state changes (cancellation, plan changes,
    /// expiry) within a day while staying well under the Cloud endpoint's rate limit.
    /// </summary>
    public string RefreshJobCron { get; set; } = "0 3 * * *";

    /// <summary>
    /// How many days before token expiry the refresh job tries to renew. Defaults to 3 — gives the
    /// scheduled job 3 daily opportunities to renew before a stale token actually expires.
    /// </summary>
    public int RefreshWithinDaysOfExpiry { get; set; } = 3;
}
