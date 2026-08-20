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
    /// <summary>Plans every carved module against its local state copy and asserts zero changes.</summary>
    public async Task MigrateProve(MigrateProveRequestBase request, HubConnection connection)
    {
        var killCts = new CancellationTokenSource();
        _processRegistry.Register(request.JobId, killCts, CancellationType.ImmediateKill);

        var gracefulCts = new CancellationTokenSource();
        _processRegistry.Register(request.JobId, gracefulCts, CancellationType.ImmediateGraceful);

        var reportingCts = CancellationTokenSource.CreateLinkedTokenSource(killCts.Token, gracefulCts.Token);
        var reportingTask = StartPeriodicTaskReporting(
            request.JobId,
            nameof(MigrateProve),
            connection,
            TimeSpan.FromSeconds(request.ReportActiveJobFrequencySeconds),
            reportingCts.Token);

        var logger = _loggerFactory.CreateLogger<Tasks>();
        var taskContext = new RunnerTaskContext(
            request.JobId,
            nameof(MigrateProve),
            logger,
            _jobLogStream,
            request.Metadata
        );

        var runnerHubClient = new RunnerHubClient(connection);

        try
        {
            taskContext.LogInformation("Now running demonolith migrate prove");

            var engine = _engineFactory.Create(
                taskContext,
                request.Engine,
                request.Metadata
            );

            var command = DemonolithCommand.Build(
                "migrate prove",
                request.RootDirectory,
                request.Engine,
                // Unrefreshed, a proof only shows the state was carved correctly and cannot see
                // drift. This is the evidence an approver reads before an irreversible push, and
                // the runner has the credentials the refresh needs.
                "--refresh");

            var output = await engine.RunProcess(command, killCts.Token, gracefulCts.Token);

            var modulesProven = DemonolithOutput.ReadInt(output, "modulesProven");
            var modulesPlanningClean = DemonolithOutput.ReadInt(output, "modulesPlanningClean");

            await InvokeWithRetryAsync(
                () => runnerHubClient.InvokeMigrateProveCompleted(request.JobId, modulesProven, modulesPlanningClean),
                nameof(runnerHubClient.InvokeMigrateProveCompleted),
                request.JobId,
                connection);

            taskContext.LogInformation("Completed MigrateProve");
        }
        catch (OperationCanceledException)
        {
            taskContext.LogWarning("MigrateProve was cancelled.");
            await InvokeWithRetryAsync(
                () => runnerHubClient.InvokeMigrateProveCancelled(request.JobId),
                nameof(runnerHubClient.InvokeMigrateProveCancelled),
                request.JobId,
                connection);
        }
        catch (Exception ex)
        {
            taskContext.LogError($"Unhandled exception occurred. {ex.Message}");
            logger.LogError(ex, "Error handling MigrateProve for job {JobId}", request.JobId);
            await InvokeWithRetryAsync(
                () => runnerHubClient.InvokeMigrateProveFaulted(request.JobId, ex.Message, ex.StackTrace),
                nameof(runnerHubClient.InvokeMigrateProveFaulted),
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
