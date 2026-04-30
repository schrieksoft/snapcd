using Microsoft.AspNetCore.SignalR.Client;
using SnapCd.Contracts;
using SnapCd.Contracts.Clients;
using SnapCd.Contracts.RunnerRequests;
using SnapCd.Contracts.RunnerRequests.HelperClasses;

namespace SnapCd.Runner.Tasks;

public partial class Tasks
{
    public async Task Output(OutputRequestBase request, HubConnection connection)
    {
        var killCts = new CancellationTokenSource();
        _processRegistry.Register(request.JobId, killCts, CancellationType.ImmediateKill);

        var gracefulCts = new CancellationTokenSource();
        _processRegistry.Register(request.JobId, gracefulCts, CancellationType.ImmediateGraceful);

        // Start periodic task reporting
        var reportingCts = CancellationTokenSource.CreateLinkedTokenSource(killCts.Token, gracefulCts.Token);
        var reportingTask = StartPeriodicTaskReporting(
            request.JobId,
            nameof(Output),
            connection,
            TimeSpan.FromSeconds(request.ReportActiveJobFrequencySeconds),
            reportingCts.Token);

        var logger = _loggerFactory.CreateLogger<Tasks>();
        var taskContext = new RunnerTaskContext(
            request.JobId,
            nameof(Output),
            logger,
            request.Metadata
        );

        var runnerHubClient = new RunnerHubClient(connection);

        try
        {
            taskContext.LogInformation("Now outputting");

            // Validate hooks against pre-approved hooks
            _hookPreapprovalService.ValidateHooks(
                (request.OutputBeforeHook, nameof(request.OutputBeforeHook)),
                (request.OutputAfterHook, nameof(request.OutputAfterHook))
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

            // Discover which outputs are defined in extra files
            var extraFileNames = request.ExtraFileNames != null
                ? new HashSet<string>(request.ExtraFileNames)
                : null;
            var discoveryService = _discoveryServiceFactory.Create(request.Engine);
            var outputSources = await discoveryService.DiscoverOutputSourcesAsync(
                engine.GetInitDir(),
                extraFileNames);

            var moduleOutputJson = await engine.Output(request.OutputBeforeHook, request.OutputAfterHook, killCts.Token, gracefulCts.Token);

            var moduleOutputSet = await engine.ParseJsonToModuleOutputSet(moduleOutputJson, outputSources);

            await InvokeWithRetryAsync(
                () => runnerHubClient.InvokeOutputCompleted(request.JobId, moduleOutputSet),
                nameof(runnerHubClient.InvokeOutputCompleted),
                request.JobId,
                connection);
        }
        catch (OperationCanceledException)
        {
            taskContext.LogWarning("Output process was cancelled.");
            await InvokeWithRetryAsync(
                () => runnerHubClient.InvokeOutputCancelled(request.JobId),
                nameof(runnerHubClient.InvokeOutputCancelled),
                request.JobId,
                connection);
        }
        catch (Exception ex)
        {
            taskContext.LogError($"Unhandled exception occurred. {ex.Message}");
            await InvokeWithRetryAsync(
                () => runnerHubClient.InvokeOutputFaulted(request.JobId, ex.Message, ex.StackTrace),
                nameof(runnerHubClient.InvokeOutputFaulted),
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