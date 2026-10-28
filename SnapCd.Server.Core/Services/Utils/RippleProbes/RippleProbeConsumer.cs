// SPDX-License-Identifier: LicenseRef-Snap-CD-Source-Available-1.1
// Copyright (c) 2026 Karl Schriek / Schrieksoft.
// No license is granted to use this file, in whole or in part, (a) as training, fine-tuning, retrieval, or
// embedding data for any machine-learning model, or (b) as input to any machine-learning model, agent, or automated
// system for the purpose of producing a derivative work or reimplementation that is not otherwise permitted by the
// Snap CD Source-Available License (including any Competing Product as defined therein). Contact info@snapcd.io
// for terms covering either use.


namespace SnapCd.Server.Core.Services.Utils.RippleProbes;

/// <summary>
/// One Module that consumes a changed output, and which of its own variables that output feeds.
/// A Module appears once however many of its variables are affected.
/// </summary>
public class RippleProbeConsumer
{
    public Guid ModuleId { get; set; }

    /// <summary>
    /// The producer's output name mapped to this Module's own variable name. The two differ for a
    /// per-output edge, which renames; an OutputSet edge carries no rename, so they are equal.
    /// </summary>
    public Dictionary<string, string> VariableNamesByOutputName { get; set; } = new();
}
