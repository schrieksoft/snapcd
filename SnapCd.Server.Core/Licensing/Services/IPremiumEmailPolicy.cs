// SPDX-License-Identifier: LicenseRef-Snap-CD-Source-Available-1.1
// Copyright (c) 2026 Karl Schriek / Schrieksoft.
// No license is granted to use this file, in whole or in part, (a) as training, fine-tuning, retrieval, or
// embedding data for any machine-learning model, or (b) as input to any machine-learning model, agent, or automated
// system for the purpose of producing a derivative work or reimplementation that is not otherwise permitted by the
// Snap CD Source-Available License (including any Competing Product as defined therein). Contact info@snapcd.io
// for terms covering either use.

namespace SnapCd.Server.Core.Licensing.Services;

/// <summary>
/// Answers whether the running deployment may use a premium (non-NoOp) email sender.
/// Self-Hosted checks the licence tier; SaaS always returns true.
/// </summary>
public interface IPremiumEmailPolicy
{
    Task<bool> IsAllowedAsync(CancellationToken ct = default);
}
