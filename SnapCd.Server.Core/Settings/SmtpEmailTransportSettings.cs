// SPDX-License-Identifier: LicenseRef-Snap-CD-Source-Available-1.1
// Copyright (c) 2026 Karl Schriek / Schrieksoft.
// No license is granted to use this file, in whole or in part, (a) as training, fine-tuning, retrieval, or
// embedding data for any machine-learning model, or (b) as input to any machine-learning model, agent, or automated
// system for the purpose of producing a derivative work or reimplementation that is not otherwise permitted by the
// Snap CD Source-Available License (including any Competing Product as defined therein). Contact info@snapcd.io
// for terms covering either use.

using System.ComponentModel.DataAnnotations;

namespace SnapCd.Server.Core.Settings;

/// <summary>
/// Generic SMTP transport credentials and sender identity. Used when
/// <c>EmailSender.EmailProvider</c> is <c>"Smtp"</c>; otherwise ignored. Suitable for relaying
/// through Google Workspace, Microsoft 365, internal MTAs, or any other RFC-compliant SMTP server.
/// </summary>
public class SmtpEmailTransportSettings
{
    /// <summary>SMTP server hostname (e.g. <c>smtp-relay.gmail.com</c>, <c>smtp.office365.com</c>).</summary>
    public string SmtpHost { get; set; } = "smtp-relay.gmail.com";

    /// <summary>SMTP port. Common values: 587 (submission with STARTTLS), 465 (implicit TLS), 25 (plain).</summary>
    public int SmtpPort { get; set; } = 587;

    /// <summary>
    /// When true, connect with implicit SSL/TLS on connect. Use for port 465. Mutually exclusive
    /// in spirit with <see cref="UseStartTls"/> — most modern SMTP servers expect either implicit
    /// TLS or STARTTLS, not both.
    /// </summary>
    public bool UseSsl { get; set; } = true;

    /// <summary>SMTP authentication username. Often the same as <see cref="FromEmail"/> for hosted services.</summary>
    [Required]
    public string Username { get; set; } = null!;

    /// <summary>
    /// SMTP authentication password (or app password for accounts with 2FA). Sensitive — source
    /// via the External Settings provider in production.
    /// </summary>
    [Required]
    public string Password { get; set; } = null!;

    /// <summary>"From" address every Server-sent email is delivered as.</summary>
    [Required]
    public string FromEmail { get; set; } = null!;

    /// <summary>Display name shown alongside <see cref="FromEmail"/> in clients.</summary>
    [Required]
    public string FromName { get; set; } = null!;

    /// <summary>
    /// When true, issue STARTTLS to upgrade the plaintext connection to TLS before authentication.
    /// Use for port 587. The standard "submission" port flow.
    /// </summary>
    public bool UseStartTls { get; set; } = true;
}