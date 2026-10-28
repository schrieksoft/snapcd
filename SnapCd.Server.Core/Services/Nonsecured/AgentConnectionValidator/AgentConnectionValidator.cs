// SPDX-License-Identifier: LicenseRef-Snap-CD-Source-Available-1.1
// Copyright (c) 2026 Karl Schriek / Schrieksoft.
// No license is granted to use this file, in whole or in part, (a) as training, fine-tuning, retrieval, or
// embedding data for any machine-learning model, or (b) as input to any machine-learning model, agent, or automated
// system for the purpose of producing a derivative work or reimplementation that is not otherwise permitted by the
// Snap CD Source-Available License (including any Competing Product as defined therein). Contact info@snapcd.io
// for terms covering either use.

using System.Text.Json;
using MassTransit;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Options;
using SnapCd.Server.Core.Events.Server;
using SnapCd.Server.Core.Repositories.Organizations.Nonsecured;
using SnapCd.Server.Core.Settings;

namespace SnapCd.Server.Core.Services.AgentConnectionValidator;

/// <summary>
/// Validates agent connection attempts, handling duplicate detection and rate limiting.
/// </summary>
public class AgentConnectionValidator
{
    private readonly AgentConnectionRepositoryFactory _repositoryFactory;
    private readonly IRequestClient<AgentHeartbeatRequest> _requestClient;
    private readonly ServerSettings _serverSettings;
    private readonly IDistributedCache _cache;
    private readonly ILogger<AgentConnectionValidator> _logger;

    private readonly TimeSpan _rateLimitWindow = TimeSpan.FromMinutes(1);
    private const int MaxAttemptsPerWindow = 5;

    private readonly TimeSpan _heartbeatTimeout = TimeSpan.FromSeconds(5);

    public AgentConnectionValidator(
        AgentConnectionRepositoryFactory repositoryFactory,
        IRequestClient<AgentHeartbeatRequest> requestClient,
        IOptions<ServerSettings> serverSettings,
        IDistributedCache cache,
        ILogger<AgentConnectionValidator> logger)
    {
        _repositoryFactory = repositoryFactory;
        _requestClient = requestClient;
        _serverSettings = serverSettings.Value;
        _cache = cache;
        _logger = logger;
    }

    public async Task<AgentConnectionValidationResult> ValidateConnection(
        Guid organizationId,
        Guid agentId,
        string instanceName)
    {
        var agentKey = $"{organizationId}:{agentId}:{instanceName}";

        if (await IsRateLimitedAsync(agentKey))
        {
            _logger.LogWarning(
                "Connection attempt rate limit exceeded for agent {InstanceName} (ID: {AgentId})",
                instanceName,
                agentId);

            return AgentConnectionValidationResult.Rejected("Rate limit exceeded - too many connection attempts");
        }

        await RecordConnectionAttemptAsync(agentKey);

        using var repository = _repositoryFactory.Create();
        var existingConnection = await repository.GetActiveConnection(organizationId, agentId, instanceName);

        if (existingConnection == null)
        {
            _logger.LogDebug(
                "No existing connection found for agent {InstanceName} (ID: {AgentId}) - allowing connection",
                instanceName,
                agentId);
            return AgentConnectionValidationResult.Allowed();
        }

        if (existingConnection.ServerInstanceId == _serverSettings.InstanceId)
        {
            _logger.LogDebug(
                "Existing connection is on this server for agent {InstanceName} (ID: {AgentId}) - cleaning up before reconnection",
                instanceName,
                agentId);
            await repository.DeleteConnection(organizationId, agentId, instanceName);
            return AgentConnectionValidationResult.Allowed();
        }

        _logger.LogInformation(
            "Existing connection found for agent {InstanceName} (ID: {AgentId}) on server {ServerInstanceId} - sending heartbeat",
            instanceName,
            agentId,
            existingConnection.ServerInstanceId);

        try
        {
            var response = await _requestClient.GetResponse<ServerHeartbeatResponse>(
                new AgentHeartbeatRequest
                {
                    ServerInstanceId = existingConnection.ServerInstanceId,
                    OrganizationId = organizationId,
                    AgentId = agentId,
                    InstanceName = instanceName
                },
                timeout: _heartbeatTimeout);

            if (response.Message.IsConnected)
            {
                _logger.LogWarning(
                    "Agent {InstanceName} (ID: {AgentId}) is still connected to server {ServerInstanceId} - rejecting duplicate connection",
                    instanceName,
                    agentId,
                    existingConnection.ServerInstanceId);

                return AgentConnectionValidationResult.Rejected("Agent is already connected to another server");
            }

            _logger.LogInformation(
                "Server {ServerInstanceId} reports agent {InstanceName} is not connected - cleaning up stale connection",
                existingConnection.ServerInstanceId,
                instanceName);
        }
        catch (RequestTimeoutException)
        {
            _logger.LogWarning(
                "Heartbeat timeout for server {ServerInstanceId} - assuming server crashed, allowing connection for agent {InstanceName}",
                existingConnection.ServerInstanceId,
                instanceName);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "Error sending heartbeat to server {ServerInstanceId} - assuming server crashed, allowing connection for agent {InstanceName}",
                existingConnection.ServerInstanceId,
                instanceName);
        }

        await repository.DeleteConnection(organizationId, agentId, instanceName);
        return AgentConnectionValidationResult.Allowed();
    }

    private async Task<bool> IsRateLimitedAsync(string agentKey)
    {
        var cacheKey = $"ratelimit:agent:{agentKey}";
        var now = DateTime.UtcNow;

        var data = await _cache.GetStringAsync(cacheKey);
        if (string.IsNullOrEmpty(data))
            return false;

        var timestamps = JsonSerializer.Deserialize<List<long>>(data) ?? [];
        var windowStart = now.Add(-_rateLimitWindow).Ticks;
        var recentAttempts = timestamps.Count(t => t >= windowStart);

        return recentAttempts >= MaxAttemptsPerWindow;
    }

    private async Task RecordConnectionAttemptAsync(string agentKey)
    {
        var cacheKey = $"ratelimit:agent:{agentKey}";
        var now = DateTime.UtcNow;
        var windowStart = now.Add(-_rateLimitWindow).Ticks;

        var data = await _cache.GetStringAsync(cacheKey);
        var timestamps = string.IsNullOrEmpty(data)
            ? []
            : JsonSerializer.Deserialize<List<long>>(data) ?? [];

        timestamps = timestamps.Where(t => t >= windowStart).ToList();
        timestamps.Add(now.Ticks);

        await _cache.SetStringAsync(cacheKey, JsonSerializer.Serialize(timestamps),
            new DistributedCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = _rateLimitWindow
            });
    }
}

public class AgentConnectionValidationResult
{
    public bool IsAllowed { get; private set; }
    public string? RejectionReason { get; private set; }

    private AgentConnectionValidationResult() { }

    public static AgentConnectionValidationResult Allowed()
    {
        return new AgentConnectionValidationResult { IsAllowed = true };
    }

    public static AgentConnectionValidationResult Rejected(string reason)
    {
        return new AgentConnectionValidationResult { IsAllowed = false, RejectionReason = reason };
    }
}
