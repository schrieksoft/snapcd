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
    /// Asks the engine whether each written module directory is valid. Credential-free — it inits
    /// without a backend and validates — so it fails before any state is pulled.
    /// </summary>
    public async Task RefactorValidate(RefactorValidateRequestBase request, HubConnection connection)
    {
        var killCts = new CancellationTokenSource();
        _processRegistry.Register(request.JobId, killCts, CancellationType.ImmediateKill);

        var gracefulCts = new CancellationTokenSource();
        _processRegistry.Register(request.JobId, gracefulCts, CancellationType.ImmediateGraceful);

        var reportingCts = CancellationTokenSource.CreateLinkedTokenSource(killCts.Token, gracefulCts.Token);
        var reportingTask = StartPeriodicTaskReporting(
            request.JobId,
            nameof(RefactorValidate),
            connection,
            TimeSpan.FromSeconds(request.ReportActiveJobFrequencySeconds),
            reportingCts.Token);

        var logger = _loggerFactory.CreateLogger<Tasks>();
        var taskContext = new RunnerTaskContext(
            request.JobId,
            nameof(RefactorValidate),
            logger,
            _jobLogStream,
            request.Metadata
        );

        var runnerHubClient = new RunnerHubClient(connection);

        try
        {
            taskContext.LogInformation("Now running demonolith refactor validate");

            var engine = _engineFactory.Create(
                taskContext,
                request.Engine,
                request.Metadata
            );

            var command = DemonolithCommand.Build("refactor validate", request.RootDirectory, request.Engine);

            await engine.RunProcess(command, killCts.Token, gracefulCts.Token);

            await InvokeWithRetryAsync(
                () => runnerHubClient.InvokeRefactorValidateCompleted(request.JobId),
                nameof(runnerHubClient.InvokeRefactorValidateCompleted),
                request.JobId,
                connection);

            taskContext.LogInformation("Completed RefactorValidate");
        }
        catch (OperationCanceledException)
        {
            taskContext.LogWarning("RefactorValidate was cancelled.");
            await InvokeWithRetryAsync(
                () => runnerHubClient.InvokeRefactorValidateCancelled(request.JobId),
                nameof(runnerHubClient.InvokeRefactorValidateCancelled),
                request.JobId,
                connection);
        }
        catch (Exception ex)
        {
            taskContext.LogError($"Unhandled exception occurred. {ex.Message}");
            logger.LogError(ex, "Error handling RefactorValidate for job {JobId}", request.JobId);
            await InvokeWithRetryAsync(
                () => runnerHubClient.InvokeRefactorValidateFaulted(request.JobId, ex.Message, ex.StackTrace),
                nameof(runnerHubClient.InvokeRefactorValidateFaulted),
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
