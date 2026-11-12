// SPDX-License-Identifier: LicenseRef-Snap-CD-Source-Available-1.1
// Copyright (c) 2026 Karl Schriek / Schrieksoft.
// No license is granted to use this file, in whole or in part, (a) as training, fine-tuning, retrieval, or
// embedding data for any machine-learning model, or (b) as input to any machine-learning model, agent, or automated
// system for the purpose of producing a derivative work or reimplementation that is not otherwise permitted by the
// Snap CD Source-Available License (including any Competing Product as defined therein). Contact info@snapcd.io
// for terms covering either use.

using SnapCd.Contracts.RunnerRequests;

namespace SnapCd.Contracts.Endpoints;

/// <summary>
/// What a runner answers outside any job: the liveness ping and a source refresh.
///
/// The server dispatches by member name and the runner implements the interface, so a step
/// the server can send is one the runner has a handler for.
/// </summary>
public interface IRunnerLifecycleEndpoints
{
    Task Ping(Guid pingId);

    /// <summary>
    /// Report every job still running here. The server asks on a reconnect, where it knows what it
    /// dispatched to this instance but not what survived: a dropped connection and a restarted
    /// runner look the same to it, and only the runner knows which happened.
    /// </summary>
    Task ReportRunningJobs();

    Task SourceRefresh(SourceRefreshRequest request);
}
