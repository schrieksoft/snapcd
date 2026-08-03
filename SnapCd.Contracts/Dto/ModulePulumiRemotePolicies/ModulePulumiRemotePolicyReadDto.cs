// SPDX-License-Identifier: LicenseRef-Snap-CD-Source-Available-1.1
// Copyright (c) 2026 Karl Schriek / Schrieksoft.
// No license is granted to use this file, in whole or in part, (a) as training, fine-tuning, retrieval, or
// embedding data for any machine-learning model, or (b) as input to any machine-learning model, agent, or automated
// system for the purpose of producing a derivative work or reimplementation that is not otherwise permitted by the
// Snap CD Source-Available License (including any Competing Product as defined therein). Contact info@snapcd.io
// for terms covering either use.

namespace SnapCd.Contracts.Dto.ModulePulumiRemotePolicies;

/// <summary>
/// DTO for reading a ModulePulumiRemotePolicy (GET operations).
/// </summary>
public class ModulePulumiRemotePolicyReadDto
{
    /// <summary>Unique ID of the ModulePulumiRemotePolicy.</summary>
    public Guid Id { get; set; }

    /// <summary>ID of the policy's parent Module.</summary>
    public Guid ModuleId { get; set; }

    /// <summary>Human-readable policy name. Must be unique in combination with `module_id`.</summary>
    public string Name { get; set; } = null!;

    /// <summary>URL of the git repository holding the CrossGuard policy pack.</summary>
    public string RepoUrl { get; set; } = null!;

    /// <summary>Git revision (tag, branch or commit SHA) to evaluate. The revision is resolved at job dispatch, pinning the evaluated policy pack.</summary>
    public string Revision { get; set; } = null!;

    /// <summary>Repo-root-relative directory of the policy pack within the repository. Empty means the repository root.</summary>
    public string Path { get; set; } = null!;

    /// <summary>Whether this policy is evaluated. Defaults to `true`; set `false` to switch the policy off without deleting it.</summary>
    public bool Enabled { get; set; } = true;

    /// <summary>When this policy is evaluated. Only `ApplyOnly` exists for Pulumi policies: CrossGuard evaluates apply-side previews only — the pulumi CLI has no policy support on destroy.</summary>
    public PulumiPolicyEvaluateOn EvaluateOn { get; set; } = PulumiPolicyEvaluateOn.ApplyOnly;
}
