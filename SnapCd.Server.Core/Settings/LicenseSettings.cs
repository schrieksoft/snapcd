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
/// Snap CD Licensing Service. Defaults point at the public Cloud licensing endpoint and a daily
/// refresh schedule — operators typically only change <see cref="LicenseServerBaseUrl"/> when
/// running the Server in a disconnected environment with a private license proxy.
/// </summary>
public class LicenseSettings
{
    /// <summary>
    /// Base URL of the Snap CD Licensing Service. Defaults to the public Cloud endpoint
    /// (https://snapcd.io). Self-Hosted deployments running on Community-Plus or higher round-trip
    /// license tokens through this endpoint at refresh time. In non-debug runs the value is
    /// force-set to https://snapcd.io regardless of appsettings.json — see Program.cs.
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
