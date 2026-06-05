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
using SnapCd.Contracts.RunnerRequests;
using SnapCd.Contracts.RunnerRequests.HelperClasses;

namespace SnapCd.Runner.Tasks;

public partial class Tasks
{
    public async Task ApplyFromPlan(ApplyFromPlanRequestBase request, HubConnection connection)
    {
        var killCts = new CancellationTokenSource();
        _processRegistry.Register(request.JobId, killCts, CancellationType.ImmediateKill);

        var gracefulCts = new CancellationTokenSource();
        _processRegistry.Register(request.JobId, gracefulCts, CancellationType.ImmediateGraceful);

        // Start periodic task reporting
        var reportingCts = CancellationTokenSource.CreateLinkedTokenSource(killCts.Token, gracefulCts.Token);
        var reportingTask = StartPeriodicTaskReporting(
            request.JobId,
            nameof(ApplyFromPlan),
            connection,
            TimeSpan.FromSeconds(request.ReportActiveJobFrequencySeconds),
            reportingCts.Token);

        var logger = _loggerFactory.CreateLogger<Tasks>();
        var taskContext = new RunnerTaskContext(
            request.JobId,
            nameof(ApplyFromPlan),
            logger,
            _jobLogStream,
            request.Metadata
        );

        var runnerHubClient = new RunnerHubClient(connection);

        try
        {
            taskContext.LogInformation("Now applying from plan");

            // Validate hooks against pre-approved hooks
            _hookPreapprovalService.ValidateHooks(
                (request.ApplyBeforeHook, nameof(request.ApplyBeforeHook)),
                (request.ApplyAfterHook, nameof(request.ApplyAfterHook))
            );

            var engine = _engineFactory.Create(
                taskContext,
                request.Engine,
                request.Metadata,
                request.PulumiFlags,
                request.PulumiArrayFlags,
                request.TerraformFlags,
                request.TerraformArrayFlags
            );

            await engine.ApplyFromPlan(request.ApplyBeforeHook, request.ApplyAfterHook, killCts.Token, gracefulCts.Token);

            // Read statistics from file written by the apply command
            var actualResourceCount = await engine.ReadStatisticsFromFile();
            taskContext.LogInformation($"Resource count: {actualResourceCount}");

            await InvokeWithRetryAsync(
                () => runnerHubClient.InvokeApplyFromPlanCompleted(request.JobId, actualResourceCount),
                nameof(runnerHubClient.InvokeApplyFromPlanCompleted),
                request.JobId,
                connection);
        }
        catch (OperationCanceledException)
        {
            taskContext.LogWarning("Apply process was cancelled.");
            await InvokeWithRetryAsync(
                () => runnerHubClient.InvokeApplyFromPlanCancelled(request.JobId),
                nameof(runnerHubClient.InvokeApplyFromPlanCancelled),
                request.JobId,
                connection);
        }
        catch (Exception ex)
        {
            taskContext.LogError($"Apply failed with exception: {ex.Message}");

            // Try to read statistics even if apply failed
            int? actualResourceCount = null;
            try
            {
                var engine = _engineFactory.Create(
                    taskContext,
                    request.Engine,
                    request.Metadata
                );
                actualResourceCount = await engine.ReadStatisticsFromFile();
            }
            catch (Exception readEx)
            {
                taskContext.LogWarning($"Could not read statistics after failure: {readEx.Message}");
            }

            await InvokeWithRetryAsync(
                () => runnerHubClient.InvokeApplyFromPlanFaulted(request.JobId, ex.Message, ex.StackTrace, actualResourceCount),
                nameof(runnerHubClient.InvokeApplyFromPlanFaulted),
                request.JobId,
                connection);
        }
        finally
        {
            // Stop periodic reporting
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