// SPDX-License-Identifier: LicenseRef-Snap-CD-Source-Available-1.1
// Copyright (c) 2026 Karl Schriek / Schrieksoft.
// No license is granted to use this file, in whole or in part, (a) as training, fine-tuning, retrieval, or
// embedding data for any machine-learning model, or (b) as input to any machine-learning model, agent, or automated
// system for the purpose of producing a derivative work or reimplementation that is not otherwise permitted by the
// Snap CD Source-Available License (including any Competing Product as defined therein). Contact info@snapcd.io
// for terms covering either use.

using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.Extensions.Options;
using SnapCd.Contracts.Clients;
using SnapCd.Runner.Settings;

namespace SnapCd.Runner.Tasks;

/// <summary>
/// Reports a running task over the runner's hub connection.
/// </summary>
public class RunningTaskReporter(IOptions<RunnerSettings> settings) : IRunningTaskReporter
{
    private readonly RunnerSettings _settings = settings.Value;

    public Task Report(Guid jobId, string taskName, HubConnection connection) =>
        new RunnerHubClient(connection).InvokeReportRunningTask(
            jobId, taskName, _settings.Id, _settings.Instance);
}
