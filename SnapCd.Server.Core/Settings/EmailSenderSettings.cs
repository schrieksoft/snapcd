// SPDX-License-Identifier: LicenseRef-Snap-CD-Source-Available-1.1
// Copyright (c) 2026 Karl Schriek / Schrieksoft.
// No license is granted to use this file, in whole or in part, (a) as training, fine-tuning, retrieval, or
// embedding data for any machine-learning model, or (b) as input to any machine-learning model, agent, or automated
// system for the purpose of producing a derivative work or reimplementation that is not otherwise permitted by the
// Snap CD Source-Available License (including any Competing Product as defined therein). Contact info@snapcd.io
// for terms covering either use.

namespace SnapCd.Server.Core.Settings;

/// <summary>
/// Outbound email configuration. <see cref="EmailProvider"/> selects which transport sub-block
/// the Server uses; the remaining blocks are ignored. When <see cref="EmailProvider"/> is
/// <c>"NoOp"</c> (or unset), the Server runs in no-op mode and email-driven flows (password
/// resets, invitations) are admin-mediated rather than self-service.
/// </summary>
/// <remarks>
/// The runtime binding pattern doesn't go through a single <c>Configure&lt;EmailSenderSettings&gt;</c>
/// call — <c>SnapCd.Server.Core/Startup/EmailSender.cs</c> reads <c>EmailSender:EmailProvider</c>
/// directly and then registers the matching transport-specific settings type. This type exists
/// to give the JSON Schema a typed source so the generator can produce useful IntelliSense and
/// docs for the <c>EmailSender</c> section as a whole.
/// </remarks>
public sealed class EmailSenderSettings
{
    /// <summary>
    /// Which transport handles outbound email. Values: <c>"NoOp"</c> (default — disables email),
    /// <c>"AmazonSES"</c>, <c>"SendGrid"</c>, <c>"Mailgun"</c>, <c>"Postmark"</c>, <c>"Smtp"</c>.
    /// Any value not in this set is treated the same as <c>"NoOp"</c>.
    /// </summary>
    public string EmailProvider { get; set; } = "NoOp";

    /// <summary>
    /// Amazon SES transport credentials and sender identity. Used when
    /// <see cref="EmailProvider"/> is <c>"AmazonSES"</c>.
    /// </summary>
    public AmazonSesEmailTransportSettings AmazonSES { get; set; } = new();

    /// <summary>
    /// SendGrid transport credentials and sender identity. Used when
    /// <see cref="EmailProvider"/> is <c>"SendGrid"</c>.
    /// </summary>
    public SendGridEmailTransportSettings SendGrid { get; set; } = new();

    /// <summary>
    /// Mailgun transport credentials and sender identity. Used when
    /// <see cref="EmailProvider"/> is <c>"Mailgun"</c>.
    /// </summary>
    public MailgunEmailTransportSettings Mailgun { get; set; } = new();

    /// <summary>
    /// Postmark transport credentials and sender identity. Used when
    /// <see cref="EmailProvider"/> is <c>"Postmark"</c>.
    /// </summary>
    public PostmarkEmailTransportSettings Postmark { get; set; } = new();

    /// <summary>
    /// Generic SMTP transport credentials and sender identity. Used when
    /// <see cref="EmailProvider"/> is <c>"Smtp"</c>.
    /// </summary>
    public SmtpEmailTransportSettings Smtp { get; set; } = new();
}
