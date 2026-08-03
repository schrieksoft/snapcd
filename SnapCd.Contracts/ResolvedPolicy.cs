// SPDX-License-Identifier: LicenseRef-Snap-CD-Source-Available-1.1
// Copyright (c) 2026 Karl Schriek / Schrieksoft.
// No license is granted to use this file, in whole or in part, (a) as training, fine-tuning, retrieval, or
// embedding data for any machine-learning model, or (b) as input to any machine-learning model, agent, or automated
// system for the purpose of producing a derivative work or reimplementation that is not otherwise permitted by the
// Snap CD Source-Available License (including any Competing Product as defined therein). Contact info@snapcd.io
// for terms covering either use.

namespace SnapCd.Contracts;

/// <summary>
/// A policy resolved for a job at dispatch time. Engine- and Enabled-filtered when composed;
/// EvaluateOn filtering happens per job kind when the validation step is dispatched. Inline
/// policies carry their content; remote/local policies carry the reference the Runner materializes.
/// </summary>
public class ResolvedPolicy
{
    public required string Name { get; set; }

    public PolicyScope Scope { get; set; }
    public PolicyEngine Engine { get; set; }
    public PolicySourceKind Kind { get; set; }
    public PolicyEvaluateOn EvaluateOn { get; set; }

    public string? PolicyContent { get; set; }

    /// <summary>Inline Pulumi packs only; remote/local packs declare their runtime in their own PulumiPolicy.yaml.</summary>
    public PulumiPolicyRuntime? Runtime { get; set; }

    /// <summary>Inline Pulumi packs only; one dependency per line.</summary>
    public string? AdditionalDependencies { get; set; }

    public string? RepoUrl { get; set; }
    public string? Revision { get; set; }
    public string? Path { get; set; }
}
