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
    /// <summary>Pulls the monolith's state read-only and splits local copies, writing a plan receipt on the runner.</summary>
    public async Task MigrateMap(MigrateMapRequestBase request, HubConnection connection)
    {
        var killCts = new CancellationTokenSource();
        _processRegistry.Register(request.JobId, killCts, CancellationType.ImmediateKill);

        var gracefulCts = new CancellationTokenSource();
        _processRegistry.Register(request.JobId, gracefulCts, CancellationType.ImmediateGraceful);

        var reportingCts = CancellationTokenSource.CreateLinkedTokenSource(killCts.Token, gracefulCts.Token);
        var reportingTask = StartPeriodicTaskReporting(
            request.JobId,
            nameof(MigrateMap),
            connection,
            TimeSpan.FromSeconds(request.ReportActiveJobFrequencySeconds),
            reportingCts.Token);

        var logger = _loggerFactory.CreateLogger<Tasks>();
        var taskContext = new RunnerTaskContext(
            request.JobId,
            nameof(MigrateMap),
            logger,
            _jobLogStream,
            request.Metadata
        );

        var runnerHubClient = new RunnerHubClient(connection);

        try
        {
            taskContext.LogInformation("Now running demonolith migrate map");

            var engine = _engineFactory.Create(
                taskContext,
                request.Engine,
                request.Metadata
            );

            var command = DemonolithCommand.Build("migrate map", request.RootDirectory, request.Engine);

            var output = await engine.RunProcess(command, killCts.Token, gracefulCts.Token);

            // Receipts stay on the runner: only the shape of the carve crosses back.
            var refactorMapHash = DemonolithOutput.ReadString(output, "refactorMapHash");
            var carvedModuleNames = DemonolithOutput.ReadStringList(output, "modules");
            var resourcesMoved = DemonolithOutput.ReadInt(output, "resourcesMoved");

            await InvokeWithRetryAsync(
                () => runnerHubClient.InvokeMigrateMapCompleted(request.JobId, refactorMapHash, carvedModuleNames, resourcesMoved),
                nameof(runnerHubClient.InvokeMigrateMapCompleted),
                request.JobId,
                connection);

            taskContext.LogInformation("Completed MigrateMap");
        }
        catch (OperationCanceledException)
        {
            taskContext.LogWarning("MigrateMap was cancelled.");
            await InvokeWithRetryAsync(
                () => runnerHubClient.InvokeMigrateMapCancelled(request.JobId),
                nameof(runnerHubClient.InvokeMigrateMapCancelled),
                request.JobId,
                connection);
        }
        catch (Exception ex)
        {
            taskContext.LogError($"Unhandled exception occurred. {ex.Message}");
            logger.LogError(ex, "Error handling MigrateMap for job {JobId}", request.JobId);
            await InvokeWithRetryAsync(
                () => runnerHubClient.InvokeMigrateMapFaulted(request.JobId, ex.Message, ex.StackTrace),
                nameof(runnerHubClient.InvokeMigrateMapFaulted),
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
