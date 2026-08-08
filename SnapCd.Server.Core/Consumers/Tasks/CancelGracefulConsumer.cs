// SPDX-License-Identifier: LicenseRef-Snap-CD-Source-Available-1.1
// Copyright (c) 2026 Karl Schriek / Schrieksoft.
// No license is granted to use this file, in whole or in part, (a) as training, fine-tuning, retrieval, or
// embedding data for any machine-learning model, or (b) as input to any machine-learning model, agent, or automated
// system for the purpose of producing a derivative work or reimplementation that is not otherwise permitted by the
// Snap CD Source-Available License (including any Competing Product as defined therein). Contact info@snapcd.io
// for terms covering either use.

using MassTransit;
using Microsoft.AspNetCore.SignalR;
using SnapCd.Contracts.Constants;
using SnapCd.Contracts.RunnerRequests;
using SnapCd.Server.Core.Events.Steps;
using SnapCd.Server.Core.Hubs;
using SnapCd.Server.Core.Repositories.Organizations.Nonsecured;

namespace SnapCd.Server.Core.Consumers.Tasks;

/// <summary>
/// Server-side consumer that receives graceful cancellation requests and dispatches them to runners via SignalR.
/// Runner will respond by publishing GracefulCancelCompleted event directly to MassTransit.
/// </summary>
public class CancelGracefulConsumer : IConsumer<CancelGracefulRequested>
{
    private readonly ILogger<CancelGracefulConsumer> _logger;
    private readonly IHubContext<RunnerHub> _hubContext;
    private readonly RunnerConnectionRepositoryFactory _connectionRepositoryFactory;

    public CancelGracefulConsumer(
        ILogger<CancelGracefulConsumer> logger,
        IHubContext<RunnerHub> hubContext,
        RunnerConnectionRepositoryFactory connectionRepositoryFactory)
    {
        _logger = logger;
        _hubContext = hubContext;
        _connectionRepositoryFactory = connectionRepositoryFactory;
    }

    public async Task Consume(ConsumeContext<CancelGracefulRequested> context)
    {
        var msg = context.Message;
        var correlationId = msg.CorrelationId;
        var orgId = msg.OrganizationId;
        var runnerName = msg.RunnerInstanceName;
        var runnerId = msg.RunnerId;

        _logger.LogDebug("Received graceful cancellation request for job {CorrelationId}", correlationId);

        if (string.IsNullOrEmpty(runnerName))
        {
            _logger.LogWarning("No runner name provided in graceful cancel request for job {CorrelationId}", correlationId);
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

            _logger.LogDebug("Sending graceful cancellation to runner {RunnerName} (ConnectionId: {ConnectionId}) for job {CorrelationId}",
                connection.InstanceName, connection.SignalRConnectionId, correlationId);

            // Send graceful cancellation request to specific client (fire and forget)
            // Runner will publish GracefulCancelCompleted event when done
            await _hubContext.Clients.Client(connection.SignalRConnectionId).SendAsync(
                RunnerEndpoints.CancelGraceful,
                new CancelGracefulRequest
                {
                    JobId = correlationId
                });

            _logger.LogDebug("Graceful cancellation request sent to runner for job {CorrelationId}", correlationId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Graceful cancellation failed for job {CorrelationId}", correlationId);
        }
    }
}
