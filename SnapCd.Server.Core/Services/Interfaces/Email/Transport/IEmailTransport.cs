// SPDX-License-Identifier: LicenseRef-Snap-CD-Source-Available-1.1
// Copyright (c) 2026 Karl Schriek / Schrieksoft.
// No license is granted to use this file, in whole or in part, (a) as training, fine-tuning, retrieval, or
// embedding data for any machine-learning model, or (b) as input to any machine-learning model, agent, or automated
// system for the purpose of producing a derivative work or reimplementation that is not otherwise permitted by the
// Snap CD Source-Available License (including any Competing Product as defined therein). Contact info@snapcd.io
// for terms covering either use.

namespace SnapCd.Server.Core.Services.Email.Transport;

public interface IEmailTransport
{
    /// <summary>
    /// Sends the email and returns true if it was actually delivered, false if it was no-op'd
    /// (either because this transport is the no-op transport, or because a routing decorator
    /// — the licence gate — downgraded the call to no-op). Throws on transport failure.
    /// </summary>
    Task<bool> SendAsync(string toEmail, string subject, string htmlContent, string? plainTextContent = null);

    /// <summary>
    /// Predictive: returns true if a <see cref="SendAsync"/> call right now would actually
    /// deliver. Use for UI decisions made before any send. After a send, branch on the bool
    /// returned by <see cref="SendAsync"/> instead — that is authoritative.
    /// </summary>
    Task<bool> IsDeliveryActiveAsync(CancellationToken ct = default) => Task.FromResult(true);
}
