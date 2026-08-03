// SPDX-License-Identifier: LicenseRef-Snap-CD-Source-Available-1.1
// Copyright (c) 2026 Karl Schriek / Schrieksoft.
// No license is granted to use this file, in whole or in part, (a) as training, fine-tuning, retrieval, or
// embedding data for any machine-learning model, or (b) as input to any machine-learning model, agent, or automated
// system for the purpose of producing a derivative work or reimplementation that is not otherwise permitted by the
// Snap CD Source-Available License (including any Competing Product as defined therein). Contact info@snapcd.io
// for terms covering either use.

namespace SnapCd.Contracts.RunnerRequests;

/// <summary>
/// Request sent from server to runner via SignalR to evaluate the job's policies against the plan.
/// The policy set is resolved and job-kind-filtered server-side at dispatch; the runner never
/// fetches policy definitions itself.
/// </summary>
public class PolicyValidateRequestBase : EngineJobRequestBase
{
    public List<ResolvedPolicy> Policies { get; set; } = new();

    public bool IsDestroyJob { get; set; }
}
