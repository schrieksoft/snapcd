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
using SnapCd.Contracts.RunnerRequests.Transfers;
using SnapCd.Runner.Services.SplitMigrate;
using SnapCd.Runner.Services.Transfers;

namespace SnapCd.Runner.Tasks;

public partial class Tasks
{
    /// <summary>Pulls and pins this Module's state. The source also writes the fragment the receiver needs.</summary>
    public async Task TransferMigrateMap(TransferMigrateMapRequestBase request, HubConnection connection)
    {
        var killCts = new CancellationTokenSource();
        _processRegistry.Register(request.JobId, killCts, CancellationType.ImmediateKill);

        var gracefulCts = new CancellationTokenSource();
        _processRegistry.Register(request.JobId, gracefulCts, CancellationType.ImmediateGraceful);

        var reportingCts = CancellationTokenSource.CreateLinkedTokenSource(killCts.Token, gracefulCts.Token);
        var reportingTask = StartPeriodicTaskReporting(
            request.JobId,
            nameof(TransferMigrateMap),
            connection,
            TimeSpan.FromSeconds(request.ReportActiveJobFrequencySeconds),
            reportingCts.Token);

        var logger = _loggerFactory.CreateLogger<Tasks>();
        var taskContext = new RunnerTaskContext(
            request.JobId,
            nameof(TransferMigrateMap),
            logger,
            _jobLogStream,
            request.Metadata
        );

        var runnerHubClient = new RunnerHubClient(connection);

        try
        {
            taskContext.LogNarration("Now running demonolith transfer migrate map");

            var engine = _engineFactory.Create(taskContext, request.Engine, request.Metadata);


            var command = DemonolithCommand.Build("transfer migrate map", request.RootDirectory, request.Engine);

            await engine.RunProcess(command, killCts.Token, gracefulCts.Token);

            await InvokeWithRetryAsync(
                () => runnerHubClient.InvokeTransferMigrateMapCompleted(
                    request.JobId, request.ModuleId, TransferMap.NeedsOutputs(request.RootDirectory)),
                nameof(runnerHubClient.InvokeTransferMigrateMapCompleted),
                request.JobId,
                connection);

            taskContext.LogSection("Completed TransferMigrateMap");
        }
        catch (OperationCanceledException)
        {
            taskContext.LogWarning("TransferMigrateMap was cancelled.");
            await InvokeWithRetryAsync(
                () => runnerHubClient.InvokeTransferMigrateMapFaulted(request.JobId, request.ModuleId, "Cancelled.", null),
                nameof(runnerHubClient.InvokeTransferMigrateMapFaulted),
                request.JobId,
                connection);
        }
        catch (Exception ex)
        {
            taskContext.LogError($"Unhandled exception occurred. {ex.Message}");
            logger.LogError(ex, "Error handling TransferMigrateMap for job {JobId}", request.JobId);
            await InvokeWithRetryAsync(
                () => runnerHubClient.InvokeTransferMigrateMapFaulted(request.JobId, request.ModuleId, ex.Message, ex.StackTrace),
                nameof(runnerHubClient.InvokeTransferMigrateMapFaulted),
                request.JobId,
                connection);
        }
        finally
        {
            reportingCts.Cancel();
            try { await reportingTask; }
            catch { /* Already logged */ }

            _processRegistry.Remove(request.JobId, CancellationType.ImmediateKill);
            _processRegistry.Remove(request.JobId, CancellationType.ImmediateGraceful);
        }
    }

    /// <summary>Asks whether this Module's root plans to zero changes with the moved resources in place.</summary>
    public async Task TransferMigrateProve(TransferMigrateProveRequestBase request, HubConnection connection)
    {
        var killCts = new CancellationTokenSource();
        _processRegistry.Register(request.JobId, killCts, CancellationType.ImmediateKill);

        var gracefulCts = new CancellationTokenSource();
        _processRegistry.Register(request.JobId, gracefulCts, CancellationType.ImmediateGraceful);

        var reportingCts = CancellationTokenSource.CreateLinkedTokenSource(killCts.Token, gracefulCts.Token);
        var reportingTask = StartPeriodicTaskReporting(
            request.JobId,
            nameof(TransferMigrateProve),
            connection,
            TimeSpan.FromSeconds(request.ReportActiveJobFrequencySeconds),
            reportingCts.Token);

        var logger = _loggerFactory.CreateLogger<Tasks>();
        var taskContext = new RunnerTaskContext(
            request.JobId,
            nameof(TransferMigrateProve),
            logger,
            _jobLogStream,
            request.Metadata
        );

        var runnerHubClient = new RunnerHubClient(connection);

        try
        {
            taskContext.LogNarration("Now running demonolith transfer migrate prove");

            var engine = _engineFactory.Create(taskContext, request.Engine, request.Metadata);


            var command = DemonolithCommand.Build("transfer migrate prove", request.RootDirectory, request.Engine);

            await engine.RunProcess(command, killCts.Token, gracefulCts.Token);

            var outputs = await TransferFiles.ReadOutputs(request.RootDirectory);

            await InvokeWithRetryAsync(
                () => runnerHubClient.InvokeTransferMigrateProveCompleted(
                    request.JobId, request.ModuleId, 0, outputs, null),
                nameof(runnerHubClient.InvokeTransferMigrateProveCompleted),
                request.JobId,
                connection);

            taskContext.LogSection("Completed TransferMigrateProve");
        }
        catch (OperationCanceledException)
        {
            taskContext.LogWarning("TransferMigrateProve was cancelled.");
            await InvokeWithRetryAsync(
                () => runnerHubClient.InvokeTransferMigrateProveFaulted(request.JobId, request.ModuleId, "Cancelled.", null),
                nameof(runnerHubClient.InvokeTransferMigrateProveFaulted),
                request.JobId,
                connection);
        }
        catch (Exception ex)
        {
            taskContext.LogError($"Unhandled exception occurred. {ex.Message}");
            logger.LogError(ex, "Error handling TransferMigrateProve for job {JobId}", request.JobId);
            await InvokeWithRetryAsync(
                () => runnerHubClient.InvokeTransferMigrateProveFaulted(request.JobId, request.ModuleId, ex.Message, ex.StackTrace),
                nameof(runnerHubClient.InvokeTransferMigrateProveFaulted),
                request.JobId,
                connection);
        }
        finally
        {
            reportingCts.Cancel();
            try { await reportingTask; }
            catch { /* Already logged */ }

            _processRegistry.Remove(request.JobId, CancellationType.ImmediateKill);
            _processRegistry.Remove(request.JobId, CancellationType.ImmediateGraceful);
        }
    }

    /// <summary>Asks whether the code in this root still matches its own copy of the map.</summary>
    public async Task TransferRefactorDiff(TransferRefactorDiffRequestBase request, HubConnection connection)
    {
        var killCts = new CancellationTokenSource();
        _processRegistry.Register(request.JobId, killCts, CancellationType.ImmediateKill);

        var gracefulCts = new CancellationTokenSource();
        _processRegistry.Register(request.JobId, gracefulCts, CancellationType.ImmediateGraceful);

        var reportingCts = CancellationTokenSource.CreateLinkedTokenSource(killCts.Token, gracefulCts.Token);
        var reportingTask = StartPeriodicTaskReporting(
            request.JobId,
            nameof(TransferRefactorDiff),
            connection,
            TimeSpan.FromSeconds(request.ReportActiveJobFrequencySeconds),
            reportingCts.Token);

        var logger = _loggerFactory.CreateLogger<Tasks>();
        var taskContext = new RunnerTaskContext(
            request.JobId,
            nameof(TransferRefactorDiff),
            logger,
            _jobLogStream,
            request.Metadata
        );

        var runnerHubClient = new RunnerHubClient(connection);

        try
        {
            taskContext.LogNarration("Now running demonolith transfer refactor diff");

            var engine = _engineFactory.Create(taskContext, request.Engine, request.Metadata);


            var command = DemonolithCommand.Build("transfer refactor diff", request.RootDirectory, request.Engine);

            await engine.RunProcess(command, killCts.Token, gracefulCts.Token);

            await InvokeWithRetryAsync(
                () => runnerHubClient.InvokeTransferRefactorDiffCompleted(request.JobId, request.ModuleId, 0, null),
                nameof(runnerHubClient.InvokeTransferRefactorDiffCompleted),
                request.JobId,
                connection);

            taskContext.LogSection("Completed TransferRefactorDiff");
        }
        catch (OperationCanceledException)
        {
            taskContext.LogWarning("TransferRefactorDiff was cancelled.");
            await InvokeWithRetryAsync(
                () => runnerHubClient.InvokeTransferRefactorDiffFaulted(request.JobId, request.ModuleId, "Cancelled.", null),
                nameof(runnerHubClient.InvokeTransferRefactorDiffFaulted),
                request.JobId,
                connection);
        }
        catch (Exception ex)
        {
            taskContext.LogError($"Unhandled exception occurred. {ex.Message}");
            logger.LogError(ex, "Error handling TransferRefactorDiff for job {JobId}", request.JobId);
            await InvokeWithRetryAsync(
                () => runnerHubClient.InvokeTransferRefactorDiffFaulted(request.JobId, request.ModuleId, ex.Message, ex.StackTrace),
                nameof(runnerHubClient.InvokeTransferRefactorDiffFaulted),
                request.JobId,
                connection);
        }
        finally
        {
            reportingCts.Cancel();
            try { await reportingTask; }
            catch { /* Already logged */ }

            _processRegistry.Remove(request.JobId, CancellationType.ImmediateKill);
            _processRegistry.Remove(request.JobId, CancellationType.ImmediateGraceful);
        }
    }
}
