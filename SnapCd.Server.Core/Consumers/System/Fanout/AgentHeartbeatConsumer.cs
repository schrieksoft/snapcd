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
/// Fanout consumer that responds to agent heartbeat requests.
/// Each server instance has its own temporary queue with this consumer.
/// Used to verify if a specific agent connection is still active on this server.
/// </summary>
public class AgentHeartbeatConsumer : IConsumer<AgentHeartbeatRequest>
{
    private readonly ServerSettings _serverSettings;
    private readonly AgentConnectionRepositoryFactory _connectionRepositoryFactory;
    private readonly ILogger<AgentHeartbeatConsumer> _logger;

    public AgentHeartbeatConsumer(
        IOptions<ServerSettings> serverSettings,
        AgentConnectionRepositoryFactory connectionRepositoryFactory,
        ILogger<AgentHeartbeatConsumer> logger)
    {
        _serverSettings = serverSettings.Value;
        _connectionRepositoryFactory = connectionRepositoryFactory;
        _logger = logger;
    }

    public async Task Consume(ConsumeContext<AgentHeartbeatRequest> context)
    {
        var request = context.Message;

        _logger.LogDebug(
            "Received heartbeat request for agent {AgentInstanceName} (ID: {AgentId}) on server {ServerInstanceId}",
            request.InstanceName,
            request.AgentId,
            request.ServerInstanceId);

        using var connectionRepository = _connectionRepositoryFactory.Create();
        var connection = await connectionRepository.GetActiveConnection(
            request.OrganizationId,
            request.AgentId,
            request.InstanceName);

        var isConnected = connection != null && connection.ServerInstanceId == _serverSettings.InstanceId;

        _logger.LogDebug(
            "Heartbeat check result for agent {AgentInstanceName}: IsConnected={IsConnected}",
            request.InstanceName,
            isConnected);

        await context.RespondAsync(new ServerHeartbeatResponse
        {
            IsConnected = isConnected
        });
    }
}
