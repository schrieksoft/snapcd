using Microsoft.AspNetCore.SignalR.Client;
using SnapCd.Contracts;
using SnapCd.Contracts.Clients;
using SnapCd.Contracts.RunnerRequests;
using SnapCd.Contracts.RunnerRequests.HelperClasses;

namespace SnapCd.Runner.Tasks;

public partial class Tasks
{
    public async Task GetModule(GetModuleRequestBase request, HubConnection connection)
    {
        var killCts = new CancellationTokenSource();
        _processRegistry.Register(request.JobId, killCts, CancellationType.ImmediateKill);

        var gracefulCts = new CancellationTokenSource();
        _processRegistry.Register(request.JobId, gracefulCts, CancellationType.ImmediateGraceful);

        // Start periodic task reporting
        var reportingCts = CancellationTokenSource.CreateLinkedTokenSource(killCts.Token, gracefulCts.Token);
        var reportingTask = StartPeriodicTaskReporting(
            request.JobId,
            nameof(GetModule),
            connection,
            TimeSpan.FromSeconds(request.ReportActiveJobFrequencySeconds),
            reportingCts.Token);

        var logger = _loggerFactory.CreateLogger<Tasks>();
        var taskContext = new RunnerTaskContext(
            request.JobId,
            nameof(GetModule),
            logger,
            request.Metadata
        );

        var runnerHubClient = new RunnerHubClient(connection);

        try
        {
            taskContext.LogInformation("Now cloning repo");

            var moduleGetter = await _moduleGetterFactory.Create(
                taskContext,
                request.SourceType,
                request.SourceRevisionType,
                request.SourceUrl,
                request.SourceRevision,
                request.Metadata,
                request.Engine
            );

            await moduleGetter.GetModule(
                request.CleanInitEnabled,
                request.ExtraFiles,
                killCts.Token,
                gracefulCts.Token,
                request.SourceDefinitiveRevision);

            await InvokeWithRetryAsync(
                () => runnerHubClient.InvokeGetModuleCompleted(request.JobId),
                nameof(runnerHubClient.InvokeGetModuleCompleted),
                request.JobId,
                connection);

            taskContext.LogInformation("Completed GetModule");
        }
        catch (OperationCanceledException)
        {
            taskContext.LogWarning("GetModule process was cancelled.");
            await InvokeWithRetryAsync(
                () => runnerHubClient.InvokeGetModuleCancelled(request.JobId),
                nameof(runnerHubClient.InvokeGetModuleCancelled),
                request.JobId,
                connection);
        }
        catch (Exception ex)
        {
            taskContext.LogError($"Unhandled exception occurred. {ex.Message}");
            logger.LogError(ex, "Error handling GetModule for job {JobId}", request.JobId);
            await InvokeWithRetryAsync(
                () => runnerHubClient.InvokeGetModuleFaulted(
                    request.JobId,
                    ex.Message,
                    ex.StackTrace
                ),
                nameof(runnerHubClient.InvokeGetModuleFaulted),
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