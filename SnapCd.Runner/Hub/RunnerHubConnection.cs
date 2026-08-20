// SPDX-License-Identifier: LicenseRef-Snap-CD-Source-Available-1.1
// Copyright (c) 2026 Karl Schriek / Schrieksoft.
// No license is granted to use this file, in whole or in part, (a) as training, fine-tuning, retrieval, or
// embedding data for any machine-learning model, or (b) as input to any machine-learning model, agent, or automated
// system for the purpose of producing a derivative work or reimplementation that is not otherwise permitted by the
// Snap CD Source-Available License (including any Competing Product as defined therein). Contact info@snapcd.io
// for terms covering either use.

using System.Collections.Concurrent;
using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;
using SnapCd.Contracts.Constants;
using SnapCd.Contracts.Dto.Misc;
using SnapCd.Contracts.RunnerRequests;
using SnapCd.Contracts.RunnerRequests.SplitMonolith;
using SnapCd.Runner.Constants;
using SnapCd.Runner.Services;
using SnapCd.Runner.Settings;

namespace SnapCd.Runner.Hub;

/// <summary>
/// SignalR client for bidirectional communication with Snap CD Server.
/// Handles connection, reconnection, and log sending with buffering.
/// </summary>
public class RunnerHubConnection : IAsyncDisposable
{
    private readonly ILogger<RunnerHubConnection> _logger;
    private readonly RunnerSettings _runnerSettings;
    private readonly ServerSettings _serverSettings;
    private readonly IMemoryCache _memoryCache;
    private readonly ILoggerFactory _loggerFactory;
    // Lazy because the DI graph has a cycle: Tasks -> IJobLogStream -> RunnerHubConnection -> Tasks.
    // Tasks is only dereferenced inside the .On<>() handlers registered in StartAsync, by which
    // time the graph is fully built — so deferring resolution to first access is safe.
    private readonly Lazy<Tasks.Tasks> _tasks;

    private HubConnection? _connection;
    private bool _isDisposing;
    private int _restarting;

    // Log buffering
    private readonly ConcurrentQueue<LogEntryDto> _logBuffer = new();
    private const int MaxLogBufferSize = 10000;
    private int _droppedLogCount = 0;

    private readonly ProcessRegistry _processRegistry;

    public RunnerHubConnection(
        ILogger<RunnerHubConnection> logger,
        IOptions<RunnerSettings> runnerSettings,
        IOptions<ServerSettings> serverSettings,
        ILoggerFactory loggerFactory,
        IMemoryCache memoryCache,
        ProcessRegistry processRegistry,
        Lazy<Tasks.Tasks> tasks)
    {
        _logger = logger;
        _loggerFactory = loggerFactory;
        _processRegistry = processRegistry;
        _runnerSettings = runnerSettings.Value;
        _serverSettings = serverSettings.Value;
        _memoryCache = memoryCache;
        _tasks = tasks;
    }

    /// <summary>
    /// Start the SignalR connection to the server
    /// </summary>
    public async Task StartAsync(CancellationToken cancellationToken = default)
    {
        if (_connection != null)
        {
            _logger.LogWarning("Already connected to server");
            return;
        }

        var hubUrl = $"{_serverSettings.Url}/runnerhub" +
                     $"?organization_id={_runnerSettings.OrganizationId}" +
                     $"&runner_id={_runnerSettings.Id}" +
                     $"&runner_instance={Uri.EscapeDataString(_runnerSettings.Instance)}";

        _logger.LogDebug("Connecting to SignalR hub at {HubUrl}", hubUrl);

        _connection = new HubConnectionBuilder()
            .WithUrl(hubUrl, options =>
            {
                // Provide access token for authentication
                options.AccessTokenProvider = () =>
                {
                    var token = _memoryCache.Get<string>(MemoryCacheConstants.AccessTokenCacheKey);
                    if (string.IsNullOrEmpty(token)) _logger.LogWarning("Access token not found in cache during connection");
                    return Task.FromResult(token);
                };

                // Configure HTTP client
                options.HttpMessageHandlerFactory = handler =>
                {
                    if (handler is HttpClientHandler clientHandler)
                    {
                        // Configure any HTTP client settings here if needed
                    }

                    return handler;
                };
            })
            .WithAutomaticReconnect(new RetryPolicy())
            .Build();

        // Handle reconnection events
        _connection.Reconnecting += async error =>
        {
            var logger = _loggerFactory.CreateLogger<RunnerHubConnection>();
            logger.LogWarning(error, "SignalR connection lost, reconnecting...");
            await Task.CompletedTask;
        };

        _connection.Reconnected += async connectionId =>
        {
            var logger = _loggerFactory.CreateLogger<RunnerHubConnection>();
            logger.LogInformation("SignalR reconnected with connection ID {ConnectionId}", connectionId);

            // Automatic reconnect reuses the token the connection was built with; the server
            // authenticates once per connection, so a stale one leaves the runner connected but
            // unable to invoke anything. Rebuild so AccessTokenProvider supplies a current token.
            var expiry = _memoryCache.Get<DateTime?>(MemoryCacheConstants.AccessTokenExpiryCacheKey);
            if (expiry.HasValue && expiry.Value <= DateTime.UtcNow.AddMinutes(1))
            {
                logger.LogWarning(
                    "Reconnected with a token expiring at {Expiry}; rebuilding the connection to pick up a fresh one",
                    expiry.Value);

                if (Interlocked.CompareExchange(ref _restarting, 1, 0) == 0)
                    _ = RestartAfterCloseAsync();
                return;
            }

            // Flush buffered logs
            await FlushLogBufferAsync();
        };

        _connection.Closed += async error =>
        {
            if (_isDisposing)
            {
                _logger.LogDebug("SignalR connection closed (disposing)");
                return;
            }

            var logger = _loggerFactory.CreateLogger<RunnerHubConnection>();
            logger.LogError(error, "SignalR connection closed unexpectedly; restarting the connection");

            // WithAutomaticReconnect covers transient drops only: once the connection reaches the
            // Closed state it is finished, so without restarting here the runner stays alive but
            // permanently disconnected. The restart stops the old connection, which raises Closed
            // again, so only one loop is allowed to run.
            if (Interlocked.CompareExchange(ref _restarting, 1, 0) == 0)
                _ = RestartAfterCloseAsync();
        };

        // Register handler for GetDefinitiveRevision
        _connection.On<GetDefinitiveRevisionRequest>(RunnerEndpoints.GetDefinitiveRevision, (request) =>
            {
                Task.Run(async () => { await _tasks.Value.GetDefinitiveRevision(request, _connection); });
                return Task.CompletedTask;
            }
        );

        // Register handler for GetModule
        _connection.On<GetModuleRequestBase>(RunnerEndpoints.GetModule, (request) =>
            {
                Task.Run(async () => { await _tasks.Value.GetModule(request, _connection); });
                return Task.CompletedTask;
            }
        );

        // Register handler for Init
        _connection.On<InitRequestBase>(RunnerEndpoints.Init, (request) =>
            {
                Task.Run(async () => { await _tasks.Value.Init(request, _connection); });
                return Task.CompletedTask;
            }
        );

        // Register handler for Validate
        _connection.On<ValidateRequestBase>(RunnerEndpoints.Validate, (request) =>
            {
                Task.Run(async () => { await _tasks.Value.Validate(request, _connection); });
                return Task.CompletedTask;
            }
        );

        // Register handler for PolicyValidate
        _connection.On<PlanEmptyVerifyRequestBase>(RunnerEndpoints.PlanEmptyVerify, (request) =>
            {
                Task.Run(async () => { await _tasks.Value.PlanEmptyVerify(request, _connection); });
                return Task.CompletedTask;
            }
        );

        // Register handler for PolicyValidate
        _connection.On<PolicyValidateRequestBase>(RunnerEndpoints.PolicyValidate, (request) =>
            {
                Task.Run(async () => { await _tasks.Value.PolicyValidate(request, _connection); });
                return Task.CompletedTask;
            }
        );

        // Register handler for Input
        _connection.On<VariablesRequestBase>(RunnerEndpoints.Variables, (request) =>
            {
                Task.Run(async () => { await _tasks.Value.Variables(request, _connection); });
                return Task.CompletedTask;
            }
        );

        // Register handler for Plan
        _connection.On<PlanRequestBase>(RunnerEndpoints.Plan, (request) =>
            {
                Task.Run(async () => { await _tasks.Value.Plan(request, _connection); });
                return Task.CompletedTask;
            }
        );

        // Register handler for PlanDestroy
        _connection.On<PlanDestroyRequestBase>(RunnerEndpoints.PlanDestroy, (request) =>
            {
                Task.Run(async () => { await _tasks.Value.PlanDestroy(request, _connection); });
                return Task.CompletedTask;
            }
        );

        // Register handler for ApplyFromPlan
        _connection.On<ApplyFromPlanRequestBase>(RunnerEndpoints.ApplyFromPlan, (request) =>
            {
                Task.Run(async () => { await _tasks.Value.ApplyFromPlan(request, _connection); });
                return Task.CompletedTask;
            }
        );

        // Register handler for DestroyFromPlan
        _connection.On<DestroyFromPlanRequestBase>(RunnerEndpoints.DestroyFromPlan, (request) =>
            {
                Task.Run(async () => { await _tasks.Value.DestroyFromPlan(request, _connection); });
                return Task.CompletedTask;
            }
        );

        // Register handler for Output
        _connection.On<OutputRequestBase>(RunnerEndpoints.Output, (request) =>
            {
                Task.Run(async () => { await _tasks.Value.Output(request, _connection); });
                return Task.CompletedTask;
            }
        );

        // Answer liveness pings so the server can tell a live connection from a wedged one.
        _connection.On<Guid>(RunnerEndpoints.Ping, (pingId) =>
            {
                Task.Run(async () =>
                {
                    try
                    {
                        if (_connection is not null)
                            await _connection.InvokeAsync("Pong", pingId);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogDebug(ex, "Could not answer liveness ping {PingId}", pingId);
                    }
                });
                return Task.CompletedTask;
            }
        );

        // Register handler for SourceRefresh (stateless operation)
        _connection.On<SourceRefreshRequest>(RunnerEndpoints.SourceRefresh, (request) =>
            {
                Task.Run(async () => { await _tasks.Value.SourceRefresh(request, _connection); });
                return Task.CompletedTask;
            }
        );

        // Register handler for CancelKill
        _connection.On<CancelKillRequest>(RunnerEndpoints.CancelKill, (request) =>
            {
                Task.Run(async () => { await _tasks.Value.CancelKill(request, _connection); });
                return Task.CompletedTask;
            }
        );

        // Register handler for CancelGraceful
        _connection.On<CancelGracefulRequest>(RunnerEndpoints.CancelGraceful, (request) =>
            {
                Task.Run(async () => { await _tasks.Value.CancelGraceful(request, _connection); });
                return Task.CompletedTask;
            }
        );

        // Start the connection
        await _connection.StartAsync(cancellationToken);
        _logger.LogInformation("Connected to SignalR hub");

        // Flush any buffered logs from before connection
        await FlushLogBufferAsync();
    }

    /// <summary>
    /// Stop the SignalR connection
    /// </summary>
    public async Task StopAsync()
    {
        _isDisposing = true;

        if (_connection != null)
        {
            await _connection.StopAsync();
            await _connection.DisposeAsync();
            _connection = null;
        }

        _logger.LogInformation("Disconnected from SignalR hub");
    }

    /// <summary>
    /// Disconnect and reconnect to refresh the connection with a new token from cache.
    /// Used when token expiration is detected.
    /// </summary>
    public async Task DisconnectAndReconnectAsync(CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Disconnecting and reconnecting to refresh authentication token");

        // Temporarily disable disposing flag to allow reconnection
        var wasDisposing = _isDisposing;
        _isDisposing = false;

        try
        {
            // Disconnect current connection
            if (_connection != null)
            {
                await _connection.StopAsync();
                await _connection.DisposeAsync();
                _connection = null;
            }

            // Small delay to ensure clean disconnect
            await Task.Delay(100, cancellationToken);

            // Reconnect - will use fresh token from cache via AccessTokenProvider
            await StartAsync(cancellationToken);

            _logger.LogInformation("Successfully reconnected with refreshed token");
        }
        finally
        {
            _isDisposing = wasDisposing;
        }
    }

    /// <summary>
    /// Rebuilds the connection after it has closed for good, retrying until it succeeds. The
    /// server rejects a connection it cannot authorize, which is indistinguishable here from a
    /// server that is still starting up, so this keeps trying rather than stranding the runner.
    /// </summary>
    private async Task RestartAfterCloseAsync()
    {
        try
        {
            var delay = TimeSpan.FromSeconds(5);
            var maxDelay = TimeSpan.FromSeconds(60);

            while (!_isDisposing)
            {
                await Task.Delay(delay);
                if (_isDisposing) return;

                try
                {
                    await DisconnectAndReconnectAsync();
                    _logger.LogInformation("SignalR connection restarted after an unexpected close");
                    return;
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Could not restart the SignalR connection; retrying in {Delay}", delay);
                    delay = delay < maxDelay ? delay + delay : maxDelay;
                }
            }
        }
        finally
        {
            Interlocked.Exchange(ref _restarting, 0);
        }
    }

    /// <summary>
    /// Send a batch of log entries to the server via SignalR.
    /// If not connected, logs are buffered for sending when connection is restored.
    /// </summary>
    public async Task SendLogsAsync(List<LogEntryDto> logEntries)
    {
        if (logEntries == null || logEntries.Count == 0)
            return;

        // If not connected, buffer the logs
        if (_connection?.State != HubConnectionState.Connected)
        {
            foreach (var log in logEntries)
            {
                if (_logBuffer.Count >= MaxLogBufferSize)
                {
                    // Drop oldest log
                    _logBuffer.TryDequeue(out _);
                    _droppedLogCount++;
                }

                _logBuffer.Enqueue(log);
            }

            if (_droppedLogCount > 0) _logger.LogWarning("Dropped {Count} log entries due to buffer overflow", _droppedLogCount);

            return;
        }

        try
        {
            await _connection.InvokeAsync(ServerEndpoints.AddLogs, logEntries);
            _logger.LogTrace("Sent {Count} log entries to server", logEntries.Count);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error sending logs to server, buffering for retry");

            // Buffer the logs for retry
            foreach (var log in logEntries)
                if (_logBuffer.Count < MaxLogBufferSize)
                    _logBuffer.Enqueue(log);
        }
    }

    private async Task FlushLogBufferAsync()
    {
        if (_logBuffer.IsEmpty || _connection?.State != HubConnectionState.Connected)
            return;

        var logsToSend = new List<LogEntryDto>();
        while (_logBuffer.TryDequeue(out var log) && logsToSend.Count < 100) logsToSend.Add(log);

        if (logsToSend.Count > 0)
        {
            _logger.LogDebug("Flushing {Count} buffered log entries", logsToSend.Count);
            try
            {
                await _connection.InvokeAsync("SendLogs", logsToSend);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error flushing buffered logs, will retry later");
                // Re-queue the logs
                foreach (var log in logsToSend) _logBuffer.Enqueue(log);
            }
        }

        if (_droppedLogCount > 0)
        {
            _logger.LogWarning("Total of {Count} log entries were dropped due to buffer overflow", _droppedLogCount);
            _droppedLogCount = 0;
        }
    }

    /// <summary>
    /// Invoke a hub method with the specified arguments.
    /// </summary>
    public async Task InvokeHubMethodAsync(string methodName, params object[] args)
    {
        if (_connection == null)
        {
            _logger.LogWarning("Cannot invoke hub method {MethodName} - not connected", methodName);
            throw new InvalidOperationException("Not connected to SignalR hub");
        }

        await _connection.InvokeAsync(methodName, args);
    }

    public async ValueTask DisposeAsync()
    {
        await StopAsync();
    }

    /// <summary>
    /// Custom retry policy for SignalR reconnection
    /// </summary>
    private class RetryPolicy : IRetryPolicy
    {
        private readonly TimeSpan[] _retryDelays = new[]
        {
            TimeSpan.FromSeconds(1),
            TimeSpan.FromSeconds(2),
            TimeSpan.FromSeconds(5),
            TimeSpan.FromSeconds(10),
            TimeSpan.FromSeconds(20)
        };

        public TimeSpan? NextRetryDelay(RetryContext retryContext)
        {
            // Use exponential backoff up to 30 seconds
            if (retryContext.PreviousRetryCount < _retryDelays.Length) return _retryDelays[retryContext.PreviousRetryCount];

            // After that, retry every 30 seconds indefinitely
            return TimeSpan.FromSeconds(30);
        }
    }
}