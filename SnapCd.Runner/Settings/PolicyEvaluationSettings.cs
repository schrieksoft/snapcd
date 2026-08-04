// SPDX-License-Identifier: LicenseRef-Snap-CD-Source-Available-1.1
// Copyright (c) 2026 Karl Schriek / Schrieksoft.
// No license is granted to use this file, in whole or in part, (a) as training, fine-tuning, retrieval, or
// embedding data for any machine-learning model, or (b) as input to any machine-learning model, agent, or automated
// system for the purpose of producing a derivative work or reimplementation that is not otherwise permitted by the
// Snap CD Source-Available License (including any Competing Product as defined therein). Contact info@snapcd.io
// for terms covering either use.

namespace SnapCd.Runner.Settings;

/// <summary>
/// Settings for policy evaluation (conftest) on the Runner.
/// </summary>
public class PolicyEvaluationSettings
{
    /// <summary>Explicit path to the conftest binary. When unset, "conftest" is resolved from PATH — the binary is operator-provided, like the engine binaries.</summary>
    public string? ConftestBinaryPath { get; set; }

    /// <summary>Hard timeout for a single policy-entity evaluation. OPA has no built-in evaluation timeout, so pathological policies are bounded here.</summary>
    public int EvaluationTimeoutSeconds { get; set; } = 300;

    /// <summary>Timeout for materializing a remote policy source (resolving and fetching the pinned revision).</summary>
    public int MaterializeTimeoutSeconds { get; set; } = 300;

    /// <summary>When true (default) the Runner provisions environments for inline CrossGuard packs (cached virtualenvs, npm installs). When false the packs run on the ambient interpreter with the policy SDK preinstalled by the operator, and inline policies declaring AdditionalDependencies fail loudly.</summary>
    public bool PackProvisioningEnabled { get; set; } = true;

    /// <summary>Python interpreter used to create virtualenvs for inline CrossGuard packs. The interpreter itself is always operator-provided.</summary>
    public string PythonPath { get; set; } = "python3";

    /// <summary>npm binary used to install dependencies for inline NodeJS CrossGuard packs.</summary>
    public string NpmPath { get; set; } = "npm";

    /// <summary>Pinned pulumi-policy SDK requirement written into synthesized inline-pack requirements.</summary>
    public string PulumiPolicySdkRequirement { get; set; } = "pulumi-policy>=1.5.0,<2.0.0";

    /// <summary>Pinned @pulumi/policy version written into synthesized inline NodeJS packs.</summary>
    public string NodePolicySdkRequirement { get; set; } = "^1.0.0";

    /// <summary>Root directory for cached inline-pack virtualenvs (keyed by requirements hash). Defaults to ~/.snapcd/policy-venvs.</summary>
    public string? VenvCacheRoot { get; set; }
}
