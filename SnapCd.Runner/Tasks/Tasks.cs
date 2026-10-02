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
    private readonly PolicyEvaluationSettings _policyEvaluationSettings;

    /// <summary>
    /// Classifies a plan-step failure as a policy outcome when CrossGuard policies were in scope.
    /// The preview's violation output may live either on the process exception or in the captured
    /// stdout (the script wrapper can swallow the exit code); only deny/warn results are reported.
    /// </summary>
    private static SnapCd.Contracts.PolicyOutcome? ClassifyFaultPolicyOutcome(int policyCount, Exception ex, string planOutput)
    {
        if (policyCount == 0)
            return null;

        var text = ex is SnapCd.Runner.Services.ProcessFailedException pfe
            ? pfe.Output + pfe.Error + planOutput
            : planOutput;

        var outcome = Services.PolicyEvaluation.PulumiPolicyOutputParser.Classify(text);
        return outcome == SnapCd.Contracts.PolicyOutcome.Passed ? null : outcome;
    }

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
        PolicyEvaluationService policyEvaluationService,
        IOptions<PolicyEvaluationSettings> policyEvaluationSettings)
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
        _policyEvaluationSettings = policyEvaluationSettings.Value;
    }

    /// <summary>How long to give WithAutomaticReconnect before giving up on a reply.</summary>
    private static readonly TimeSpan ReconnectWait = TimeSpan.FromSeconds(30);

    /// <summary>
    /// Waits for the socket to come back, so a reply is not thrown away in the second it takes to
    /// reconnect. Returns either way; the caller's own attempt limit decides when to stop.
    /// </summary>
    private static async Task WaitForConnection(HubConnection connection, TimeSpan timeout)
    {
        var deadline = DateTime.UtcNow + timeout;

        while (connection.State != HubConnectionState.Connected && DateTime.UtcNow < deadline)
            await Task.Delay(TimeSpan.FromMilliseconds(250));
    }

    /// <summary>
    /// Invoke hub method with automatic retry on transient failures.
    /// </summary>
    private async Task InvokeWithRetryAsync(
        Func<Task> invocation,
        string operationName,
        Guid jobId,
        HubConnection connection,
        int maxRetries = HubInvocationRetry.DefaultMaxRetries,
        TimeSpan? initialDelay = null)
    {
        var logger = _loggerFactory.CreateLogger<Tasks>();

        // Stateless operations (e.g. source refresh) have no job to attribute the call to.
        var target = jobId == Guid.Empty ? "(no job)" : $"job {jobId}";

        try
        {
            await HubInvocationRetry.InvokeAsync(
                invocation,
                maxRetries,
                initialDelay,
                onAttemptFailed: async (ex, attempt, delay) =>
                {
                    if (HubInvocationRetry.IsTokenExpired(ex))
                    {
                        logger.LogWarning(
                            "{Operation} for {Target} failed due to expired token. Will retry on reconnection...",
                            operationName, target);

                        // The connection reconnects on its own via WithAutomaticReconnect.
                        return TimeSpan.FromSeconds(2);
                    }

                    if (connection.State != HubConnectionState.Connected)
                    {
                        logger.LogWarning(
                            "{Operation} for {Target} found the connection {State}; waiting for it",
                            operationName, target, connection.State);

                        await WaitForConnection(connection, ReconnectWait);
                        return TimeSpan.Zero;
                    }

                    logger.LogWarning(
                        "{Operation} for {Target} failed (attempt {Attempt}/{Max}): {Error}. " +
                        "Retrying in {Delay} seconds",
                        operationName, target, attempt, maxRetries, ex.Message, delay.TotalSeconds);

                    return delay;
                },
                onSucceededAfterRetry: attempt => logger.LogInformation(
                    "{Operation} for {Target} succeeded on attempt {Attempt}",
                    operationName, target, attempt),
                // A call made while the socket was down cannot have reached the server, whatever
                // the client threw, so the reply is worth repeating rather than discarding.
                isTransportDown: () => connection.State != HubConnectionState.Connected);
        }
        catch (Exception ex) when (HubInvocationRetry.IsRetryable(ex))
        {
            logger.LogError(
                ex,
                "{Operation} for {Target} failed after {Attempts} attempts",
                operationName, target, maxRetries);
            throw;
        }
        catch (Exception ex)
        {
            logger.LogError(
                ex,
                "{Operation} for {Target} failed with non-retryable exception",
                operationName, target);
            throw;
        }
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