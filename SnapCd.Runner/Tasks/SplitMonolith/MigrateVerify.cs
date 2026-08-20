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
    /// <summary>Re-runs the proof against the real backends, after the push.</summary>
    public async Task MigrateVerify(MigrateVerifyRequestBase request, HubConnection connection)
    {
        var killCts = new CancellationTokenSource();
        _processRegistry.Register(request.JobId, killCts, CancellationType.ImmediateKill);

        var gracefulCts = new CancellationTokenSource();
        _processRegistry.Register(request.JobId, gracefulCts, CancellationType.ImmediateGraceful);

        var reportingCts = CancellationTokenSource.CreateLinkedTokenSource(killCts.Token, gracefulCts.Token);
        var reportingTask = StartPeriodicTaskReporting(
            request.JobId,
            nameof(MigrateVerify),
            connection,
            TimeSpan.FromSeconds(request.ReportActiveJobFrequencySeconds),
            reportingCts.Token);

        var logger = _loggerFactory.CreateLogger<Tasks>();
        var taskContext = new RunnerTaskContext(
            request.JobId,
            nameof(MigrateVerify),
            logger,
            _jobLogStream,
            request.Metadata
        );

        var runnerHubClient = new RunnerHubClient(connection);

        try
        {
            taskContext.LogInformation("Now running demonolith migrate verify");

            var engine = _engineFactory.Create(
                taskContext,
                request.Engine,
                request.Metadata
            );

            var command = DemonolithCommand.Build(
                "migrate verify",
                request.RootDirectory,
                request.Engine,
                DemonolithCommand.BackendConfigFlags(request.BackendConfigs).ToArray());

            await engine.RunProcess(command, killCts.Token, gracefulCts.Token);

            // module_states names each module the proof covered; a receipt marked complete means
            // every one of them planned clean, since demonolith fails the run otherwise.
            var receipt = DemonolithReceipt.Read(request.RootDirectory, DemonolithReceipt.VerifyReceiptFile);
            var modulesProven = receipt?.ModuleStates.Count ?? 0;
            var modulesPlanningClean = receipt is { Complete: true } ? modulesProven : 0;


            await InvokeWithRetryAsync(
                () => runnerHubClient.InvokeMigrateVerifyCompleted(request.JobId, modulesProven, modulesPlanningClean),
                nameof(runnerHubClient.InvokeMigrateVerifyCompleted),
                request.JobId,
                connection);

            taskContext.LogInformation("Completed MigrateVerify");
        }
        catch (OperationCanceledException)
        {
            taskContext.LogWarning("MigrateVerify was cancelled.");
            await InvokeWithRetryAsync(
                () => runnerHubClient.InvokeMigrateVerifyCancelled(request.JobId),
                nameof(runnerHubClient.InvokeMigrateVerifyCancelled),
                request.JobId,
                connection);
        }
        catch (Exception ex)
        {
            taskContext.LogError($"Unhandled exception occurred. {ex.Message}");
            logger.LogError(ex, "Error handling MigrateVerify for job {JobId}", request.JobId);
            await InvokeWithRetryAsync(
                () => runnerHubClient.InvokeMigrateVerifyFaulted(request.JobId, ex.Message, ex.StackTrace),
                nameof(runnerHubClient.InvokeMigrateVerifyFaulted),
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
