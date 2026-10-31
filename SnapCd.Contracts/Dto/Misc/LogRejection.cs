// SPDX-License-Identifier: LicenseRef-Snap-CD-Source-Available-1.1
// Copyright (c) 2026 Karl Schriek / Schrieksoft.
// No license is granted to use this file, in whole or in part, (a) as training, fine-tuning, retrieval, or
// embedding data for any machine-learning model, or (b) as input to any machine-learning model, agent, or automated
// system for the purpose of producing a derivative work or reimplementation that is not otherwise permitted by the
// Snap CD Source-Available License (including any Competing Product as defined therein). Contact info@snapcd.io
// for terms covering either use.

namespace SnapCd.Contracts.Dto.Misc;

/// <summary>
/// Marks a log batch the server will never accept, whatever the sender does.
///
/// A sender holds a batch it could not send and retries it, which is right while the reason is a
/// dropped connection or a server that is down. A batch refused for what it contains is refused
/// the same way on every attempt, so a sender that cannot tell the two apart holds it for good and
/// everything behind it with it.
/// </summary>
public static class LogRejection
{
    /// <summary>Prefix on the hub exception's message, which is all a SignalR client receives.</summary>
    public const string PermanentPrefix = "PERMANENT: ";

    /// <summary>Whether a failed send will fail the same way however often it is retried.</summary>
    public static bool IsPermanent(Exception exception) =>
        exception.Message.StartsWith(PermanentPrefix, StringComparison.Ordinal);
}
