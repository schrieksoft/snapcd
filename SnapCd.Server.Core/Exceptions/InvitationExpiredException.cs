// SPDX-License-Identifier: LicenseRef-Snap-CD-Source-Available-1.1
// Copyright (c) 2026 Karl Schriek / Schrieksoft.
// No license is granted to use this file, in whole or in part, (a) as training, fine-tuning, retrieval, or
// embedding data for any machine-learning model, or (b) as input to any machine-learning model, agent, or automated
// system for the purpose of producing a derivative work or reimplementation that is not otherwise permitted by the
// Snap CD Source-Available License (including any Competing Product as defined therein). Contact info@snapcd.io
// for terms covering either use.

namespace SnapCd.Server.Core.Exceptions;

public class InvitationExpiredException : Exception
{
    public DateTime ExpirationDateTime { get; }

    public InvitationExpiredException(DateTime expirationDateTime)
        : base($"This invitation expired on {expirationDateTime:yyyy-MM-dd}. Please request a new invitation.")
    {
        ExpirationDateTime = expirationDateTime;
    }

    public InvitationExpiredException(string message) : base(message)
    {
        ExpirationDateTime = DateTime.MinValue;
    }
}
