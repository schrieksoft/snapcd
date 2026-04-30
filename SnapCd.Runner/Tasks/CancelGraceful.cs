using Microsoft.AspNetCore.SignalR.Client;
using SnapCd.Contracts;
using SnapCd.Contracts.Clients;
using SnapCd.Contracts.RunnerRequests;

namespace SnapCd.Runner.Tasks;

public partial class Tasks
{
    public async Task CancelGraceful(CancelGracefulRequest request, HubConnection connection)
    {
        var logger = _loggerFactory.CreateLogger<Tasks>();

        logger.LogInformation("Received graceful cancellation request for job {JobId}", request.JobId);

        var result = _processRegistry.TryCancel(request.JobId, CancellationType.ImmediateGraceful);

        if (result)
            logger.LogInformation("Graceful cancellation signal sent to running process for job {JobId}", request.JobId);
        else
            logger.LogWarning("No running process found for graceful cancellation of job {JobId}", request.JobId);

        // Send completion response to server
        var runnerHubClient = new RunnerHubClient(connection);
        await InvokeWithRetryAsync(
            () => runnerHubClient.InvokeCancelGracefulCompleted(request.JobId),
            nameof(runnerHubClient.InvokeCancelGracefulCompleted),
            request.JobId,
            connection);
    }
}