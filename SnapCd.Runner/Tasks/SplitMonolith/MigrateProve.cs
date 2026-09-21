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
            taskContext.LogNarration("Now running demonolith split migrate prove");

            var engine = _engineFactory.Create(
                taskContext,
                request.Engine,
                request.Metadata
            );

            // The proof plans against the local state copies; demonolith no longer refreshes, so
            // this asserts the carve was correct rather than that reality still matches. The
            // monolith's own agreement with reality was already established by PlanEmptyVerify.
            var command = DemonolithCommand.Build(
                "split migrate prove",
                request.RootDirectory,
                request.Engine,
                DemonolithCommand.VarFileFlags(engine.GetSnapCdDir()).ToArray());
            if (request.RederiveBackend) command += " --rederive-backend";

            await engine.RunProcess(command, killCts.Token, gracefulCts.Token);

            // module_states names each module the proof covered; a receipt marked complete means
            // every one of them planned clean, since demonolith fails the run otherwise.
            var receipt = DemonolithReceipt.Read(request.RootDirectory, DemonolithReceipt.ProveReceiptFile);
            var modulesProven = receipt?.ModuleStates.Count ?? 0;
            var modulesPlanningClean = receipt is { Complete: true } ? modulesProven : 0;


            await InvokeWithRetryAsync(
                () => runnerHubClient.InvokeMigrateProveCompleted(request.JobId, modulesProven, modulesPlanningClean),
                nameof(runnerHubClient.InvokeMigrateProveCompleted),
                request.JobId,
                connection);

            taskContext.LogSection("Completed MigrateProve");
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
