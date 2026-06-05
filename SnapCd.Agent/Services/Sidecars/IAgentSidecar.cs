// SPDX-License-Identifier: LicenseRef-Snap-CD-Source-Available-1.1
// Copyright (c) 2026 Karl Schriek / Schrieksoft.
// No license is granted to use this file, in whole or in part, (a) as training, fine-tuning, retrieval, or
// embedding data for any machine-learning model, or (b) as input to any machine-learning model, agent, or automated
// system for the purpose of producing a derivative work or reimplementation that is not otherwise permitted by the
// Snap CD Source-Available License (including any Competing Product as defined therein). Contact info@snapcd.io
// for terms covering either use.

using SnapCd.Agent.Models;

namespace SnapCd.Agent.Services.Sidecars;

/// <summary>
/// A provider-specific AI sidecar (e.g. claude-sidecar). The orchestrator is provider-agnostic and
/// only talks to sidecars through this contract.
/// </summary>
public interface IAgentSidecar
{
    string Name { get; }

    /// <summary>Invokes the sidecar's <c>POST /invoke</c> and streams the run back as it happens
    /// (log lines, then a final result event).</summary>
    IAsyncEnumerable<SidecarStreamEvent> InvokeStreamAsync(InvokeRequest request, CancellationToken cancellationToken);

    Task<bool> IsHealthyAsync(CancellationToken cancellationToken);
}
