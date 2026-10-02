// SPDX-License-Identifier: LicenseRef-Snap-CD-Source-Available-1.1
// Copyright (c) 2026 Karl Schriek / Schrieksoft.
// No license is granted to use this file, in whole or in part, (a) as training, fine-tuning, retrieval, or
// embedding data for any machine-learning model, or (b) as input to any machine-learning model, agent, or automated
// system for the purpose of producing a derivative work or reimplementation that is not otherwise permitted by the
// Snap CD Source-Available License (including any Competing Product as defined therein). Contact info@snapcd.io
// for terms covering either use.

namespace SnapCd.Contracts.Constants;

/// <summary>
/// Codes a hub method puts at the front of a <c>HubException</c> message, so the caller can tell
/// one rejection from another. SignalR sends only the message text across the wire - the exception
/// type does not survive - so the code travels in the message and is read from there.
/// </summary>
public static class HubErrorCodes
{
    /// <summary>
    /// The caller's authentication token has expired. Transient: the connection reconnects with a
    /// fresh token and the same call succeeds.
    /// </summary>
    public const string TokenExpired = "SNAPCD-E002";

    /// <summary>Whether a message carries the given code.</summary>
    public static bool Is(string code, string? message) =>
        message is not null && message.StartsWith(code, StringComparison.Ordinal);

    /// <summary>The codes a caller should try again; everything else is permanent.</summary>
    public static readonly IReadOnlySet<string> Retryable = new HashSet<string>(StringComparer.Ordinal)
    {
        TokenExpired
    };

    /// <summary>Whether a message carries any code the caller should try again.</summary>
    public static bool IsRetryable(string? message) =>
        message is not null && Retryable.Any(code => Is(code, message));
}
