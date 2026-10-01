// SPDX-License-Identifier: LicenseRef-Snap-CD-Source-Available-1.1
// Copyright (c) 2026 Karl Schriek / Schrieksoft.
// No license is granted to use this file, in whole or in part, (a) as training, fine-tuning, retrieval, or
// embedding data for any machine-learning model, or (b) as input to any machine-learning model, agent, or automated
// system for the purpose of producing a derivative work or reimplementation that is not otherwise permitted by the
// Snap CD Source-Available License (including any Competing Product as defined therein). Contact info@snapcd.io
// for terms covering either use.

using SnapCd.Contracts.RunnerRequests.StateMigrations;
using System.Collections.Concurrent;
using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;
using SnapCd.Contracts.Constants;
using SnapCd.Contracts.Dto.Misc;
using SnapCd.Contracts.RunnerRequests;
using SnapCd.Contracts.RunnerRequests.SplitMigrate;
using SnapCd.Contracts.Endpoints;
using SnapCd.Contracts.RunnerRequests.Transfers;
using SnapCd.Runner.Constants;
using SnapCd.Runner.Services;
using SnapCd.Runner.Settings;

using SnapCd.Contracts.Clients;

namespace SnapCd.Runner.Hub;

/// <summary>
/// SignalR client for bidirectional communication with Snap CD Server.
/// Handles connection, reconnection, and log sending with buffering.
/// </summary>
public class RunnerHubConnection :
    IAsyncDisposable,
    IApplyEndpoints,
    IDestroyEndpoints,
    ISplitEndpoints,
    ITransferEndpoints,
    ILookupAddressesEndpoints,
    IMoveEndpoints,
    IImportEndpoints,
    IRemoveEndpoints,
    IRunnerLifecycleEndpoints
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

    /// <summary>Replies go back over the connection the request arrived on.</summary>
    private RunnerHubClient Client => new(_connection!);

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
        var client = new RunnerHubClient(_connection!);

        _connection.On<GetDefinitiveRevisionRequest>(nameof(IApplyEndpoints.ApplyGetDefinitiveRevision),
            request =>
            {
                Task.Run(() => ApplyGetDefinitiveRevision(request));
                return Task.CompletedTask;
            });

        _connection.On<GetDefinitiveRevisionRequest>(nameof(IDestroyEndpoints.DestroyGetDefinitiveRevision),
            request =>
            {
                Task.Run(() => DestroyGetDefinitiveRevision(request));
                return Task.CompletedTask;
            });


        _connection.On<GetModuleRequestBase>(nameof(IApplyEndpoints.ApplyGetModule),
            request =>
            {
                Task.Run(() => ApplyGetModule(request));
                return Task.CompletedTask;
            });

        _connection.On<InitRequestBase>(nameof(IApplyEndpoints.ApplyInit),
            request =>
            {
                Task.Run(() => ApplyInit(request));
                return Task.CompletedTask;
            });

        _connection.On<ValidateRequestBase>(nameof(IApplyEndpoints.ApplyValidate),
            request =>
            {
                Task.Run(() => ApplyValidate(request));
                return Task.CompletedTask;
            });

        _connection.On<VariablesRequestBase>(nameof(IApplyEndpoints.ApplyVariables),
            request =>
            {
                Task.Run(() => ApplyVariables(request));
                return Task.CompletedTask;
            });
        _connection.On<GetModuleRequestBase>(nameof(IDestroyEndpoints.DestroyGetModule),
            request =>
            {
                Task.Run(() => DestroyGetModule(request));
                return Task.CompletedTask;
            });

        _connection.On<InitRequestBase>(nameof(IDestroyEndpoints.DestroyInit),
            request =>
            {
                Task.Run(() => DestroyInit(request));
                return Task.CompletedTask;
            });

        _connection.On<ValidateRequestBase>(nameof(IDestroyEndpoints.DestroyValidate),
            request =>
            {
                Task.Run(() => DestroyValidate(request));
                return Task.CompletedTask;
            });

        _connection.On<VariablesRequestBase>(nameof(IDestroyEndpoints.DestroyVariables),
            request =>
            {
                Task.Run(() => DestroyVariables(request));
                return Task.CompletedTask;
            });

        // The manual families run the same checkout and init, each answering on its own endpoint.
        _connection.On<GetModuleRequestBase>(nameof(ILookupAddressesEndpoints.LookupAddressesGetModule),
            request =>
            {
                Task.Run(() => LookupAddressesGetModule(request));
                return Task.CompletedTask;
            });

        _connection.On<InitRequestBase>(nameof(ILookupAddressesEndpoints.LookupAddressesInit),
            request =>
            {
                Task.Run(() => LookupAddressesInit(request));
                return Task.CompletedTask;
            });
        _connection.On<GetModuleRequestBase>(nameof(IMoveEndpoints.MoveGetModule),
            request =>
            {
                Task.Run(() => MoveGetModule(request));
                return Task.CompletedTask;
            });

        _connection.On<InitRequestBase>(nameof(IMoveEndpoints.MoveInit),
            request =>
            {
                Task.Run(() => MoveInit(request));
                return Task.CompletedTask;
            });
        _connection.On<GetModuleRequestBase>(nameof(IImportEndpoints.ImportGetModule),
            request =>
            {
                Task.Run(() => ImportGetModule(request));
                return Task.CompletedTask;
            });

        _connection.On<InitRequestBase>(nameof(IImportEndpoints.ImportInit),
            request =>
            {
                Task.Run(() => ImportInit(request));
                return Task.CompletedTask;
            });
        _connection.On<GetModuleRequestBase>(nameof(IRemoveEndpoints.RemoveGetModule),
            request =>
            {
                Task.Run(() => RemoveGetModule(request));
                return Task.CompletedTask;
            });

        _connection.On<InitRequestBase>(nameof(IRemoveEndpoints.RemoveInit),
            request =>
            {
                Task.Run(() => RemoveInit(request));
                return Task.CompletedTask;
            });

        // Register handler for PolicyValidate
        _connection.On<SplitRefactorValidateRequestBase>(nameof(ISplitEndpoints.SplitRefactorValidate),
            request =>
            {
                Task.Run(() => SplitRefactorValidate(request));
                return Task.CompletedTask;
            });

        _connection.On<SplitRefactorDiffRequestBase>(nameof(ISplitEndpoints.SplitRefactorDiff),
            request =>
            {
                Task.Run(() => SplitRefactorDiff(request));
                return Task.CompletedTask;
            });





        // A transfer runs two Modules under one job, so its replies name the Module as well.
        _connection.On<GetModuleRequestBase>(nameof(ITransferEndpoints.TransferGetModule),
            request =>
            {
                Task.Run(() => TransferGetModule(request));
                return Task.CompletedTask;
            });

        // A transfer runs two Modules under one job, so its replies name the Module as well.
        _connection.On<InitRequestBase>(nameof(ITransferEndpoints.TransferInit),
            request =>
            {
                Task.Run(() => TransferInit(request));
                return Task.CompletedTask;
            });

        // A transfer runs two Modules under one job, so its replies name the Module as well.
        _connection.On<ValidateRequestBase>(nameof(ITransferEndpoints.TransferValidate),
            request =>
            {
                Task.Run(() => TransferValidate(request));
                return Task.CompletedTask;
            });


        _connection.On<TransferAnalyseMapRequestBase>(nameof(ITransferEndpoints.TransferAnalyseMap),
            request =>
            {
                Task.Run(() => TransferAnalyseMap(request));
                return Task.CompletedTask;
            });

        _connection.On<TransferMigrateMapRequestBase>(nameof(ITransferEndpoints.TransferMigrateMap),
            request =>
            {
                Task.Run(() => TransferMigrateMap(request));
                return Task.CompletedTask;
            });

        _connection.On<TransferMigrateProveRequestBase>(nameof(ITransferEndpoints.TransferMigrateProve),
            request =>
            {
                Task.Run(() => TransferMigrateProve(request));
                return Task.CompletedTask;
            });

        _connection.On<TransferMigrateRunRequestBase>(nameof(ITransferEndpoints.TransferMigrateRun),
            request =>
            {
                Task.Run(() => TransferMigrateRun(request));
                return Task.CompletedTask;
            });

        _connection.On<TransferMigrateVerifyRequestBase>(nameof(ITransferEndpoints.TransferMigrateVerify),
            request =>
            {
                Task.Run(() => TransferMigrateVerify(request));
                return Task.CompletedTask;
            });

        _connection.On<StateMoveRequestBase>(nameof(IMoveEndpoints.Move),
            request =>
            {
                Task.Run(() => Move(request));
                return Task.CompletedTask;
            });

        _connection.On<StateMoveRequestBase>(nameof(IImportEndpoints.Import),
            request =>
            {
                Task.Run(() => Import(request));
                return Task.CompletedTask;
            });

        _connection.On<StateMoveRequestBase>(nameof(IRemoveEndpoints.Remove),
            request =>
            {
                Task.Run(() => Remove(request));
                return Task.CompletedTask;
            });

        _connection.On<StateMoveRequestBase>(nameof(IMoveEndpoints.MoveDryRun),
            request =>
            {
                Task.Run(() => MoveDryRun(request));
                return Task.CompletedTask;
            });

        _connection.On<StateMoveRequestBase>(nameof(IRemoveEndpoints.RemoveDryRun),
            request =>
            {
                Task.Run(() => RemoveDryRun(request));
                return Task.CompletedTask;
            });

        _connection.On<StateMoveRequestBase>(nameof(IImportEndpoints.ImportPreCheck),
            request =>
            {
                Task.Run(() => ImportPreCheck(request));
                return Task.CompletedTask;
            });

        _connection.On<LookupAddressesRequestBase>(nameof(ILookupAddressesEndpoints.LookupAddresses),
            request =>
            {
                Task.Run(() => LookupAddresses(request));
                return Task.CompletedTask;
            });


        _connection.On<SplitMigrateMapRequestBase>(nameof(ISplitEndpoints.SplitMigrateMap),
            request =>
            {
                Task.Run(() => SplitMigrateMap(request));
                return Task.CompletedTask;
            });

        _connection.On<SplitMigrateProveRequestBase>(nameof(ISplitEndpoints.SplitMigrateProve),
            request =>
            {
                Task.Run(() => SplitMigrateProve(request));
                return Task.CompletedTask;
            });

        _connection.On<SplitMigrateRunRequestBase>(nameof(ISplitEndpoints.SplitMigrateRun),
            request =>
            {
                Task.Run(() => SplitMigrateRun(request));
                return Task.CompletedTask;
            });

        _connection.On<SplitMigrateVerifyRequestBase>(nameof(ISplitEndpoints.SplitMigrateVerify),
            request =>
            {
                Task.Run(() => SplitMigrateVerify(request));
                return Task.CompletedTask;
            });

        _connection.On<SplitPlanEmptyVerifyRequestBase>(nameof(ISplitEndpoints.SplitPlanEmptyVerify),
            request =>
            {
                Task.Run(() => SplitPlanEmptyVerify(request));
                return Task.CompletedTask;
            });

        // Register handler for PolicyValidate
        _connection.On<PolicyValidateRequestBase>(nameof(IApplyEndpoints.ApplyPolicyValidate),
            request =>
            {
                Task.Run(() => ApplyPolicyValidate(request));
                return Task.CompletedTask;
            });

        _connection.On<PolicyValidateRequestBase>(nameof(IDestroyEndpoints.DestroyPolicyValidate),
            request =>
            {
                Task.Run(() => DestroyPolicyValidate(request));
                return Task.CompletedTask;
            });

        // Register handler for Plan
        // An apply, a destroy, a split and a transfer all plan; each answers on its own endpoint.
        _connection.On<PlanRequestBase>(nameof(IApplyEndpoints.ApplyPlan),
            request =>
            {
                Task.Run(() => ApplyPlan(request));
                return Task.CompletedTask;
            });

        _connection.On<GetModuleRequestBase>(nameof(ISplitEndpoints.SplitGetModule),
            request =>
            {
                Task.Run(() => SplitGetModule(request));
                return Task.CompletedTask;
            });

        _connection.On<InitRequestBase>(nameof(ISplitEndpoints.SplitInit),
            request =>
            {
                Task.Run(() => SplitInit(request));
                return Task.CompletedTask;
            });

        _connection.On<ValidateRequestBase>(nameof(ISplitEndpoints.SplitValidate),
            request =>
            {
                Task.Run(() => SplitValidate(request));
                return Task.CompletedTask;
            });

        _connection.On<PlanRequestBase>(nameof(ISplitEndpoints.SplitPlan),
            request =>
            {
                Task.Run(() => SplitPlan(request));
                return Task.CompletedTask;
            });

        // Register handler for PlanDestroy
        _connection.On<PlanDestroyRequestBase>(nameof(IDestroyEndpoints.DestroyPlan),
            request =>
            {
                Task.Run(() => DestroyPlan(request));
                return Task.CompletedTask;
            });

        // Register handler for ApplyFromPlan
        _connection.On<ApplyFromPlanRequestBase>(nameof(IApplyEndpoints.ApplyFromPlan),
            request =>
            {
                Task.Run(() => ApplyFromPlan(request));
                return Task.CompletedTask;
            });

        // Register handler for DestroyFromPlan
        _connection.On<DestroyFromPlanRequestBase>(nameof(IDestroyEndpoints.DestroyFromPlan),
            request =>
            {
                Task.Run(() => DestroyFromPlan(request));
                return Task.CompletedTask;
            });

        // Register handler for Output
        _connection.On<OutputRequestBase>(nameof(IApplyEndpoints.ApplyOutput),
            request =>
            {
                Task.Run(() => ApplyOutput(request));
                return Task.CompletedTask;
            });

        _connection.On<OutputRequestBase>(nameof(IDestroyEndpoints.DestroyOutput),
            request =>
            {
                Task.Run(() => DestroyOutput(request));
                return Task.CompletedTask;
            });

        // Answer liveness pings so the server can tell a live connection from a wedged one.
        _connection.On<Guid>(nameof(IRunnerLifecycleEndpoints.Ping),
            pingId =>
            {
                Task.Run(() => Ping(pingId));
                return Task.CompletedTask;
            });

        // Register handler for SourceRefresh (stateless operation)
        _connection.On<SourceRefreshRequest>(nameof(IRunnerLifecycleEndpoints.SourceRefresh),
            request =>
            {
                Task.Run(() => SourceRefresh(request));
                return Task.CompletedTask;
            });

        _connection.On<CancelKillRequest>(nameof(IApplyEndpoints.ApplyCancelKill),
            request =>
            {
                Task.Run(() => ApplyCancelKill(request));
                return Task.CompletedTask;
            });

        _connection.On<CancelKillRequest>(nameof(IDestroyEndpoints.DestroyCancelKill),
            request =>
            {
                Task.Run(() => DestroyCancelKill(request));
                return Task.CompletedTask;
            });

        _connection.On<CancelKillRequest>(nameof(ISplitEndpoints.SplitCancelKill),
            request =>
            {
                Task.Run(() => SplitCancelKill(request));
                return Task.CompletedTask;
            });

        _connection.On<CancelKillRequest>(nameof(IMoveEndpoints.MoveCancelKill),
            request =>
            {
                Task.Run(() => MoveCancelKill(request));
                return Task.CompletedTask;
            });

        _connection.On<CancelKillRequest>(nameof(IImportEndpoints.ImportCancelKill),
            request =>
            {
                Task.Run(() => ImportCancelKill(request));
                return Task.CompletedTask;
            });

        _connection.On<CancelKillRequest>(nameof(IRemoveEndpoints.RemoveCancelKill),
            request =>
            {
                Task.Run(() => RemoveCancelKill(request));
                return Task.CompletedTask;
            });

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

    // The transfer steps, as the interface the server dispatches against. A transfer runs two
    // Modules under one job, so the preamble's replies name the Module as well as the job.

    public Task TransferGetModule(GetModuleRequestBase request)
    {
        var client = Client;
        var moduleId = request.Metadata.ModuleId;
        return _tasks.Value.GetModule(
            request, _connection!,
            jobId => client.InvokeTransferGetModuleCompleted(jobId, moduleId),
            jobId => client.InvokeTransferGetModuleCancelled(jobId, moduleId),
            (jobId, error, stack) => client.InvokeTransferGetModuleFaulted(jobId, moduleId, error, stack));
    }

    public Task TransferInit(InitRequestBase request)
    {
        var client = Client;
        var moduleId = request.Metadata.ModuleId;
        return _tasks.Value.Init(
            request, _connection!,
            jobId => client.InvokeTransferInitCompleted(jobId, moduleId),
            jobId => client.InvokeTransferInitCancelled(jobId, moduleId),
            (jobId, error, stack) => client.InvokeTransferInitFaulted(jobId, moduleId, error, stack));
    }

    public Task TransferValidate(ValidateRequestBase request)
    {
        var client = Client;
        var moduleId = request.Metadata.ModuleId;
        return _tasks.Value.Validate(
            request, _connection!,
            jobId => client.InvokeTransferValidateCompleted(jobId, moduleId),
            jobId => client.InvokeTransferValidateCancelled(jobId, moduleId),
            (jobId, error, stack) => client.InvokeTransferValidateFaulted(jobId, moduleId, error, stack));
    }

    public Task TransferAnalyseMap(TransferAnalyseMapRequestBase request) =>
        _tasks.Value.TransferAnalyseMap(request, _connection!);

    public Task TransferMigrateMap(TransferMigrateMapRequestBase request) =>
        _tasks.Value.TransferMigrateMap(request, _connection!);

    public Task TransferMigrateProve(TransferMigrateProveRequestBase request) =>
        _tasks.Value.TransferMigrateProve(request, _connection!);

    public Task TransferMigrateRun(TransferMigrateRunRequestBase request) =>
        _tasks.Value.TransferMigrateRun(request, _connection!);

    public Task TransferMigrateVerify(TransferMigrateVerifyRequestBase request) =>
        _tasks.Value.TransferMigrateVerify(request, _connection!);

    // Apply

    public Task ApplyCancelKill(CancelKillRequest request) =>
        _tasks.Value.ApplyCancelKill(request, _connection!);

    public Task ApplyFromPlan(ApplyFromPlanRequestBase request) =>
        _tasks.Value.ApplyFromPlan(request, _connection!);

    public Task ApplyGetDefinitiveRevision(GetDefinitiveRevisionRequest request) =>
        _tasks.Value.GetDefinitiveRevision(
            request, _connection!,
            Client.InvokeApplyGetDefinitiveRevisionCompleted,
            Client.InvokeApplyGetDefinitiveRevisionCancelled,
            Client.InvokeApplyGetDefinitiveRevisionFaulted);

    public Task ApplyGetModule(GetModuleRequestBase request) =>
        _tasks.Value.GetModule(
            request, _connection!,
            Client.InvokeApplyGetModuleCompleted,
            Client.InvokeApplyGetModuleCancelled,
            Client.InvokeApplyGetModuleFaulted);

    public Task ApplyInit(InitRequestBase request) =>
        _tasks.Value.Init(
            request, _connection!,
            Client.InvokeApplyInitCompleted,
            Client.InvokeApplyInitCancelled,
            Client.InvokeApplyInitFaulted);

    public Task ApplyOutput(OutputRequestBase request) =>
        _tasks.Value.Output(
            request, _connection!,
            Client.InvokeApplyOutputCompleted,
            Client.InvokeApplyOutputCancelled,
            Client.InvokeApplyOutputFaulted);

    public Task ApplyPlan(PlanRequestBase request) =>
        _tasks.Value.Plan(
            request, _connection!,
            Client.InvokeApplyPlanCompleted,
            Client.InvokeApplyPlanCancelled,
            Client.InvokeApplyPlanFaulted);

    public Task ApplyPolicyValidate(PolicyValidateRequestBase request) =>
        _tasks.Value.PolicyValidate(
            request, _connection!,
            Client.InvokeApplyPolicyValidateCompleted,
            Client.InvokeApplyPolicyValidateCancelled,
            Client.InvokeApplyPolicyValidateFaulted);

    public Task ApplyValidate(ValidateRequestBase request) =>
        _tasks.Value.Validate(
            request, _connection!,
            Client.InvokeApplyValidateCompleted,
            Client.InvokeApplyValidateCancelled,
            Client.InvokeApplyValidateFaulted);

    public Task ApplyVariables(VariablesRequestBase request) =>
        _tasks.Value.Variables(
            request, _connection!,
            Client.InvokeApplyVariablesCompleted,
            Client.InvokeApplyVariablesCancelled,
            Client.InvokeApplyVariablesFaulted);

    // Destroy

    public Task DestroyCancelKill(CancelKillRequest request) =>
        _tasks.Value.DestroyCancelKill(request, _connection!);

    public Task DestroyFromPlan(DestroyFromPlanRequestBase request) =>
        _tasks.Value.DestroyFromPlan(request, _connection!);

    public Task DestroyGetDefinitiveRevision(GetDefinitiveRevisionRequest request) =>
        _tasks.Value.GetDefinitiveRevision(
            request, _connection!,
            Client.InvokeDestroyGetDefinitiveRevisionCompleted,
            Client.InvokeDestroyGetDefinitiveRevisionCancelled,
            Client.InvokeDestroyGetDefinitiveRevisionFaulted);

    public Task DestroyGetModule(GetModuleRequestBase request) =>
        _tasks.Value.GetModule(
            request, _connection!,
            Client.InvokeDestroyGetModuleCompleted,
            Client.InvokeDestroyGetModuleCancelled,
            Client.InvokeDestroyGetModuleFaulted);

    public Task DestroyInit(InitRequestBase request) =>
        _tasks.Value.Init(
            request, _connection!,
            Client.InvokeDestroyInitCompleted,
            Client.InvokeDestroyInitCancelled,
            Client.InvokeDestroyInitFaulted);

    public Task DestroyOutput(OutputRequestBase request) =>
        _tasks.Value.Output(
            request, _connection!,
            Client.InvokeDestroyOutputCompleted,
            Client.InvokeDestroyOutputCancelled,
            Client.InvokeDestroyOutputFaulted);

    public Task DestroyPlan(PlanDestroyRequestBase request) =>
        _tasks.Value.PlanDestroy(request, _connection!);

    public Task DestroyPolicyValidate(PolicyValidateRequestBase request) =>
        _tasks.Value.PolicyValidate(
            request, _connection!,
            Client.InvokeDestroyPolicyValidateCompleted,
            Client.InvokeDestroyPolicyValidateCancelled,
            Client.InvokeDestroyPolicyValidateFaulted);

    public Task DestroyValidate(ValidateRequestBase request) =>
        _tasks.Value.Validate(
            request, _connection!,
            Client.InvokeDestroyValidateCompleted,
            Client.InvokeDestroyValidateCancelled,
            Client.InvokeDestroyValidateFaulted);

    public Task DestroyVariables(VariablesRequestBase request) =>
        _tasks.Value.Variables(
            request, _connection!,
            Client.InvokeDestroyVariablesCompleted,
            Client.InvokeDestroyVariablesCancelled,
            Client.InvokeDestroyVariablesFaulted);

    // Split

    public Task SplitCancelKill(CancelKillRequest request) =>
        _tasks.Value.SplitCancelKill(request, _connection!);

    public Task SplitGetModule(GetModuleRequestBase request) =>
        _tasks.Value.GetModule(
            request, _connection!,
            Client.InvokeSplitGetModuleCompleted,
            Client.InvokeSplitGetModuleCancelled,
            Client.InvokeSplitGetModuleFaulted);

    public Task SplitInit(InitRequestBase request) =>
        _tasks.Value.Init(
            request, _connection!,
            Client.InvokeSplitInitCompleted,
            Client.InvokeSplitInitCancelled,
            Client.InvokeSplitInitFaulted);

    public Task SplitMigrateMap(SplitMigrateMapRequestBase request) =>
        _tasks.Value.SplitMigrateMap(request, _connection!);

    public Task SplitMigrateProve(SplitMigrateProveRequestBase request) =>
        _tasks.Value.SplitMigrateProve(request, _connection!);

    public Task SplitMigrateRun(SplitMigrateRunRequestBase request) =>
        _tasks.Value.SplitMigrateRun(request, _connection!);

    public Task SplitMigrateVerify(SplitMigrateVerifyRequestBase request) =>
        _tasks.Value.SplitMigrateVerify(request, _connection!);

    public Task SplitPlan(PlanRequestBase request) =>
        _tasks.Value.Plan(
            request, _connection!,
            (jobId,
            data) => Client.InvokeSplitPlanCompleted(jobId,
            data),
            Client.InvokeSplitPlanCancelled,
            (jobId,
            error,
            stack,
            _) => Client.InvokeSplitPlanFaulted(jobId,
            error,
            stack));

    public Task SplitPlanEmptyVerify(SplitPlanEmptyVerifyRequestBase request) =>
        _tasks.Value.SplitPlanEmptyVerify(request, _connection!);

    public Task SplitRefactorDiff(SplitRefactorDiffRequestBase request) =>
        _tasks.Value.SplitRefactorDiff(request, _connection!);

    public Task SplitRefactorValidate(SplitRefactorValidateRequestBase request) =>
        _tasks.Value.SplitRefactorValidate(request, _connection!);

    public Task SplitValidate(ValidateRequestBase request) =>
        _tasks.Value.Validate(
            request, _connection!,
            Client.InvokeSplitValidateCompleted,
            Client.InvokeSplitValidateCancelled,
            Client.InvokeSplitValidateFaulted);

    // LookupAddresses

    public Task LookupAddresses(LookupAddressesRequestBase request) =>
        _tasks.Value.LookupAddresses(request, _connection!);

    public Task LookupAddressesGetModule(GetModuleRequestBase request) =>
        _tasks.Value.GetModule(
            request, _connection!,
            Client.InvokeLookupAddressesGetModuleCompleted,
            Client.InvokeLookupAddressesGetModuleCancelled,
            Client.InvokeLookupAddressesGetModuleFaulted);

    public Task LookupAddressesInit(InitRequestBase request) =>
        _tasks.Value.Init(
            request, _connection!,
            Client.InvokeLookupAddressesInitCompleted,
            Client.InvokeLookupAddressesInitCancelled,
            Client.InvokeLookupAddressesInitFaulted);

    // Move

    public Task Move(StateMoveRequestBase request) =>
        _tasks.Value.Move(request, _connection!);

    public Task MoveCancelKill(CancelKillRequest request) =>
        _tasks.Value.MoveCancelKill(request, _connection!);

    public Task MoveDryRun(StateMoveRequestBase request) =>
        _tasks.Value.MoveDryRun(request, _connection!);

    public Task MoveGetModule(GetModuleRequestBase request) =>
        _tasks.Value.GetModule(
            request, _connection!,
            Client.InvokeMoveGetModuleCompleted,
            Client.InvokeMoveGetModuleCancelled,
            Client.InvokeMoveGetModuleFaulted);

    public Task MoveInit(InitRequestBase request) =>
        _tasks.Value.Init(
            request, _connection!,
            Client.InvokeMoveInitCompleted,
            Client.InvokeMoveInitCancelled,
            Client.InvokeMoveInitFaulted);

    // Import

    public Task Import(StateMoveRequestBase request) =>
        _tasks.Value.Import(request, _connection!);

    public Task ImportCancelKill(CancelKillRequest request) =>
        _tasks.Value.ImportCancelKill(request, _connection!);

    public Task ImportGetModule(GetModuleRequestBase request) =>
        _tasks.Value.GetModule(
            request, _connection!,
            Client.InvokeImportGetModuleCompleted,
            Client.InvokeImportGetModuleCancelled,
            Client.InvokeImportGetModuleFaulted);

    public Task ImportInit(InitRequestBase request) =>
        _tasks.Value.Init(
            request, _connection!,
            Client.InvokeImportInitCompleted,
            Client.InvokeImportInitCancelled,
            Client.InvokeImportInitFaulted);

    public Task ImportPreCheck(StateMoveRequestBase request) =>
        _tasks.Value.ImportPreCheck(request, _connection!);

    // Remove

    public Task Remove(StateMoveRequestBase request) =>
        _tasks.Value.Remove(request, _connection!);

    public Task RemoveCancelKill(CancelKillRequest request) =>
        _tasks.Value.RemoveCancelKill(request, _connection!);

    public Task RemoveDryRun(StateMoveRequestBase request) =>
        _tasks.Value.RemoveDryRun(request, _connection!);

    public Task RemoveGetModule(GetModuleRequestBase request) =>
        _tasks.Value.GetModule(
            request, _connection!,
            Client.InvokeRemoveGetModuleCompleted,
            Client.InvokeRemoveGetModuleCancelled,
            Client.InvokeRemoveGetModuleFaulted);

    public Task RemoveInit(InitRequestBase request) =>
        _tasks.Value.Init(
            request, _connection!,
            Client.InvokeRemoveInitCompleted,
            Client.InvokeRemoveInitCancelled,
            Client.InvokeRemoveInitFaulted);

    // RunnerLifecycle

    public async Task Ping(Guid pingId)
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
    }

    public Task SourceRefresh(SourceRefreshRequest request) =>
        _tasks.Value.SourceRefresh(request, _connection!);

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