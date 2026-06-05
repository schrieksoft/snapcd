// SPDX-License-Identifier: LicenseRef-Snap-CD-Source-Available-1.1
// Copyright (c) 2026 Karl Schriek / Schrieksoft.
// No license is granted to use this file, in whole or in part, (a) as training, fine-tuning, retrieval, or
// embedding data for any machine-learning model, or (b) as input to any machine-learning model, agent, or automated
// system for the purpose of producing a derivative work or reimplementation that is not otherwise permitted by the
// Snap CD Source-Available License (including any Competing Product as defined therein). Contact info@snapcd.io
// for terms covering either use.

namespace SnapCd.Server.Core.Settings;

/// <summary>
/// Lifecycle settings for User invitations — how long invitations remain valid, whether
/// auto-cleanup runs for incomplete sign-ups, and whether email verification is required for the
/// invited account to become active.
/// </summary>
public class InvitationSettings
{
    /// <summary>
    /// How many days an invitation link remains valid before expiring. Defaults to 30.
    /// </summary>
    public int ExpirationDays { get; set; } = 30;

    /// <summary>
    /// When true (default), the cleanup job deletes Identity rows for invitees who never completed
    /// sign-up after their invitation expired. Set false to retain expired-but-incomplete rows
    /// (useful for audit trails that must record every invitation attempt).
    /// </summary>
    public bool AutoDeleteIncompleteUsers { get; set; } = true;

    /// <summary>
    /// When true (default), the invited User must confirm their email address before the account
    /// is enabled. Set false only in environments where email delivery is unreliable and the
    /// alternative-verification flow has been arranged out-of-band.
    /// </summary>
    public bool RequireEmailVerification { get; set; } = true;

    /// <summary>
    /// Quartz cron expression for the invitation-cleanup background job. Defaults to 00:00 daily.
    /// </summary>
    public string CleanupJobCron { get; set; } = "0 0 * * *"; // Daily at midnight
}