// SPDX-License-Identifier: LicenseRef-Snap-CD-Source-Available-1.1
// Copyright (c) 2026 Karl Schriek / Schrieksoft.
// No license is granted to use this file, in whole or in part, (a) as training, fine-tuning, retrieval, or
// embedding data for any machine-learning model, or (b) as input to any machine-learning model, agent, or automated
// system for the purpose of producing a derivative work or reimplementation that is not otherwise permitted by the
// Snap CD Source-Available License (including any Competing Product as defined therein). Contact info@snapcd.io
// for terms covering either use.

namespace SnapCd.Server.Core.Settings;

/// <summary>
/// Postmark transport credentials and sender identity. Used when
/// <c>EmailSender.EmailProvider</c> is <c>"Postmark"</c>; otherwise ignored.
/// </summary>
public class PostmarkEmailTransportSettings
{
    /// <summary>Postmark server API token. Sensitive — source via the External Settings provider in production.</summary>
    public string ApiKey { get; set; } = null!;

    /// <summary>"From" address every Server-sent email is delivered as. Must be a verified sender signature in Postmark.</summary>
    public string FromEmail { get; set; } = null!;

    /// <summary>Display name shown alongside <see cref="FromEmail"/> in clients.</summary>
    public string FromName { get; set; } = null!;
}