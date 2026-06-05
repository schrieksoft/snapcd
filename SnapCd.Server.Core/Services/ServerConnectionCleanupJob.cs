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

namespace SnapCd.Server.Core.Services;

/// <summary>
/// Background job that periodically checks all server instances for connectivity.
/// Cleans up stale connections from crashed servers that didn't gracefully disconnect.
/// Runs as a singleton across all server instances (Hangfire ensures only one executes at a time).
/// </summary>
public class ServerConnectionCleanupJob
{
    private readonly RunnerConnectionRepositoryFactory _repositoryFactory;
    private readonly AgentConnectionRepositoryFactory _agentRepositoryFactory;
    private readonly IRequestClient<ServerHeartbeatRequest> _requestClient;
    private readonly ServerSettings _serverSettings;
    private readonly ILogger<ServerConnectionCleanupJob> _logger;

    private readonly TimeSpan _heartbeatTimeout = TimeSpan.FromSeconds(15);

    public ServerConnectionCleanupJob(
        RunnerConnectionRepositoryFactory repositoryFactory,
        AgentConnectionRepositoryFactory agentRepositoryFactory,
        IRequestClient<ServerHeartbeatRequest> requestClient,
        IOptions<ServerSettings> serverSettings,
        ILogger<ServerConnectionCleanupJob> logger)
    {
        _repositoryFactory = repositoryFactory;
        _agentRepositoryFactory = agentRepositoryFactory;
        _requestClient = requestClient;
        _serverSettings = serverSettings.Value;
        _logger = logger;
    }

    public async Task ExecuteJob()
    {
        _logger.LogDebug("Starting server connection cleanup job");

        try
        {
            using var repository = _repositoryFactory.Create();
            using var agentRepository = _agentRepositoryFactory.Create();

            // Get all distinct server instance IDs that have active runner or agent connections
            var serverInstanceIds = (await repository.GetDistinctServerInstanceIds())
                .Union(await agentRepository.GetDistinctServerInstanceIds())
                .Distinct()
                .ToList();

            if (serverInstanceIds.Count == 0)
            {
                _logger.LogTrace("No active connections found in database");
                return;
            }

            _logger.LogDebug(
                "Found {Count} server instance(s) with active connections",
                serverInstanceIds.Count);

            // Check each server (except this one)
            var cleanupTasks = serverInstanceIds
                .Where(id => id != _serverSettings.InstanceId)
                .Select(serverInstanceId => CheckAndCleanupServer(serverInstanceId, repository, agentRepository));

            await Task.WhenAll(cleanupTasks);

            _logger.LogDebug("Server connection cleanup job completed");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during server connection cleanup");
        }
    }

    private async Task CheckAndCleanupServer(
        Guid serverInstanceId,
        RunnerConnectionRepository repository,
        AgentConnectionRepository agentRepository)
    {
        try
        {
            _logger.LogDebug("Sending heartbeat to server {ServerInstanceId}", serverInstanceId);

            // Send heartbeat request to the server
            // Note: We send a dummy request just to check if the server is alive
            // The actual connection verification happens in the ServerHeartbeatConsumer
            var response = await _requestClient.GetResponse<ServerHeartbeatResponse>(
                new ServerHeartbeatRequest
                {
                    ServerInstanceId = serverInstanceId,
                    OrganizationId = Guid.Empty, // Not used for cleanup check
                    RunnerId = Guid.Empty, // Not used for cleanup check
                    InstanceName = string.Empty // Not used for cleanup check
                },
                timeout: _heartbeatTimeout);

            _logger.LogDebug("Server {ServerInstanceId} is alive", serverInstanceId);
        }
        catch (RequestTimeoutException)
        {
            _logger.LogWarning(
                "Server {ServerInstanceId} did not respond to heartbeat - cleaning up connections",
                serverInstanceId);

            await CleanupServerConnections(serverInstanceId, repository, agentRepository);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "Error checking server {ServerInstanceId} - assuming crashed, cleaning up connections",
                serverInstanceId);

            await CleanupServerConnections(serverInstanceId, repository, agentRepository);
        }
    }

    private async Task CleanupServerConnections(
        Guid serverInstanceId,
        RunnerConnectionRepository repository,
        AgentConnectionRepository agentRepository)
    {
        try
        {
            await repository.DeleteConnectionsByServerId(serverInstanceId);
            await agentRepository.DeleteConnectionsByServerId(serverInstanceId);
            _logger.LogInformation(
                "Cleaned up runner + agent connections for crashed server {ServerInstanceId}",
                serverInstanceId);
        }
        catch (Exception cleanupEx)
        {
            _logger.LogError(cleanupEx,
                "Error cleaning up connections for server {ServerInstanceId}",
                serverInstanceId);
        }
    }
}
