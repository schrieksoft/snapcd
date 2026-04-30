using Microsoft.AspNetCore.SignalR.Client;
using SnapCd.Contracts;
using SnapCd.Contracts.Clients;
using SnapCd.Contracts.RunnerRequests;

namespace SnapCd.Runner.Tasks;

public partial class Tasks
{
    public async Task SourceRefresh(SourceRefreshRequest request, HubConnection connection)
    {
        var logger = _loggerFactory.CreateLogger<Tasks>();
        var runnerHubClient = new RunnerHubClient(connection);

        try
        {
            logger.LogInformation("Refreshing source: {SourceUrl} @ {SourceRevision} (Type: {SourceType}, RevisionType: {SourceRevisionType})",
                request.SourceUrl, request.SourceRevision, request.SourceType, request.SourceRevisionType);

            var refresher = _moduleSourceRefresherFactory.Create(request.SourceType);

            var definitiveRevision = refresher.GetRemoteDefinitiveRevision(
                request.SourceUrl,
                request.SourceRevision,
                request.SourceRevisionType);

            logger.LogInformation("Source refresh completed: {SourceUrl} @ {SourceRevision} -> {DefinitiveRevision}",
                request.SourceUrl, request.SourceRevision, definitiveRevision);

            await InvokeWithRetryAsync(
                () => runnerHubClient.InvokeSourceRefreshCompleted(
                    request.SourceUrl,
                    request.SourceRevision,
                    request.SourceType,
                    request.SourceRevisionType,
                    definitiveRevision),
                nameof(runnerHubClient.InvokeSourceRefreshCompleted),
                Guid.Empty,
                connection); // SourceRefresh is stateless, no JobId
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Source refresh failed: {SourceUrl} @ {SourceRevision}", request.SourceUrl, request.SourceRevision);

            var errorMessage = $"Failed to refresh revision.\n- SourceUrl: {request.SourceUrl}\n- SourceType: {request.SourceType}\n- SourceRevision: {request.SourceRevision}\n- SourceRevisionType: {request.SourceRevisionType}\nError message: {ex.Message}";

            await InvokeWithRetryAsync(
                () => runnerHubClient.InvokeSourceRefreshFaulted(
                    request.SourceUrl,
                    request.SourceRevision,
                    request.SourceType,
                    request.SourceRevisionType,
                    errorMessage,
                    ex.StackTrace),
                nameof(runnerHubClient.InvokeSourceRefreshFaulted),
                Guid.Empty,
                connection); // SourceRefresh is stateless, no JobId
        }
    }
}
