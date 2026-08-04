// SPDX-License-Identifier: LicenseRef-Snap-CD-Source-Available-1.1
// Copyright (c) 2026 Karl Schriek / Schrieksoft.
// No license is granted to use this file, in whole or in part, (a) as training, fine-tuning, retrieval, or
// embedding data for any machine-learning model, or (b) as input to any machine-learning model, agent, or automated
// system for the purpose of producing a derivative work or reimplementation that is not otherwise permitted by the
// Snap CD Source-Available License (including any Competing Product as defined therein). Contact info@snapcd.io
// for terms covering either use.

using SnapCd.Contracts;

namespace SnapCd.Runner.Services.PolicyEvaluation;

/// <summary>
/// Classifies CrossGuard policy results from pulumi preview output. Violations print in a
/// `Policies:` block with per-violation `[mandatory]` / `[advisory]` level markers; mandatory
/// violations additionally fail the preview itself.
/// </summary>
public static class PulumiPolicyOutputParser
{
    public static PolicyOutcome Classify(string output)
    {
        var hasMandatory = output.Contains("[mandatory]");
        var hasAdvisory = output.Contains("[advisory]");

        if (hasMandatory) return PolicyOutcome.HardDenied;
        if (hasAdvisory) return PolicyOutcome.SoftWarned;
        return PolicyOutcome.Passed;
    }
}
