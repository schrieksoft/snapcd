using System.Text.Json;
using MassTransit;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Options;
using SnapCd.Server.Core.Events.Server;
using SnapCd.Server.Core.Repositories.Organizations.Nonsecured;
using SnapCd.Server.Core.Settings;

namespace SnapCd.Server.Core.Services.RunnerConnectionValidator;

/// <summary>
/// Validates runner connection attempts, handling duplicate detection and rate limiting.
/// </summary>
public class RunnerConnectionValidator
{
    private readonly RunnerConnectionRepositoryFactory _repositoryFactory;
    private readonly IRequestClient<ServerHeartbeatRequest> _requestClient;
    private readonly ServerSettings _serverSettings;
    private readonly IDistributedCache _cache;
    private readonly ILogger<RunnerConnectionValidator> _logger;

    private readonly TimeSpan _rateLimitWindow = TimeSpan.FromMinutes(1);
    private const int MaxAttemptsPerWindow = 5;

    // Timeout for server heartbeat requests
    private readonly TimeSpan _heartbeatTimeout = TimeSpan.FromSeconds(5);

    public RunnerConnectionValidator(
        RunnerConnectionRepositoryFactory repositoryFactory,
        IRequestClient<ServerHeartbeatRequest> requestClient,
        IOptions<ServerSettings> serverSettings,
        IDistributedCache cache,
        ILogger<RunnerConnectionValidator> logger)
    {
        _repositoryFactory = repositoryFactory;
        _requestClient = requestClient;
        _serverSettings = serverSettings.Value;
        _cache = cache;
        _logger = logger;
    }

    /// <summary>
    /// Validates a runner connection attempt.
    /// Returns true if the connection should be allowed, false if it should be rejected.
    /// </summary>
    public async Task<RunnerConnectionValidationResult> ValidateConnection(
        Guid organizationId,
        Guid runnerId,
        string instanceName)
    {
        var runnerKey = $"{organizationId}:{runnerId}:{instanceName}";

        // Check rate limiting first
        if (await IsRateLimitedAsync(runnerKey))
        {
            _logger.LogWarning(
                "Connection attempt rate limit exceeded for runner {InstanceName} (ID: {RunnerId})",
                instanceName,
                runnerId);

            return RunnerConnectionValidationResult.Rejected("Rate limit exceeded - too many connection attempts");
        }

        // Record this attempt
        await RecordConnectionAttemptAsync(runnerKey);

        // Check for existing connection in database
        using var repository = _repositoryFactory.Create();
        var existingConnection = await repository.GetActiveConnection(organizationId, runnerId, instanceName);

        if (existingConnection == null)
        {
            _logger.LogDebug(
                "No existing connection found for runner {InstanceName} (ID: {RunnerId}) - allowing connection",
                instanceName,
                runnerId);
            return RunnerConnectionValidationResult.Allowed();
        }

        // Check if it's the same server (reconnection scenario)
        if (existingConnection.ServerInstanceId == _serverSettings.InstanceId)
        {
            _logger.LogDebug(
                "Existing connection is on this server for runner {InstanceName} (ID: {RunnerId}) - allowing reconnection",
                instanceName,
                runnerId);
            return RunnerConnectionValidationResult.Allowed();
        }

        // Different server - send heartbeat request to verify if still alive
        _logger.LogInformation(
            "Existing connection found for runner {InstanceName} (ID: {RunnerId}) on server {ServerInstanceId} - sending heartbeat",
            instanceName,
            runnerId,
            existingConnection.ServerInstanceId);

        try
        {
            var response = await _requestClient.GetResponse<ServerHeartbeatResponse>(
                new ServerHeartbeatRequest
                {
                    ServerInstanceId = existingConnection.ServerInstanceId,
                    OrganizationId = organizationId,
                    RunnerId = runnerId,
                    InstanceName = instanceName
                },
                timeout: _heartbeatTimeout);

            if (response.Message.IsConnected)
            {
                _logger.LogWarning(
                    "Runner {InstanceName} (ID: {RunnerId}) is still connected to server {ServerInstanceId} - rejecting duplicate connection",
                    instanceName,
                    runnerId,
                    existingConnection.ServerInstanceId);

                return RunnerConnectionValidationResult.Rejected("Runner is already connected to another server");
            }

            _logger.LogInformation(
                "Server {ServerInstanceId} reports runner {InstanceName} is not connected - cleaning up stale connection",
                existingConnection.ServerInstanceId,
                instanceName);
        }
        catch (RequestTimeoutException)
        {
            _logger.LogWarning(
                "Heartbeat timeout for server {ServerInstanceId} - assuming server crashed, allowing connection for runner {InstanceName}",
                existingConnection.ServerInstanceId,
                instanceName);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "Error sending heartbeat to server {ServerInstanceId} - assuming server crashed, allowing connection for runner {InstanceName}",
                existingConnection.ServerInstanceId,
                instanceName);
        }

        // Server is dead or runner not connected - delete old connection and allow new one
        await repository.DeleteConnection(organizationId, runnerId, instanceName);
        return RunnerConnectionValidationResult.Allowed();
    }

    private async Task<bool> IsRateLimitedAsync(string runnerKey)
    {
        var cacheKey = $"ratelimit:{runnerKey}";
        var now = DateTime.UtcNow;

        var data = await _cache.GetStringAsync(cacheKey);
        if (string.IsNullOrEmpty(data))
            return false;

        var timestamps = JsonSerializer.Deserialize<List<long>>(data) ?? [];
        var windowStart = now.Add(-_rateLimitWindow).Ticks;
        var recentAttempts = timestamps.Count(t => t >= windowStart);

        return recentAttempts >= MaxAttemptsPerWindow;
    }

    private async Task RecordConnectionAttemptAsync(string runnerKey)
    {
        var cacheKey = $"ratelimit:{runnerKey}";
        var now = DateTime.UtcNow;
        var windowStart = now.Add(-_rateLimitWindow).Ticks;

        var data = await _cache.GetStringAsync(cacheKey);
        var timestamps = string.IsNullOrEmpty(data)
            ? []
            : JsonSerializer.Deserialize<List<long>>(data) ?? [];

        // Filter to window and add new timestamp
        timestamps = timestamps.Where(t => t >= windowStart).ToList();
        timestamps.Add(now.Ticks);

        await _cache.SetStringAsync(cacheKey, JsonSerializer.Serialize(timestamps),
            new DistributedCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = _rateLimitWindow
            });
    }
}

/// <summary>
/// Result of runner connection validation.
/// </summary>
public class RunnerConnectionValidationResult
{
    public bool IsAllowed { get; private set; }
    public string? RejectionReason { get; private set; }

    private RunnerConnectionValidationResult() { }

    public static RunnerConnectionValidationResult Allowed()
    {
        return new RunnerConnectionValidationResult { IsAllowed = true };
    }

    public static RunnerConnectionValidationResult Rejected(string reason)
    {
        return new RunnerConnectionValidationResult { IsAllowed = false, RejectionReason = reason };
    }
}
