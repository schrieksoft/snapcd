// SPDX-License-Identifier: LicenseRef-Snap-CD-Source-Available-1.1
// Copyright (c) 2026 Karl Schriek / Schrieksoft.
// No license is granted to use this file, in whole or in part, (a) as training, fine-tuning, retrieval, or
// embedding data for any machine-learning model, or (b) as input to any machine-learning model, agent, or automated
// system for the purpose of producing a derivative work or reimplementation that is not otherwise permitted by the
// Snap CD Source-Available License (including any Competing Product as defined therein). Contact info@snapcd.io
// for terms covering either use.

namespace SnapCd.Contracts.Dto.ModulePulumiLocalPolicies;

/// <summary>
/// DTO for creating a new ModulePulumiLocalPolicy (POST operations).
/// </summary>
public class ModulePulumiLocalPolicyCreateDto
{
    /// <summary>ID of the policy's parent Module.</summary>
    public Guid ModuleId { get; set; }

    /// <summary>Human-readable policy name. Must be unique in combination with `module_id`.</summary>
    public string Name { get; set; } = null!;

    /// <summary>Absolute directory path on the Runner host holding the CrossGuard policy pack. Operator-managed: the contents at evaluation time are whatever the folder holds — there is no revision pinning.</summary>
    public string Path { get; set; } = null!;

    /// <summary>Whether this policy is evaluated. Defaults to `true`; set `false` to switch the policy off without deleting it.</summary>
    public bool Enabled { get; set; } = true;

    /// <summary>Which job kinds evaluate this policy: `ApplyAndDestroy` (default), `ApplyOnly` or `DestroyOnly`.</summary>
    public PolicyEvaluateOn EvaluateOn { get; set; } = PolicyEvaluateOn.ApplyAndDestroy;
}
