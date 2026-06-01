// SPDX-License-Identifier: LicenseRef-Snap-CD-Source-Available-1.1
// Copyright (c) 2026 Karl Schriek / Schrieksoft.
// No license is granted to use this file, in whole or in part, (a) as training, fine-tuning, retrieval, or
// embedding data for any machine-learning model, or (b) as input to any machine-learning model, agent, or automated
// system for the purpose of producing a derivative work or reimplementation that is not otherwise permitted by the
// Snap CD Source-Available License (including any Competing Product as defined therein). Contact info@snapcd.io
// for terms covering either use.

using Microsoft.AspNetCore.SignalR.Client;
using SnapCd.Contracts;
using SnapCd.Contracts.Clients;
using SnapCd.Contracts.RunnerRequests;

namespace SnapCd.Runner.Tasks;

public partial class Tasks
{
    public async Task CancelGraceful(CancelGracefulRequest request, HubConnection connection)
    {
        var logger = _loggerFactory.CreateLogger<Tasks>();

        logger.LogInformation("Received graceful cancellation request for job {JobId}", request.JobId);

        var result = _processRegistry.TryCancel(request.JobId, CancellationType.ImmediateGraceful);

        if (result)
            logger.LogInformation("Graceful cancellation signal sent to running process for job {JobId}", request.JobId);
        else
            logger.LogWarning("No running process found for graceful cancellation of job {JobId}", request.JobId);

        // Send completion response to server
        var runnerHubClient = new RunnerHubClient(connection);
        await InvokeWithRetryAsync(
            () => runnerHubClient.InvokeCancelGracefulCompleted(request.JobId),
            nameof(runnerHubClient.InvokeCancelGracefulCompleted),
            request.JobId,
            connection);
    }
}