using Microsoft.AspNetCore.SignalR.Client;
using SnapCd.Contracts;
using SnapCd.Contracts.Clients;
using SnapCd.Contracts.RunnerRequests;
using SnapCd.Contracts.RunnerRequests.HelperClasses;

namespace SnapCd.Runner.Tasks;

public partial class Tasks
{
    public async Task GetDefinitiveRevision(GetDefinitiveRevisionRequest request, HubConnection connection)
    {
        var killCts = new CancellationTokenSource();
        _processRegistry.Register(request.JobId, killCts, CancellationType.ImmediateKill);

        var gracefulCts = new CancellationTokenSource();
        _processRegistry.Register(request.JobId, gracefulCts, CancellationType.ImmediateGraceful);

        // Start periodic task reporting
        var reportingCts = CancellationTokenSource.CreateLinkedTokenSource(killCts.Token, gracefulCts.Token);
        var reportingTask = StartPeriodicTaskReporting(
            request.JobId,
            nameof(GetDefinitiveRevision),
            connection,
            TimeSpan.FromSeconds(request.ReportActiveJobFrequencySeconds),
            reportingCts.Token);

        var logger = _loggerFactory.CreateLogger<Tasks>();
        var taskContext = new RunnerTaskContext(
            request.JobId,
            nameof(GetDefinitiveRevision),
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
                request.Metadata
            );

            var definitiveRevision = await moduleGetter.GetRemoteDefinitiveRevision();

            await InvokeWithRetryAsync(
                () => runnerHubClient.InvokeGetDefinitiveRevisionCompleted(
                    request.JobId,
                    definitiveRevision
                ),
                nameof(runnerHubClient.InvokeGetDefinitiveRevisionCompleted),
                request.JobId,
                connection);

            taskContext.LogInformation("Completed GetDefinitiveRevision");
        }
        catch (OperationCanceledException)
        {
            taskContext.LogWarning("GetDefinitiveRevision process was cancelled.");
            await InvokeWithRetryAsync(
                () => runnerHubClient.InvokeGetDefinitiveRevisionCancelled(request.JobId),
                nameof(runnerHubClient.InvokeGetDefinitiveRevisionCancelled),
                request.JobId,
                connection);
        }
        catch (Exception ex)
        {
            taskContext.LogError($"Unhandled exception occurred. {ex.Message}");
            logger.LogError(ex, "Error handling GetDefinitiveRevision for job {JobId}", request.JobId);
            await InvokeWithRetryAsync(
                () => runnerHubClient.InvokeGetDefinitiveRevisionFaulted(
                    request.JobId,
                    ex.Message,
                    ex.StackTrace
                ),
                nameof(runnerHubClient.InvokeGetDefinitiveRevisionFaulted),
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