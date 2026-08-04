// SPDX-License-Identifier: LicenseRef-Snap-CD-Source-Available-1.1
// Copyright (c) 2026 Karl Schriek / Schrieksoft.
// No license is granted to use this file, in whole or in part, (a) as training, fine-tuning, retrieval, or
// embedding data for any machine-learning model, or (b) as input to any machine-learning model, agent, or automated
// system for the purpose of producing a derivative work or reimplementation that is not otherwise permitted by the
// Snap CD Source-Available License (including any Competing Product as defined therein). Contact info@snapcd.io
// for terms covering either use.

namespace SnapCd.Server.Core.Licensing.Services;

/// <summary>
/// How much of a quota'd resource an organization may use. The three cases are distinct on
/// purpose: an unentitled organization is <see cref="Denied"/>, not "no limit configured".
/// </summary>
public abstract record QuotaAllowance
{
    private QuotaAllowance()
    {
    }

    public static QuotaAllowance Unlimited { get; } = new UnlimitedAllowance();

    public static QuotaAllowance Denied { get; } = new DeniedAllowance();

    public static QuotaAllowance Limited(int limit) => new LimitedAllowance(limit);

    /// <summary>
    /// Whether <paramref name="currentCount"/> has reached the allowance.
    /// </summary>
    public bool IsExceededAt(int currentCount) => this switch
    {
        UnlimitedAllowance => false,
        LimitedAllowance limited => currentCount >= limited.Limit,
        _ => true
    };

    /// <summary>
    /// The numeric limit, or null when unlimited or denied. For display; prefer
    /// <see cref="IsExceededAt"/> for decisions.
    /// </summary>
    public int? LimitOrNull => this is LimitedAllowance limited ? limited.Limit : null;

    public sealed record UnlimitedAllowance : QuotaAllowance;

    public sealed record DeniedAllowance : QuotaAllowance;

    public sealed record LimitedAllowance(int Limit) : QuotaAllowance;
}
