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

namespace SnapCd.Runner.Tasks;

public partial class Tasks
{
    /// <summary>
    /// Asserts the plan written by the preceding Plan step is empty. A monolith with pending
    /// changes cannot be split: the carve would be proved against a baseline that was never real.
    /// The plan file is already on disk, so nothing is executed here — it is only read.
    /// </summary>
    public async Task PlanEmptyVerify(PlanEmptyVerifyRequestBase request, HubConnection connection)
    {
        var killCts = new CancellationTokenSource();
        _processRegistry.Register(request.JobId, killCts, CancellationType.ImmediateKill);

        var gracefulCts = new CancellationTokenSource();
        _processRegistry.Register(request.JobId, gracefulCts, CancellationType.ImmediateGraceful);

        var reportingCts = CancellationTokenSource.CreateLinkedTokenSource(killCts.Token, gracefulCts.Token);
        var reportingTask = StartPeriodicTaskReporting(
            request.JobId,
            nameof(PlanEmptyVerify),
            connection,
            TimeSpan.FromSeconds(request.ReportActiveJobFrequencySeconds),
            reportingCts.Token);

        var logger = _loggerFactory.CreateLogger<Tasks>();
        var taskContext = new RunnerTaskContext(
            request.JobId,
            nameof(PlanEmptyVerify),
            logger,
            _jobLogStream,
            request.Metadata
        );

        var runnerHubClient = new RunnerHubClient(connection);

        try
        {
            taskContext.LogInformation("Verifying that the plan is empty");

            var engine = _engineFactory.Create(
                taskContext,
                request.Engine,
                request.Metadata
            );

            var plan = engine.ParseApplyPlan();

            var createCount = plan.GetResourceCount(PlanAction.Create);
            var modifyCount = plan.GetResourceCount(PlanAction.Update);
            var destroyCount = plan.GetResourceCount(PlanAction.Delete);
            var recreateCount = plan.GetResourceCount(PlanAction.Replace);
            var changedCount = createCount + modifyCount + destroyCount + recreateCount;

            if (changedCount > 0)
            {
                var summary =
                    $"The module's plan is not empty: {changedCount} pending change(s) "
                    + $"(create {createCount}, modify {modifyCount}, destroy {destroyCount}, recreate {recreateCount}). "
                    + "Apply the module before splitting it.";

                taskContext.LogError(summary);

                await InvokeWithRetryAsync(
                    () => runnerHubClient.InvokePlanEmptyVerifyFaulted(request.JobId, summary, null),
                    nameof(runnerHubClient.InvokePlanEmptyVerifyFaulted),
                    request.JobId,
                    connection);

                return;
            }

            taskContext.LogInformation("Plan is empty");

            await InvokeWithRetryAsync(
                () => runnerHubClient.InvokePlanEmptyVerifyCompleted(request.JobId),
                nameof(runnerHubClient.InvokePlanEmptyVerifyCompleted),
                request.JobId,
                connection);

            taskContext.LogInformation("Completed PlanEmptyVerify");
        }
        catch (OperationCanceledException)
        {
            taskContext.LogWarning("PlanEmptyVerify was cancelled.");
            await InvokeWithRetryAsync(
                () => runnerHubClient.InvokePlanEmptyVerifyCancelled(request.JobId),
                nameof(runnerHubClient.InvokePlanEmptyVerifyCancelled),
                request.JobId,
                connection);
        }
        catch (Exception ex)
        {
            taskContext.LogError($"Unhandled exception occurred. {ex.Message}");
            logger.LogError(ex, "Error handling PlanEmptyVerify for job {JobId}", request.JobId);
            await InvokeWithRetryAsync(
                () => runnerHubClient.InvokePlanEmptyVerifyFaulted(request.JobId, ex.Message, ex.StackTrace),
                nameof(runnerHubClient.InvokePlanEmptyVerifyFaulted),
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
