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
    /// <summary>Pushes each module's state to its derived backend. The job's only irreversible step.</summary>
    public async Task MigrateRun(MigrateRunRequestBase request, HubConnection connection)
    {
        var killCts = new CancellationTokenSource();
        _processRegistry.Register(request.JobId, killCts, CancellationType.ImmediateKill);

        var gracefulCts = new CancellationTokenSource();
        _processRegistry.Register(request.JobId, gracefulCts, CancellationType.ImmediateGraceful);

        var reportingCts = CancellationTokenSource.CreateLinkedTokenSource(killCts.Token, gracefulCts.Token);
        var reportingTask = StartPeriodicTaskReporting(
            request.JobId,
            nameof(MigrateRun),
            connection,
            TimeSpan.FromSeconds(request.ReportActiveJobFrequencySeconds),
            reportingCts.Token);

        var logger = _loggerFactory.CreateLogger<Tasks>();
        var taskContext = new RunnerTaskContext(
            request.JobId,
            nameof(MigrateRun),
            logger,
            _jobLogStream,
            request.Metadata
        );

        var runnerHubClient = new RunnerHubClient(connection);

        try
        {
            taskContext.LogInformation("Now running demonolith migrate run");

            var engine = _engineFactory.Create(
                taskContext,
                request.Engine,
                request.Metadata
            );

            var command = DemonolithCommand.Build(
                "migrate run",
                request.RootDirectory,
                request.Engine,
                DemonolithCommand.BackendConfigFlags(request.BackendConfigs).ToArray());
            if (request.Force) command += " --force";

            await engine.RunProcess(command, killCts.Token, gracefulCts.Token);

            await InvokeWithRetryAsync(
                () => runnerHubClient.InvokeMigrateRunCompleted(request.JobId),
                nameof(runnerHubClient.InvokeMigrateRunCompleted),
                request.JobId,
                connection);

            taskContext.LogInformation("Completed MigrateRun");
        }
        catch (OperationCanceledException)
        {
            taskContext.LogWarning("MigrateRun was cancelled.");
            await InvokeWithRetryAsync(
                () => runnerHubClient.InvokeMigrateRunCancelled(request.JobId),
                nameof(runnerHubClient.InvokeMigrateRunCancelled),
                request.JobId,
                connection);
        }
        catch (Exception ex)
        {
            taskContext.LogError($"Unhandled exception occurred. {ex.Message}");
            logger.LogError(ex, "Error handling MigrateRun for job {JobId}", request.JobId);
            await InvokeWithRetryAsync(
                () => runnerHubClient.InvokeMigrateRunFaulted(request.JobId, ex.Message, ex.StackTrace),
                nameof(runnerHubClient.InvokeMigrateRunFaulted),
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
