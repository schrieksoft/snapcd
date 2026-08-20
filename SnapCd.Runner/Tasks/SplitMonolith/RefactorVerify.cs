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
using SnapCd.Contracts.RunnerRequests.SplitMonolith;
using SnapCd.Runner.Services.SplitMonolith;

namespace SnapCd.Runner.Tasks;

public partial class Tasks
{
    /// <summary>
    /// Asserts the committed module directories still match the source. Offline and credential-free.
    /// This is a checksum comparison only: whether the engine accepts each carved module is a
    /// separate question, and will get its own step rather than a flag on this one.
    /// </summary>
    public async Task RefactorVerify(RefactorVerifyRequestBase request, HubConnection connection)
    {
        var killCts = new CancellationTokenSource();
        _processRegistry.Register(request.JobId, killCts, CancellationType.ImmediateKill);

        var gracefulCts = new CancellationTokenSource();
        _processRegistry.Register(request.JobId, gracefulCts, CancellationType.ImmediateGraceful);

        var reportingCts = CancellationTokenSource.CreateLinkedTokenSource(killCts.Token, gracefulCts.Token);
        var reportingTask = StartPeriodicTaskReporting(
            request.JobId,
            nameof(RefactorVerify),
            connection,
            TimeSpan.FromSeconds(request.ReportActiveJobFrequencySeconds),
            reportingCts.Token);

        var logger = _loggerFactory.CreateLogger<Tasks>();
        var taskContext = new RunnerTaskContext(
            request.JobId,
            nameof(RefactorVerify),
            logger,
            _jobLogStream,
            request.Metadata
        );

        var runnerHubClient = new RunnerHubClient(connection);

        try
        {
            taskContext.LogInformation("Now running demonolith refactor verify");

            var engine = _engineFactory.Create(
                taskContext,
                request.Engine,
                request.Metadata
            );

            // refactor verify takes no --engine: it compares files without asking the engine.
            var command = DemonolithCommand.Build("refactor verify", request.RootDirectory, engine: null);

            await engine.RunProcess(command, killCts.Token, gracefulCts.Token);

            await InvokeWithRetryAsync(
                () => runnerHubClient.InvokeRefactorVerifyCompleted(request.JobId),
                nameof(runnerHubClient.InvokeRefactorVerifyCompleted),
                request.JobId,
                connection);

            taskContext.LogInformation("Completed RefactorVerify");
        }
        catch (OperationCanceledException)
        {
            taskContext.LogWarning("RefactorVerify was cancelled.");
            await InvokeWithRetryAsync(
                () => runnerHubClient.InvokeRefactorVerifyCancelled(request.JobId),
                nameof(runnerHubClient.InvokeRefactorVerifyCancelled),
                request.JobId,
                connection);
        }
        catch (Exception ex)
        {
            taskContext.LogError($"Unhandled exception occurred. {ex.Message}");
            logger.LogError(ex, "Error handling RefactorVerify for job {JobId}", request.JobId);
            await InvokeWithRetryAsync(
                () => runnerHubClient.InvokeRefactorVerifyFaulted(request.JobId, ex.Message, ex.StackTrace),
                nameof(runnerHubClient.InvokeRefactorVerifyFaulted),
                request.JobId,
                connection);
        }
        finally
        {
            reportingCts?.Cancel();
            if (reportingTask != null)
            {
                try { await reportingTask; }
                catch { /* Already logged */ }
            }

            _processRegistry.Remove(request.JobId, CancellationType.ImmediateKill);
            _processRegistry.Remove(request.JobId, CancellationType.ImmediateGraceful);
        }
    }
}
