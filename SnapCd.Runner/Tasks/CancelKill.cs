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
    public Task ApplyCancelKill(CancelKillRequest request, HubConnection connection) =>
        CancelKill(request, connection,
            c => c.InvokeApplyCancelKillCompleted(request.JobId), nameof(RunnerHubClient.InvokeApplyCancelKillCompleted));

    public Task DestroyCancelKill(CancelKillRequest request, HubConnection connection) =>
        CancelKill(request, connection,
            c => c.InvokeDestroyCancelKillCompleted(request.JobId), nameof(RunnerHubClient.InvokeDestroyCancelKillCompleted));

    public Task SplitCancelKill(CancelKillRequest request, HubConnection connection) =>
        CancelKill(request, connection,
            c => c.InvokeSplitCancelKillCompleted(request.JobId), nameof(RunnerHubClient.InvokeSplitCancelKillCompleted));

    public Task MoveCancelKill(CancelKillRequest request, HubConnection connection) =>
        CancelKill(request, connection,
            c => c.InvokeMoveCancelKillCompleted(request.JobId), nameof(RunnerHubClient.InvokeMoveCancelKillCompleted));

    public Task ImportCancelKill(CancelKillRequest request, HubConnection connection) =>
        CancelKill(request, connection,
            c => c.InvokeImportCancelKillCompleted(request.JobId), nameof(RunnerHubClient.InvokeImportCancelKillCompleted));

    public Task RemoveCancelKill(CancelKillRequest request, HubConnection connection) =>
        CancelKill(request, connection,
            c => c.InvokeRemoveCancelKillCompleted(request.JobId), nameof(RunnerHubClient.InvokeRemoveCancelKillCompleted));

    /// <summary>
    /// Killing the process is the same work for every family; the endpoint the outcome is reported
    /// on is not, so the caller hands in the reply to make.
    /// </summary>
    private async Task CancelKill(
        CancelKillRequest request,
        HubConnection connection,
        Func<RunnerHubClient, Task> reportCompleted,
        string reportName)
    {
        var logger = _loggerFactory.CreateLogger<Tasks>();

        logger.LogInformation("Received kill cancellation request for job {JobId}", request.JobId);

        var result = _processRegistry.TryCancel(request.JobId, CancellationType.ImmediateKill);

        if (result)
            logger.LogInformation("Kill cancellation signal sent to running process for job {JobId}", request.JobId);
        else
            logger.LogWarning("No running process found for kill cancellation of job {JobId}", request.JobId);

        var runnerHubClient = new RunnerHubClient(connection);
        await InvokeWithRetryAsync(
            () => reportCompleted(runnerHubClient),
            reportName,
            request.JobId,
            connection);
    }
}