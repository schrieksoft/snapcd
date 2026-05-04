using Microsoft.AspNetCore.SignalR.Client;
using SnapCd.Contracts;
using SnapCd.Contracts.Clients;
using SnapCd.Contracts.RunnerRequests;
using SnapCd.Contracts.RunnerRequests.HelperClasses;

namespace SnapCd.Runner.Tasks;

public partial class Tasks
{
    public async Task Init(InitRequestBase request, HubConnection connection)
    {
        var killCts = new CancellationTokenSource();
        _processRegistry.Register(request.JobId, killCts, CancellationType.ImmediateKill);

        var gracefulCts = new CancellationTokenSource();
        _processRegistry.Register(request.JobId, gracefulCts, CancellationType.ImmediateGraceful);

        // Start periodic task reporting
        var reportingCts = CancellationTokenSource.CreateLinkedTokenSource(killCts.Token, gracefulCts.Token);
        var reportingTask = StartPeriodicTaskReporting(
            request.JobId,
            nameof(Init),
            connection,
            TimeSpan.FromSeconds(request.ReportActiveJobFrequencySeconds),
            reportingCts.Token);

        var logger = _loggerFactory.CreateLogger<Tasks>();
        var taskContext = new RunnerTaskContext(
            request.JobId,
            nameof(Init),
            logger,
            request.Metadata
        );
        
        var runnerHubClient = new RunnerHubClient(connection);

        try
        {
            taskContext.LogInformation("Now initializing");

            // Validate hooks against pre-approved hooks
            _hookPreapprovalService.ValidateHooks(
                (request.InitBeforeHook, nameof(request.InitBeforeHook)),
                (request.InitAfterHook, nameof(request.InitAfterHook))
            );

            var engine = _engineFactory.Create(
                taskContext,
                request.Engine,
                request.Metadata,
                request.BackendConfiguration.PulumiFlags,
                request.BackendConfiguration.PulumiArrayFlags,
                request.BackendConfiguration.TerraformFlags,
                request.BackendConfiguration.TerraformArrayFlags
            );

            await engine.Init(
                request.ResolvedEnvVars,
                request.InitBeforeHook,
                request.InitAfterHook,
                request.BackendConfiguration,
                killCts.Token,
                gracefulCts.Token);

            await InvokeWithRetryAsync(
                () => runnerHubClient.InvokeInitCompleted(request.JobId),
                nameof(runnerHubClient.InvokeInitCompleted),
                request.JobId,
                connection);

            taskContext.LogInformation("Completed Init");
        }
        catch (OperationCanceledException)
        {
            taskContext.LogWarning("Init process was cancelled.");
            await InvokeWithRetryAsync(
                () => runnerHubClient.InvokeInitCancelled(request.JobId),
                nameof(runnerHubClient.InvokeInitCancelled),
                request.JobId,
                connection);
        }
        catch (Exception ex)
        {
            taskContext.LogError($"Unhandled exception occurred. {ex.Message}");
            logger.LogError(ex, "Error handling Init for job {JobId}", request.JobId);
            await InvokeWithRetryAsync(
                () => runnerHubClient.InvokeInitFaulted(
                    request.JobId,
                    ex.Message,
                    ex.StackTrace
                ),
                nameof(runnerHubClient.InvokeInitFaulted),
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