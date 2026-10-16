// SPDX-License-Identifier: LicenseRef-Snap-CD-Source-Available-1.1
// Copyright (c) 2026 Karl Schriek / Schrieksoft.
// No license is granted to use this file, in whole or in part, (a) as training, fine-tuning, retrieval, or
// embedding data for any machine-learning model, or (b) as input to any machine-learning model, agent, or automated
// system for the purpose of producing a derivative work or reimplementation that is not otherwise permitted by the
// Snap CD Source-Available License (including any Competing Product as defined therein). Contact info@snapcd.io
// for terms covering either use.

namespace SnapCd.Contracts.RunnerRequests;

/// <summary>
/// Request sent from server to runner to probe the downstream effect of an upstream's changed
/// outputs, in the checkout this Module's own job is already holding.
/// </summary>
public class ProbeRequestBase : EngineJobRequestBase
{
    /// <summary>
    /// The values to plan with, keyed by this Module's own variable names. Carried as encoded text
    /// the whole way: an unknown has no JSON representation, so only planwalker decodes it.
    /// </summary>
    public string InputsJson { get; set; } = null!;
}
