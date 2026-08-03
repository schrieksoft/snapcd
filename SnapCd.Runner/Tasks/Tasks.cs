// SPDX-License-Identifier: LicenseRef-Snap-CD-Source-Available-1.1
// Copyright (c) 2026 Karl Schriek / Schrieksoft.
// No license is granted to use this file, in whole or in part, (a) as training, fine-tuning, retrieval, or
// embedding data for any machine-learning model, or (b) as input to any machine-learning model, agent, or automated
// system for the purpose of producing a derivative work or reimplementation that is not otherwise permitted by the
// Snap CD Source-Available License (including any Competing Product as defined therein). Contact info@snapcd.io
// for terms covering either use.

using Microsoft.AspNetCore.SignalR;
using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.Extensions.Options;
using SnapCd.Contracts;
using SnapCd.Contracts.Clients;
using SnapCd.Runner.Factories;
using SnapCd.Runner.Logging;
using SnapCd.Runner.Services;
using SnapCd.Runner.Services.ModuleSourceRefresher;
using SnapCd.Runner.Settings;
using SnapCd.Runner.Services.PolicyEvaluation;

namespace SnapCd.Runner.Tasks;

public partial class Tasks
{
    private readonly ILoggerFactory _loggerFactory;
    private readonly IJobLogStream _jobLogStream;
    private readonly ProcessRegistry _processRegistry;
    private readonly RunnerSettings _settings;
    private readonly ModuleGetterFactory _moduleGetterFactory;
    private readonly EngineFactory _engineFactory;
    private readonly VariableDiscoveryServiceFactory _discoveryServiceFactory;
    private readonly IModuleSourceRefresherFactory _moduleSourceRefresherFactory;
    private readonly HookPreapprovalService _hookPreapprovalService;
    private readonly BareCloneCache _bareCloneCache;
    private readonly SnapCdInspect _snapCdInspect;
    private readonly PolicyEvaluationService _policyEvaluationService;

    public Tasks(
        ProcessRegistry processRegistry,
        IOptions<RunnerSettings> settings,
        ILoggerFactory loggerFactory,
        IJobLogStream jobLogStream,
        ModuleGetterFactory moduleGetterFactory,
        EngineFactory engineFactory,
        VariableDiscoveryServiceFactory discoveryServiceFactory,
        IModuleSourceRefresherFactory moduleSourceRefresherFactory,
        HookPreapprovalService hookPreapprovalService,
        BareCloneCache bareCloneCache,
        SnapCdInspect snapCdInspect,
        PolicyEvaluationService policyEvaluationService)
    {
        _processRegistry = processRegistry;
        _loggerFactory = loggerFactory;
        _jobLogStream = jobLogStream;
        _settings = settings.Value;
        _moduleGetterFactory = moduleGetterFactory;
        _engineFactory = engineFactory;
        _discoveryServiceFactory = discoveryServiceFactory;
        _moduleSourceRefresherFactory = moduleSourceRefresherFactory;
        _hookPreapprovalService = hookPreapprovalService;
        _bareCloneCache = bareCloneCache;
        _snapCdInspect = snapCdInspect;
        _policyEvaluationService = policyEvaluationService;
    }

    /// <summary>
    /// Invoke hub method with automatic retry on transient failures.
    /// </summary>
    private async Task InvokeWithRetryAsync(
        Func<Task> invocation,
        string operationName,
        Guid jobId,
        HubConnection connection,
        int maxRetries = 3,
        TimeSpan? initialDelay = null)
    {
        var logger = _loggerFactory.CreateLogger<Tasks>();
        var attempt = 0;
        var delay = initialDelay ?? TimeSpan.FromSeconds(1);
        Exception? lastException = null;

        while (attempt < maxRetries)
        {
            try
            {
                await invocation();

                if (attempt > 0)
                {
                    logger.LogInformation(
                        "{Operation} for job {JobId} succeeded on attempt {Attempt}",
                        operationName, jobId, attempt + 1);
                }

                return; // Success
            }
            catch (HubException ex) when (IsRetryableException(ex))
            {
                lastException = ex;
                attempt++;

                if (attempt >= maxRetries)
                {
                    logger.LogError(
                        ex,
                        "{Operation} for job {JobId} failed after {Attempts} attempts",
                        operationName, jobId, attempt);
                    throw;
                }

                // Check if this is a token expiration error
                if (IsTokenExpiredException(ex))
                {
                    logger.LogWarning(
                        "{Operation} for job {JobId} failed due to expired token. Will retry on reconnection...",
                        operationName, jobId);

                    // Note: Connection will automatically reconnect via WithAutomaticReconnect
                    // Wait for reconnection before retrying
                    await Task.Delay(TimeSpan.FromSeconds(2));

                    logger.LogInformation(
                        "Retrying {Operation} for job {JobId} after token expiration",
                        operationName, jobId);

                    // Don't wait additional time - retry immediately
                    continue;
                }

                logger.LogWarning(
                    "{Operation} for job {JobId} failed (attempt {Attempt}/{Max}): {Error}. " +
                    "Retrying in {Delay} seconds",
                    operationName, jobId, attempt, maxRetries, ex.Message, delay.TotalSeconds);

                await Task.Delay(delay);

                // Exponential backoff with max of 10 seconds
                delay = TimeSpan.FromSeconds(Math.Min(delay.TotalSeconds * 2, 10));
            }
            catch (Exception ex)
            {
                logger.LogError(
                    ex,
                    "{Operation} for job {JobId} failed with non-retryable exception",
                    operationName, jobId);
                throw;
            }
        }

        // Should never reach here, but just in case
        throw lastException ?? new Exception($"{operationName} failed after {maxRetries} attempts");
    }

    private static bool IsRetryableException(Exception ex)
    {
        // Retry on connection issues, timeouts, token expiration, and transient failures
        return ex.Message.Contains("TokenExpired", StringComparison.OrdinalIgnoreCase) ||
               ex.Message.Contains("expired", StringComparison.OrdinalIgnoreCase) ||
               ex.Message.Contains("connection", StringComparison.OrdinalIgnoreCase) ||
               ex.Message.Contains("timeout", StringComparison.OrdinalIgnoreCase) ||
               ex.Message.Contains("unavailable", StringComparison.OrdinalIgnoreCase) ||
               ex is TaskCanceledException ||
               ex is TimeoutException;
    }

    private static bool IsTokenExpiredException(Exception ex)
    {
        // Check if the exception is specifically about token expiration
        return ex.Message.Contains("TokenExpired", StringComparison.OrdinalIgnoreCase) ||
               (ex.Message.Contains("token", StringComparison.OrdinalIgnoreCase) &&
                ex.Message.Contains("expired", StringComparison.OrdinalIgnoreCase)) ||
               ex.Message.Contains("authentication token has expired", StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>
    /// Starts a background task that periodically reports task progress to the server.
    /// Sends an immediate first report, then continues reporting at the specified interval.
    /// </summary>
    private Task StartPeriodicTaskReporting(
        Guid jobId,
        string taskName,
        HubConnection connection,
        TimeSpan interval,
        CancellationToken cancellationToken)
    {
        return Task.Run(async () =>
        {
            var logger = _loggerFactory.CreateLogger<Tasks>();
            var runnerHubClient = new RunnerHubClient(connection);

            try
            {
                // Send immediate first report
                logger.LogDebug(
                    "Sending initial task report for job {JobId}, task {TaskName}",
                    jobId, taskName);

                await InvokeWithRetryAsync(
                    () => runnerHubClient.InvokeReportRunningTask(
                        jobId,
                        taskName,
                        _settings.Id,
                        _settings.Instance),
                    nameof(runnerHubClient.InvokeReportRunningTask),
                    jobId,
                    connection);

                // Start periodic reporting
                using var timer = new PeriodicTimer(interval);

                while (await timer.WaitForNextTickAsync(cancellationToken))
                {
                    try
                    {
                        logger.LogDebug(
                            "Sending periodic task report for job {JobId}, task {TaskName}",
                            jobId, taskName);

                        await InvokeWithRetryAsync(
                            () => runnerHubClient.InvokeReportRunningTask(
                                jobId,
                                taskName,
                                _settings.Id,
                                _settings.Instance),
                            nameof(runnerHubClient.InvokeReportRunningTask),
                            jobId,
                            connection);
                    }
                    catch (Exception ex)
                    {
                        // Log but don't throw - reporting failures should not stop task execution
                        logger.LogWarning(
                            ex,
                            "Failed to report running task for job {JobId}, task {TaskName}. Will retry on next interval.",
                            jobId, taskName);
                    }
                }
            }
            catch (OperationCanceledException)
            {
                // Expected when task completes
                logger.LogDebug(
                    "Task reporting cancelled for job {JobId}, task {TaskName}",
                    jobId, taskName);
            }
            catch (Exception ex)
            {
                // Log but don't throw - reporting failures should not stop task execution
                logger.LogWarning(
                    ex,
                    "Task reporting failed for job {JobId}, task {TaskName}",
                    jobId, taskName);
            }
        }, cancellationToken);
    }
}