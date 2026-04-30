using MassTransit;
using Microsoft.AspNetCore.SignalR;
using SnapCd.Contracts.Constants;
using SnapCd.Contracts.RunnerRequests;
using SnapCd.Server.Core.Events.Steps;
using SnapCd.Server.Core.Hubs;
using SnapCd.Server.Core.Repositories.Organizations.Nonsecured;

namespace SnapCd.Server.Core.Consumers.Tasks;

/// <summary>
/// Server-side consumer that receives kill cancellation requests and dispatches them to runners via SignalR.
/// Runner will respond by publishing KillCancelCompleted event directly to MassTransit.
/// </summary>
public class CancelKillConsumer : IConsumer<CancelKillRequested>
{
    private readonly ILogger<CancelKillConsumer> _logger;
    private readonly IHubContext<RunnerHub> _hubContext;
    private readonly RunnerConnectionRepositoryFactory _connectionRepositoryFactory;

    public CancelKillConsumer(
        ILogger<CancelKillConsumer> logger,
        IHubContext<RunnerHub> hubContext,
        RunnerConnectionRepositoryFactory connectionRepositoryFactory)
    {
        _logger = logger;
        _hubContext = hubContext;
        _connectionRepositoryFactory = connectionRepositoryFactory;
    }

    public async Task Consume(ConsumeContext<CancelKillRequested> context)
    {
        var msg = context.Message;
        var correlationId = msg.CorrelationId;
        var orgId = msg.OrganizationId;
        var runnerName = msg.RunnerInstanceName;
        var runnerId = msg.RunnerId;

        _logger.LogInformation("Received kill cancellation request for job {CorrelationId}", correlationId);

        if (string.IsNullOrEmpty(runnerName))
        {
            _logger.LogWarning("No runner name provided in kill cancel request for job {CorrelationId}", correlationId);
            return;
        }

        try
        {
            // Get the runner connection from database
            using var connectionRepository = _connectionRepositoryFactory.Create();
            var connection = await connectionRepository.GetActiveConnection(orgId, runnerId, runnerName);

            if (connection == null)
            {
                _logger.LogWarning("Runner {RunnerName} not found in database for job {CorrelationId}",
                    runnerName, correlationId);
                return;
            }

            _logger.LogInformation("Sending kill cancellation to runner {RunnerName} (ConnectionId: {ConnectionId}) for job {CorrelationId}",
                connection.InstanceName, connection.SignalRConnectionId, correlationId);

            // Send kill cancellation request to specific client (fire and forget)
            // Runner will publish KillCancelCompleted event when done
            await _hubContext.Clients.Client(connection.SignalRConnectionId).SendAsync(
                RunnerEndpoints.CancelKill,
                new CancelKillRequest
                {
                    JobId = correlationId
                });

            _logger.LogInformation("Kill cancellation request sent to runner for job {CorrelationId}", correlationId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Kill cancellation failed for job {CorrelationId}", correlationId);
        }
    }
}
