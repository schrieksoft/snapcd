// SPDX-License-Identifier: LicenseRef-Snap-CD-Source-Available-1.1
// Copyright (c) 2026 Karl Schriek / Schrieksoft.
// No license is granted to use this file, in whole or in part, (a) as training, fine-tuning, retrieval, or
// embedding data for any machine-learning model, or (b) as input to any machine-learning model, agent, or automated
// system for the purpose of producing a derivative work or reimplementation that is not otherwise permitted by the
// Snap CD Source-Available License (including any Competing Product as defined therein). Contact info@snapcd.io
// for terms covering either use.

namespace SnapCd.Contracts.Dto.ModuleTerraformInlinePolicies;

/// <summary>
/// DTO for reading a ModuleTerraformInlinePolicy (GET operations).
/// </summary>
public class ModuleTerraformInlinePolicyReadDto
{
    /// <summary>Unique ID of the ModuleTerraformInlinePolicy.</summary>
    public Guid Id { get; set; }

    /// <summary>ID of the policy's parent Module.</summary>
    public Guid ModuleId { get; set; }

    /// <summary>Human-readable policy name. Must be unique in combination with `module_id`.</summary>
    public string Name { get; set; } = null!;

    /// <summary>Inline OPA/Rego policy document evaluated with conftest against the JSON export of the plan. Severity is carried by rule names: `deny`/`violation` rules block the job, `warn` rules log a warning and continue. Any package name is accepted (all namespaces are evaluated).</summary>
    public string PolicyContent { get; set; } = null!;

    /// <summary>Whether this policy is evaluated. Defaults to `true`; set `false` to switch the policy off without deleting it.</summary>
    public bool Enabled { get; set; } = true;

    /// <summary>Which job kinds evaluate this policy: `ApplyAndDestroy` (default), `ApplyOnly` or `DestroyOnly`.</summary>
    public PolicyEvaluateOn EvaluateOn { get; set; } = PolicyEvaluateOn.ApplyAndDestroy;
}
