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
/// SendGrid transport credentials and sender identity. Used when
/// <c>EmailSender.EmailProvider</c> is <c>"SendGrid"</c>; otherwise ignored.
/// </summary>
public class SendGridEmailTransportSettings
{
    /// <summary>SendGrid API key. Sensitive — source via the External Settings provider in production.</summary>
    [Required]
    public string ApiKey { get; set; } = null!;

    /// <summary>"From" address every Server-sent email is delivered as. Must be verified in SendGrid.</summary>
    [Required]
    public string FromEmail { get; set; } = null!;

    /// <summary>Display name shown alongside <see cref="FromEmail"/> in clients.</summary>
    [Required]
    public string FromName { get; set; } = null!;
}
