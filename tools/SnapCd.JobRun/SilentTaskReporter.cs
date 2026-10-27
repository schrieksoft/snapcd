// SPDX-License-Identifier: LicenseRef-Snap-CD-Source-Available-1.1
// Copyright (c) 2026 Karl Schriek / Schrieksoft.
// No license is granted to use this file, in whole or in part, (a) as training, fine-tuning, retrieval, or
// embedding data for any machine-learning model, or (b) as input to any machine-learning model, agent, or automated
// system for the purpose of producing a derivative work or reimplementation that is not otherwise permitted by the
// Snap CD Source-Available License (including any Competing Product as defined therein). Contact info@snapcd.io
// for terms covering either use.

using Microsoft.AspNetCore.SignalR.Client;
using SnapCd.Runner.Tasks;

namespace SnapCd.JobRun;

/// <summary>
/// Drops the running-task heartbeat. The connection the tasks carry is never started, so the
/// real reporter's invocation cannot reach a server and spends its retry budget waiting for a
/// connection that will not arrive.
/// </summary>
public class SilentTaskReporter : IRunningTaskReporter
{
    public Task Report(Guid jobId, string taskName, HubConnection connection) => Task.CompletedTask;
}
