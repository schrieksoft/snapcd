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
using SnapCd.Contracts.RunnerRequests.HelperClasses;

namespace SnapCd.Runner.Tasks;

public partial class Tasks
{
    /// <summary>
    /// The scaffolding every transfer step shares: cancellation registration, periodic reporting,
    /// and turning an unhandled exception into the step's own faulted reply.
    /// </summary>
    private async Task RunTransferStep(
        Guid jobId,
        Guid moduleId,
        string task,
        JobMetadata metadata,
        int reportFrequencySeconds,
        HubConnection connection,
        Func<RunnerTaskContext, RunnerHubClient, CancellationToken, CancellationToken, Task> run,
        Func<RunnerHubClient, string?, string?, Task> fault)
    {
        var killCts = new CancellationTokenSource();
        _processRegistry.Register(jobId, killCts, CancellationType.ImmediateKill);

        var gracefulCts = new CancellationTokenSource();
        _processRegistry.Register(jobId, gracefulCts, CancellationType.ImmediateGraceful);

        var reportingCts = CancellationTokenSource.CreateLinkedTokenSource(killCts.Token, gracefulCts.Token);
        var reportingTask = StartPeriodicTaskReporting(
            jobId, task, connection, TimeSpan.FromSeconds(reportFrequencySeconds), reportingCts.Token);

        var logger = _loggerFactory.CreateLogger<Tasks>();
        var taskContext = new RunnerTaskContext(jobId, task, logger, _jobLogStream, metadata);
        var runnerHubClient = new RunnerHubClient(connection);

        try
        {
            await run(taskContext, runnerHubClient, killCts.Token, gracefulCts.Token);
            taskContext.LogSection($"Completed {task}");
        }
        catch (OperationCanceledException)
        {
            taskContext.LogWarning($"{task} was cancelled.");
            await InvokeWithRetryAsync(
                () => fault(runnerHubClient, "Cancelled.", null), $"{task}Faulted", jobId, connection);
        }
        catch (Exception ex)
        {
            taskContext.LogError($"Unhandled exception occurred. {ex.Message}");
            logger.LogError(ex, "Error handling {Task} for Module {ModuleId} of job {JobId}", task, moduleId, jobId);

            await InvokeWithRetryAsync(
                () => fault(runnerHubClient, ex.Message, ex.StackTrace), $"{task}Faulted", jobId, connection);
        }
        finally
        {
            reportingCts.Cancel();
            try { await reportingTask; }
            catch { /* Already logged */ }

            _processRegistry.Remove(jobId, CancellationType.ImmediateKill);
            _processRegistry.Remove(jobId, CancellationType.ImmediateGraceful);
        }
    }
}
