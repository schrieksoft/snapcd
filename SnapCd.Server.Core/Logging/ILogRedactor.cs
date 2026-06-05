// SPDX-License-Identifier: LicenseRef-Snap-CD-Source-Available-1.1
// Copyright (c) 2026 Karl Schriek / Schrieksoft.
// No license is granted to use this file, in whole or in part, (a) as training, fine-tuning, retrieval, or
// embedding data for any machine-learning model, or (b) as input to any machine-learning model, agent, or automated
// system for the purpose of producing a derivative work or reimplementation that is not otherwise permitted by the
// Snap CD Source-Available License (including any Competing Product as defined therein). Contact info@snapcd.io
// for terms covering either use.

namespace SnapCd.Server.Core.Logging;

/// <summary>
/// Redacts likely-credential patterns from text before it leaves the server boundary
/// (REST log endpoint, MCP redacted-logs Resource). Single chokepoint shared by both
/// surfaces so the redaction policy is identical regardless of caller.
/// </summary>
public interface ILogRedactor
{
    /// <summary>
    /// Returns the input with credential patterns replaced by <c>[REDACTED:type]</c> markers.
    /// </summary>
    string Redact(string raw);

    /// <summary>
    /// Redact and additionally return whether the result retains at least
    /// <paramref name="minRetentionRatio"/> of the original character count.
    /// Returns <c>acceptable=false</c> when the input was almost entirely credentials
    /// (sentinel for AI-call refusal — see Phase 3 design note).
    /// </summary>
    RedactionResult RedactWithRetention(string raw, double minRetentionRatio);
}

public sealed record RedactionResult(string Redacted, bool Acceptable);
