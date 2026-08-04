// SPDX-License-Identifier: LicenseRef-Snap-CD-Source-Available-1.1
// Copyright (c) 2026 Karl Schriek / Schrieksoft.
// No license is granted to use this file, in whole or in part, (a) as training, fine-tuning, retrieval, or
// embedding data for any machine-learning model, or (b) as input to any machine-learning model, agent, or automated
// system for the purpose of producing a derivative work or reimplementation that is not otherwise permitted by the
// Snap CD Source-Available License (including any Competing Product as defined therein). Contact info@snapcd.io
// for terms covering either use.

namespace SnapCd.Contracts.Dto.NamespacePulumiInlinePolicies;

/// <summary>
/// DTO for reading a NamespacePulumiInlinePolicy (GET operations).
/// </summary>
public class NamespacePulumiInlinePolicyReadDto
{
    /// <summary>Unique ID of the NamespacePulumiInlinePolicy.</summary>
    public Guid Id { get; set; }

    /// <summary>ID of the policy's parent Namespace.</summary>
    public Guid NamespaceId { get; set; }

    /// <summary>Human-readable policy name. Must be unique in combination with `namespace_id`.</summary>
    public string Name { get; set; } = null!;

    /// <summary>Inline CrossGuard policy module (the pack's entry file, e.g. `__main__.py` defining a `PolicyPack`). The Runner synthesizes the surrounding pack scaffold. Policies declare `mandatory` (blocks the job) or `advisory` (warns and continues) enforcement in the pack itself.</summary>
    public string PolicyContent { get; set; } = null!;

    /// <summary>Language runtime of the policy pack. Determines the scaffold the Runner synthesizes around the policy content.</summary>
    public PulumiPolicyRuntime Runtime { get; set; }

    /// <summary>Optional extra package dependencies required by the policy content, one per line (requirements.txt semantics), installed after the pinned policy SDK. Runners configured for operator-managed environments reject entities that set this.</summary>
    public string? AdditionalDependencies { get; set; }

    /// <summary>Whether this policy is evaluated. Defaults to `true`; set `false` to switch the policy off without deleting it.</summary>
    public bool Enabled { get; set; } = true;

    /// <summary>When this policy is evaluated. Only `ApplyOnly` exists for Pulumi policies: CrossGuard evaluates apply-side previews only — the pulumi CLI has no policy support on destroy.</summary>
    public PulumiPolicyEvaluateOn EvaluateOn { get; set; } = PulumiPolicyEvaluateOn.ApplyOnly;
}
