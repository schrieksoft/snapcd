using Microsoft.AspNetCore.SignalR.Client;
using SnapCd.Contracts;
using SnapCd.Contracts.Clients;
using SnapCd.Contracts.RunnerRequests;

namespace SnapCd.Runner.Tasks;

public partial class Tasks
{
    public async Task CancelKill(CancelKillRequest request, HubConnection connection)
    {
        var logger = _loggerFactory.CreateLogger<Tasks>();

        logger.LogInformation("Received kill cancellation request for job {JobId}", request.JobId);

        var result = _processRegistry.TryCancel(request.JobId, CancellationType.ImmediateKill);

        if (result)
            logger.LogInformation("Kill cancellation signal sent to running process for job {JobId}", request.JobId);
        else
            logger.LogWarning("No running process found for kill cancellation of job {JobId}", request.JobId);

        // Send completion response to server
        var runnerHubClient = new RunnerHubClient(connection);
        await InvokeWithRetryAsync(
            () => runnerHubClient.InvokeCancelKillCompleted(request.JobId),
            nameof(runnerHubClient.InvokeCancelKillCompleted),
            request.JobId,
            connection);
    }
}