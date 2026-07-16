// SPDX-License-Identifier: LicenseRef-Snap-CD-Source-Available-1.1
// Copyright (c) 2026 Karl Schriek / Schrieksoft.
// No license is granted to use this file, in whole or in part, (a) as training, fine-tuning, retrieval, or
// embedding data for any machine-learning model, or (b) as input to any machine-learning model, agent, or automated
// system for the purpose of producing a derivative work or reimplementation that is not otherwise permitted by the
// Snap CD Source-Available License (including any Competing Product as defined therein). Contact info@snapcd.io
// for terms covering either use.

using MassTransit;
using Microsoft.Extensions.Options;
using SnapCd.Server.Core.Events.Server;
using SnapCd.Server.Core.Repositories.Organizations.Nonsecured;
using SnapCd.Server.Core.Settings;

namespace SnapCd.Server.Core.Consumers.System.Fanout;

/// <summary>
/// Fanout consumer that responds to server heartbeat requests.
/// Each server instance has its own temporary queue with this consumer.
/// Used to verify if a specific runner connection is still active on this server.
///
/// A heartbeat request is addressed to exactly one server, via
/// <see cref="ServerHeartbeatRequest.ServerInstanceId"/>. Because this is a fanout,
/// every live server receives every request — so each one MUST ignore requests that
/// are not addressed to it. Callers rely on a <c>RequestTimeoutException</c> to mean
/// "that server is gone": if any server answers on a dead server's behalf, the caller
/// concludes it is alive and never cleans up after it.
/// </summary>
public class ServerHeartbeatConsumer : IConsumer<ServerHeartbeatRequest>
{
    private readonly ServerSettings _serverSettings;
    private readonly RunnerConnectionRepositoryFactory _connectionRepositoryFactory;
    private readonly ILogger<ServerHeartbeatConsumer> _logger;

    public ServerHeartbeatConsumer(
        IOptions<ServerSettings> serverSettings,
        RunnerConnectionRepositoryFactory connectionRepositoryFactory,
        ILogger<ServerHeartbeatConsumer> logger)
    {
        _serverSettings = serverSettings.Value;
        _connectionRepositoryFactory = connectionRepositoryFactory;
        _logger = logger;
    }

    public async Task Consume(ConsumeContext<ServerHeartbeatRequest> context)
    {
        var request = context.Message;

        // Only the addressed server may answer. Every other server stays silent so the
        // caller's request times out — which is exactly how it detects a dead server.
        if (request.ServerInstanceId != _serverSettings.InstanceId)
        {
            _logger.LogTrace(
                "Ignoring heartbeat request addressed to server {TargetServerInstanceId}; this is server {ServerInstanceId}",
                request.ServerInstanceId,
                _serverSettings.InstanceId);
            return;
        }

        _logger.LogDebug(
            "Received heartbeat request for runner {RunnerInstanceName} (ID: {RunnerId}) on server {ServerInstanceId}",
            request.InstanceName,
            request.RunnerId,
            request.ServerInstanceId);

        // Check if this server still has the connection in the database
        using var connectionRepository = _connectionRepositoryFactory.Create();
        var connection = await connectionRepository.GetActiveConnection(
            request.OrganizationId,
            request.RunnerId,
            request.InstanceName);

        var isConnected = connection != null && connection.ServerInstanceId == _serverSettings.InstanceId;

        _logger.LogDebug(
            "Heartbeat check result for runner {RunnerInstanceName}: IsConnected={IsConnected}",
            request.InstanceName,
            isConnected);

        await context.RespondAsync(new ServerHeartbeatResponse
        {
            IsConnected = isConnected
        });
    }
}
