using Microsoft.AspNetCore.SignalR.Client;
using SnapCd.Contracts;
using SnapCd.Contracts.Clients;
using SnapCd.Contracts.RunnerRequests;
using SnapCd.Contracts.RunnerRequests.HelperClasses;

namespace SnapCd.Runner.Tasks;

public partial class Tasks
{
    public async Task DestroyFromPlan(DestroyFromPlanRequestBase request, HubConnection connection)
    {
        var killCts = new CancellationTokenSource();
        _processRegistry.Register(request.JobId, killCts, CancellationType.ImmediateKill);

        var gracefulCts = new CancellationTokenSource();
        _processRegistry.Register(request.JobId, gracefulCts, CancellationType.ImmediateGraceful);

        // Start periodic task reporting
        var reportingCts = CancellationTokenSource.CreateLinkedTokenSource(killCts.Token, gracefulCts.Token);
        var reportingTask = StartPeriodicTaskReporting(
            request.JobId,
            nameof(DestroyFromPlan),
            connection,
            TimeSpan.FromSeconds(request.ReportActiveJobFrequencySeconds),
            reportingCts.Token);

        var logger = _loggerFactory.CreateLogger<Tasks>();
        var taskContext = new RunnerTaskContext(
            request.JobId,
            nameof(DestroyFromPlan),
            logger,
            request.Metadata
        );

        var runnerHubClient = new RunnerHubClient(connection);

        try
        {
            taskContext.LogInformation("Now destroying from plan");

            // Validate hooks against pre-approved hooks
            _hookPreapprovalService.ValidateHooks(
                (request.DestroyBeforeHook, nameof(request.DestroyBeforeHook)),
                (request.DestroyAfterHook, nameof(request.DestroyAfterHook))
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

            await engine.DestroyFromPlan(request.DestroyBeforeHook, request.DestroyAfterHook, killCts.Token, gracefulCts.Token);

            // Read statistics from file written by the destroy command
            var actualResourceCount = await engine.ReadStatisticsFromFile();
            taskContext.LogInformation($"Resource count: {actualResourceCount}");

            await InvokeWithRetryAsync(
                () => runnerHubClient.InvokeDestroyFromPlanCompleted(request.JobId, actualResourceCount),
                nameof(runnerHubClient.InvokeDestroyFromPlanCompleted),
                request.JobId,
                connection);
        }
        catch (OperationCanceledException)
        {
            taskContext.LogWarning("Destroy process was cancelled.");
            await InvokeWithRetryAsync(
                () => runnerHubClient.InvokeDestroyFromPlanCancelled(request.JobId),
                nameof(runnerHubClient.InvokeDestroyFromPlanCancelled),
                request.JobId,
                connection);
        }
        catch (Exception ex)
        {
            taskContext.LogError($"Destroy failed with exception: {ex.Message}");
            logger.LogError(ex, "Error handling DestroyFromPlan for job {JobId}", request.JobId);

            // Try to read statistics even if destroy failed
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
            catch
            {
                // Swallow any errors reading stats - we're already handling an exception
            }

            await InvokeWithRetryAsync(
                () => runnerHubClient.InvokeDestroyFromPlanFaulted(
                    request.JobId,
                    ex.Message,
                    ex.StackTrace,
                    actualResourceCount
                ),
                nameof(runnerHubClient.InvokeDestroyFromPlanFaulted),
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