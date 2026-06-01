// SPDX-License-Identifier: LicenseRef-Snap-CD-Source-Available-1.1
// Copyright (c) 2026 Karl Schriek / Schrieksoft.
// No license is granted to use this file, in whole or in part, (a) as training, fine-tuning, retrieval, or
// embedding data for any machine-learning model, or (b) as input to any machine-learning model, agent, or automated
// system for the purpose of producing a derivative work or reimplementation that is not otherwise permitted by the
// Snap CD Source-Available License (including any Competing Product as defined therein). Contact info@snapcd.io
// for terms covering either use.

using SnapCd.Contracts.RunnerRequests.HelperClasses;

namespace SnapCd.Contracts.RunnerRequests;

/// <summary>
/// Request sent from server to runner via SignalR to create a Terraform destroy plan.
/// PlanDestroy now receives pre-resolved Terraform variables from the server.
/// </summary>
public class PlanDestroyRequestBase : EngineJobRequestBase
{
    public string? PlanDestroyBeforeHook { get; set; }
    public string? PlanDestroyAfterHook { get; set; }

    public Dictionary<string, string> ResolvedParameters { get; set; } = null!;

    public List<PulumiFlagEntry> PulumiFlags { get; set; } = [];
    public List<PulumiArrayFlagEntry> PulumiArrayFlags { get; set; } = [];

    public List<TerraformFlagEntry> TerraformFlags { get; set; } = [];
    public List<TerraformArrayFlagEntry> TerraformArrayFlags { get; set; } = [];
}
