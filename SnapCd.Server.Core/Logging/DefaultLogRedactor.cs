// SPDX-License-Identifier: LicenseRef-Snap-CD-Source-Available-1.1
// Copyright (c) 2026 Karl Schriek / Schrieksoft.
// No license is granted to use this file, in whole or in part, (a) as training, fine-tuning, retrieval, or
// embedding data for any machine-learning model, or (b) as input to any machine-learning model, agent, or automated
// system for the purpose of producing a derivative work or reimplementation that is not otherwise permitted by the
// Snap CD Source-Available License (including any Competing Product as defined therein). Contact info@snapcd.io
// for terms covering either use.

using System.Text.RegularExpressions;

namespace SnapCd.Server.Core.Logging;

/// <summary>
/// Default snapcd log redactor. Each pattern below has a tag that is emitted in the
/// replacement (e.g. <c>[REDACTED:jwt]</c>) so downstream tooling can distinguish
/// what was scrubbed.
/// </summary>
public sealed class DefaultLogRedactor : ILogRedactor
{
    // Order matters: more-specific patterns first so they win over the generic
    // `password_form` catch-all, and `bearer_header` precedes `jwt` because a JWT
    // appearing inside `Authorization: Bearer ...` is semantically a header value.
    private static readonly (string Tag, Regex Pattern)[] Patterns =
    [
        // AWS access key id — looks like AKIA + 16 uppercase alphanumeric chars
        ("aws_access_key_id",
            new Regex(@"\bAKIA[0-9A-Z]{16}\b", RegexOptions.Compiled)),

        // AWS secret access key — common form: `aws_secret_access_key = <40 chars>`
        ("aws_secret_access_key",
            new Regex(@"\baws_secret_access_key\s*=\s*[a-zA-Z0-9/+=]{40}\b", RegexOptions.Compiled | RegexOptions.IgnoreCase)),

        // GCP service-account JSON snippet — `"private_key": "-----BEGIN ... -----"`
        ("gcp_private_key",
            new Regex("\"private_key\"\\s*:\\s*\"-----BEGIN.*?-----\"", RegexOptions.Compiled | RegexOptions.Singleline)),

        // Stripe live keys
        ("stripe_live",
            new Regex(@"\bsk_live_[A-Za-z0-9]{24,}\b", RegexOptions.Compiled)),

        // GitHub personal access tokens (ghp_*) and server-side (ghs_*)
        ("github_pat",
            new Regex(@"\b(ghp|ghs)_[A-Za-z0-9]{36}\b", RegexOptions.Compiled)),

        // Bearer header values — must precede `jwt` since a JWT used as a bearer token
        // should redact as `bearer_header`, not `jwt`.
        ("bearer_header",
            new Regex(@"\bBearer\s+[A-Za-z0-9._\-+/=]{20,}\b", RegexOptions.Compiled)),

        // JWTs — three base64url segments split by dots; the second starts with eyJ to
        // distinguish from generic dotted identifiers
        ("jwt",
            new Regex(@"\beyJ[A-Za-z0-9_-]+?\.eyJ[A-Za-z0-9_-]+?\.[A-Za-z0-9_-]+\b", RegexOptions.Compiled)),

        // Generic `password=...` / `secret=...` / `api_key=...` / `token=...` forms.
        // Negative lookahead `(?!\[REDACTED:)` prevents this catch-all from overwriting
        // a more-specific marker that an earlier pattern already produced.
        ("password_form",
            new Regex(@"\b(password|secret|apikey|api_key|token)\s*[=:]\s*(?!\[REDACTED:)[^\s,;]+", RegexOptions.Compiled | RegexOptions.IgnoreCase)),
    ];

    public string Redact(string raw)
    {
        if (string.IsNullOrEmpty(raw)) return raw;

        var result = raw;
        foreach (var (tag, pattern) in Patterns)
        {
            result = pattern.Replace(result, $"[REDACTED:{tag}]");
        }
        return result;
    }

    public RedactionResult RedactWithRetention(string raw, double minRetentionRatio)
    {
        if (minRetentionRatio is < 0 or > 1)
            throw new ArgumentOutOfRangeException(nameof(minRetentionRatio), "Must be in [0, 1]");

        var redacted = Redact(raw);
        if (string.IsNullOrEmpty(raw))
            return new RedactionResult(redacted, true);

        var retentionRatio = (double)redacted.Length / raw.Length;
        // Note: redactions can grow the string slightly (the marker text), so this is a
        // soft signal — we only flag clear "almost entirely credentials" cases by
        // counting the unredacted characters that survived.
        var survivedChars = raw.Length - CountReplacedChars(raw, redacted);
        var survivedRatio = (double)survivedChars / raw.Length;
        var acceptable = survivedRatio >= minRetentionRatio;
        return new RedactionResult(redacted, acceptable);
    }

    private static int CountReplacedChars(string raw, string redacted)
    {
        // Approximate: total chars hidden by markers = sum of original match lengths.
        // We recompute matches against the raw text for an honest count.
        var replaced = 0;
        foreach (var (_, pattern) in Patterns)
        {
            foreach (Match match in pattern.Matches(raw))
            {
                replaced += match.Length;
            }
        }
        return replaced;
    }
}
