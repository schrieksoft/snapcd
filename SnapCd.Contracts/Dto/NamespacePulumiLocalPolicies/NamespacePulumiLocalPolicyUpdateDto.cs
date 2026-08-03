// SPDX-License-Identifier: LicenseRef-Snap-CD-Source-Available-1.1
// Copyright (c) 2026 Karl Schriek / Schrieksoft.
// No license is granted to use this file, in whole or in part, (a) as training, fine-tuning, retrieval, or
// embedding data for any machine-learning model, or (b) as input to any machine-learning model, agent, or automated
// system for the purpose of producing a derivative work or reimplementation that is not otherwise permitted by the
// Snap CD Source-Available License (including any Competing Product as defined therein). Contact info@snapcd.io
// for terms covering either use.

namespace SnapCd.Contracts.Dto.NamespacePulumiLocalPolicies;

/// <summary>
/// DTO for updating an existing NamespacePulumiLocalPolicy (PUT operations).
/// </summary>
public class NamespacePulumiLocalPolicyUpdateDto
{
    /// <summary>ID of the policy's parent Namespace.</summary>
    public Guid NamespaceId { get; set; }

    /// <summary>Human-readable policy name. Must be unique in combination with `namespace_id`.</summary>
    public string Name { get; set; } = null!;

    /// <summary>Absolute directory path on the Runner host holding the CrossGuard policy pack. Operator-managed: the contents at evaluation time are whatever the folder holds — there is no revision pinning.</summary>
    public string Path { get; set; } = null!;

    /// <summary>Whether this policy is evaluated. Defaults to `true`; set `false` to switch the policy off without deleting it.</summary>
    public bool Enabled { get; set; } = true;

    /// <summary>When this policy is evaluated. Only `ApplyOnly` exists for Pulumi policies: CrossGuard evaluates apply-side previews only — the pulumi CLI has no policy support on destroy.</summary>
    public PulumiPolicyEvaluateOn EvaluateOn { get; set; } = PulumiPolicyEvaluateOn.ApplyOnly;
}
