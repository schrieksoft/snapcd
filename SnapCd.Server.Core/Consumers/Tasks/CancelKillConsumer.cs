// SPDX-License-Identifier: LicenseRef-Snap-CD-Source-Available-1.1
// Copyright (c) 2026 Karl Schriek / Schrieksoft.
// No license is granted to use this file, in whole or in part, (a) as training, fine-tuning, retrieval, or
// embedding data for any machine-learning model, or (b) as input to any machine-learning model, agent, or automated
// system for the purpose of producing a derivative work or reimplementation that is not otherwise permitted by the
// Snap CD Source-Available License (including any Competing Product as defined therein). Contact info@snapcd.io
// for terms covering either use.

using SnapCd.Contracts.Endpoints;
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
public abstract class CancelKillConsumer<TRequested> : IConsumer<TRequested>
    where TRequested : CancelKillRequestedBase
{
    protected abstract string Endpoint { get; }

    private readonly ILogger _logger;
    private readonly IHubContext<RunnerHub> _hubContext;
    private readonly RunnerConnectionRepositoryFactory _connectionRepositoryFactory;

    protected CancelKillConsumer(
        ILogger logger,
        IHubContext<RunnerHub> hubContext,
        RunnerConnectionRepositoryFactory connectionRepositoryFactory)
    {
        _logger = logger;
        _hubContext = hubContext;
        _connectionRepositoryFactory = connectionRepositoryFactory;
    }

    public async Task Consume(ConsumeContext<TRequested> context)
    {
        var msg = context.Message;
        var correlationId = msg.CorrelationId;
        var orgId = msg.OrganizationId;
        var runnerName = msg.RunnerInstanceName;
        var runnerId = msg.RunnerId;

        _logger.LogDebug("Received kill cancellation request for job {CorrelationId}", correlationId);

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

            _logger.LogDebug("Sending kill cancellation to runner {RunnerName} (ConnectionId: {ConnectionId}) for job {CorrelationId}",
                connection.InstanceName, connection.SignalRConnectionId, correlationId);

            // Send kill cancellation request to specific client (fire and forget)
            // Runner will publish KillCancelCompleted event when done
            await _hubContext.Clients.Client(connection.SignalRConnectionId).SendAsync(
                Endpoint,
                new CancelKillRequest
                {
                    JobId = correlationId
                });

            _logger.LogDebug("Kill cancellation request sent to runner for job {CorrelationId}", correlationId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Kill cancellation failed for job {CorrelationId}", correlationId);
        }
    }
}


public class ApplyCancelKillConsumer : CancelKillConsumer<ApplyCancelKillRequested>
{
    public ApplyCancelKillConsumer(
        ILogger<ApplyCancelKillConsumer> logger,
        IHubContext<RunnerHub> hubContext,
        RunnerConnectionRepositoryFactory connectionRepositoryFactory)
        : base(logger, hubContext, connectionRepositoryFactory) { }

    protected override string Endpoint => nameof(IApplyEndpoints.ApplyCancelKill);
}

public class DestroyCancelKillConsumer : CancelKillConsumer<DestroyCancelKillRequested>
{
    public DestroyCancelKillConsumer(
        ILogger<DestroyCancelKillConsumer> logger,
        IHubContext<RunnerHub> hubContext,
        RunnerConnectionRepositoryFactory connectionRepositoryFactory)
        : base(logger, hubContext, connectionRepositoryFactory) { }

    protected override string Endpoint => nameof(IDestroyEndpoints.DestroyCancelKill);
}

public class SplitCancelKillConsumer : CancelKillConsumer<SplitCancelKillRequested>
{
    public SplitCancelKillConsumer(
        ILogger<SplitCancelKillConsumer> logger,
        IHubContext<RunnerHub> hubContext,
        RunnerConnectionRepositoryFactory connectionRepositoryFactory)
        : base(logger, hubContext, connectionRepositoryFactory) { }

    protected override string Endpoint => nameof(ISplitEndpoints.SplitCancelKill);
}

public class MoveCancelKillConsumer : CancelKillConsumer<MoveCancelKillRequested>
{
    public MoveCancelKillConsumer(
        ILogger<MoveCancelKillConsumer> logger,
        IHubContext<RunnerHub> hubContext,
        RunnerConnectionRepositoryFactory connectionRepositoryFactory)
        : base(logger, hubContext, connectionRepositoryFactory) { }

    protected override string Endpoint => nameof(IMoveEndpoints.MoveCancelKill);
}

public class ImportCancelKillConsumer : CancelKillConsumer<ImportCancelKillRequested>
{
    public ImportCancelKillConsumer(
        ILogger<ImportCancelKillConsumer> logger,
        IHubContext<RunnerHub> hubContext,
        RunnerConnectionRepositoryFactory connectionRepositoryFactory)
        : base(logger, hubContext, connectionRepositoryFactory) { }

    protected override string Endpoint => nameof(IImportEndpoints.ImportCancelKill);
}

public class RemoveCancelKillConsumer : CancelKillConsumer<RemoveCancelKillRequested>
{
    public RemoveCancelKillConsumer(
        ILogger<RemoveCancelKillConsumer> logger,
        IHubContext<RunnerHub> hubContext,
        RunnerConnectionRepositoryFactory connectionRepositoryFactory)
        : base(logger, hubContext, connectionRepositoryFactory) { }

    protected override string Endpoint => nameof(IRemoveEndpoints.RemoveCancelKill);
}
