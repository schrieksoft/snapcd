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
    /// <summary>
    /// Reads the committed map to learn which part this root plays. No engine runs: a receiver's
    /// map step fails without the source's fragment, so the role is read before that step rather
    /// than discovered by attempting it.
    /// </summary>
    public async Task TransferAnalyseMap(TransferAnalyseMapRequestBase request, HubConnection connection)
    {
        var logger = _loggerFactory.CreateLogger<Tasks>();
        var taskContext = new RunnerTaskContext(
            request.JobId,
            nameof(TransferAnalyseMap),
            logger,
            _jobLogStream,
            request.Metadata);

        var runnerHubClient = new RunnerHubClient(connection);

        try
        {
            var role = TransferMap.RoleOf(request.RootDirectory);
            var needsOutputs = TransferMap.NeedsOutputs(request.RootDirectory);

            taskContext.LogNarration(role.Kind switch
            {
                TransferRoleKind.Source => "This module gives the resources away",
                TransferRoleKind.Receiver => "This module takes the resources in",
                _ => $"The transfer map could not be read: {role.Problem}"
            });

            await InvokeWithRetryAsync(
                () => runnerHubClient.InvokeAnalyseMapCompleted(
                    request.JobId, request.ModuleId, role.Kind, needsOutputs, role.Problem),
                nameof(runnerHubClient.InvokeAnalyseMapCompleted),
                request.JobId,
                connection);

            taskContext.LogSection("Completed TransferAnalyseMap");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error handling TransferAnalyseMap for job {JobId}", request.JobId);
            await InvokeWithRetryAsync(
                () => runnerHubClient.InvokeAnalyseMapFaulted(
                    request.JobId, request.ModuleId, ex.Message, ex.StackTrace),
                nameof(runnerHubClient.InvokeAnalyseMapFaulted),
                request.JobId,
                connection);
        }
    }

    /// <summary>Pulls and pins this Module's state. The source also writes the fragment the receiver needs.</summary>
    public async Task TransferMigrateMap(TransferMigrateMapRequestBase request, HubConnection connection)
    {
        var killCts = new CancellationTokenSource();
        _processRegistry.Register(request.JobId, killCts, CancellationType.ImmediateKill);


        var reportingCts = CancellationTokenSource.CreateLinkedTokenSource(killCts.Token);
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

            // A receiver's map reads the fragment from its own working directory, so the source's
            // has to be on disk before demonolith runs.
            if (request.SourceFragment is { } fragment && request.SourceFragmentMeta is { } fragmentMeta)
            {
                await TransferFiles.WriteFragment(request.RootDirectory, fragment, fragmentMeta);
                taskContext.LogNarration("Applied the fragment from the other module");
            }

            var engine = _engineFactory.Create(taskContext, request.Engine, request.Metadata);


            var command = DemonolithCommand.Build("transfer migrate map", request.RootDirectory, request.Engine);

            await engine.RunProcess(command, killCts.Token);

            var role = TransferMap.RoleOf(request.RootDirectory);
            var (producedFragment, producedMeta) = role.ReceiverBase is { } receiverBase
                ? await TransferFiles.ReadFragmentFor(request.RootDirectory, receiverBase)
                : (null, null);

            await InvokeWithRetryAsync(
                () => runnerHubClient.InvokeTransferMigrateMapCompleted(
                    request.JobId, request.ModuleId, producedFragment, producedMeta),
                nameof(runnerHubClient.InvokeTransferMigrateMapCompleted),
                request.JobId,
                connection);

            taskContext.LogSection("Completed TransferMigrateMap");
        }
        catch (OperationCanceledException)
        {
            taskContext.LogWarning("TransferMigrateMap was cancelled.");
            await InvokeWithRetryAsync(
                () => runnerHubClient.InvokeTransferMigrateMapCancelled(request.JobId, request.ModuleId),
                nameof(runnerHubClient.InvokeTransferMigrateMapCancelled),
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
        }
    }

    /// <summary>Asks whether this Module's root plans to zero changes with the moved resources in place.</summary>
    public async Task TransferMigrateProve(TransferMigrateProveRequestBase request, HubConnection connection)
    {
        var killCts = new CancellationTokenSource();
        _processRegistry.Register(request.JobId, killCts, CancellationType.ImmediateKill);


        var reportingCts = CancellationTokenSource.CreateLinkedTokenSource(killCts.Token);
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

            // demonolith threads the producer's values from a file in this root's working
            // directory, so what the other half produced has to be on disk before the plan runs.
            if (request.ReceiverOutputs is { } sourceOutputs)
            {
                await TransferFiles.WriteOutputs(request.RootDirectory, sourceOutputs);
                taskContext.LogNarration("Applied the output values from the other module");
            }

            var engine = _engineFactory.Create(taskContext, request.Engine, request.Metadata);


            var command = DemonolithCommand.Build("transfer migrate prove", request.RootDirectory, request.Engine);

            await engine.RunProcess(command, killCts.Token);

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
                () => runnerHubClient.InvokeTransferMigrateProveCancelled(request.JobId, request.ModuleId),
                nameof(runnerHubClient.InvokeTransferMigrateProveCancelled),
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
        }
    }
}
