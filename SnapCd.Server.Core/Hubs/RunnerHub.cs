// SPDX-License-Identifier: LicenseRef-Snap-CD-Source-Available-1.1
// Copyright (c) 2026 Karl Schriek / Schrieksoft.
// No license is granted to use this file, in whole or in part, (a) as training, fine-tuning, retrieval, or
// embedding data for any machine-learning model, or (b) as input to any machine-learning model, agent, or automated
// system for the purpose of producing a derivative work or reimplementation that is not otherwise permitted by the
// Snap CD Source-Available License (including any Competing Product as defined therein). Contact info@snapcd.io
// for terms covering either use.

using MassTransit;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using SnapCd.Contracts;
using SnapCd.Contracts.Dto.Misc;
using SnapCd.Contracts.Dto.OutputSets;
using SnapCd.Contracts.Dto.VariableSets;
using SnapCd.Contracts.RunnerRequests;
using SnapCd.Contracts.RunnerRequests.HelperClasses;
using SnapCd.Server.Core.Database;
using SnapCd.Server.Core.Entities.Definition;
using SnapCd.Server.Core.Enums;
using SnapCd.Server.Core.Events.Runners;
using SnapCd.Server.Core.Events.System;
using SnapCd.Server.Core.Hubs.Handlers;
using SnapCd.Server.Core.Misc.Constants;
using SnapCd.Server.Core.Repositories.Organizations.Nonsecured;
using SnapCd.Server.Core.Services;
using SnapCd.Server.Core.Services.Crud.Jobs;
using SnapCd.Server.Core.Services.RunnerConnectionValidator;
using SnapCd.Server.Core.Settings;

namespace SnapCd.Server.Core.Hubs;

/// <summary>
/// SignalR hub for bidirectional communication with runners.
/// Handles registration, logging, and future task dispatch.
/// </summary>
[Authorize(AuthenticationSchemes = "Bearer")]
public class RunnerHub : Hub
{
    private readonly IDbContextFactory<SnapCdDbContext> _dbContextFactory;
    private readonly LogService _logService;
    private readonly JobServiceFactory _jobServiceFactory;
    private readonly IBus _bus;
    private readonly ILogger<RunnerHub> _logger;
    private readonly RunnerJobAuthorizationService _authorizationService;
    private readonly GetDefinitiveRevisionHandler _getModuleDefinitiveRevisionHandler;
    private readonly GetModuleHandler _getModuleHandler;
    private readonly InitHandler _initHandler;
    private readonly ValidateHandler _validateHandler;
    private readonly PolicyValidateHandler _policyValidateHandler;
    private readonly VariableHandler _variableHandler;
    private readonly PlanHandler _planHandler;
    private readonly PlanDestroyHandler _planDestroyHandler;
    private readonly ApplyFromPlanHandler _applyFromPlanHandler;
    private readonly DestroyFromPlanHandler _destroyFromPlanHandler;
    private readonly OutputHandler _outputHandler;
    private readonly SourceRefreshHandler _sourceRefreshHandler;
    private readonly RunnerConnectionValidator _connectionValidator;
    private readonly ServerSettings _serverSettings;
    private readonly RunnerConnectionRepositoryFactory _connectionRepositoryFactory;
    private readonly ReportRunningTaskHandler _reportRunningTaskHandler;
    private readonly CancelKillHandler _cancelKillHandler;
    private readonly CancelGracefulHandler _cancelGracefulHandler;

    public RunnerHub(
        IDbContextFactory<SnapCdDbContext> dbContextFactory,
        LogService logService,
        JobServiceFactory jobServiceFactory,
        IBus bus,
        ILogger<RunnerHub> logger,
        RunnerJobAuthorizationService authorizationService,
        GetDefinitiveRevisionHandler getModuleDefinitiveRevisionHandler,
        GetModuleHandler getModuleHandler,
        InitHandler initHandler,
        ValidateHandler validateHandler,
        PolicyValidateHandler policyValidateHandler,
        VariableHandler variableHandler,
        PlanHandler planHandler,
        PlanDestroyHandler planDestroyHandler,
        ApplyFromPlanHandler applyFromPlanHandler,
        DestroyFromPlanHandler destroyFromPlanHandler,
        OutputHandler outputHandler,
        SourceRefreshHandler sourceRefreshHandler,
        RunnerConnectionValidator connectionValidator,
        IOptions<ServerSettings> serverSettings,
        RunnerConnectionRepositoryFactory connectionRepositoryFactory,
        ReportRunningTaskHandler reportRunningTaskHandler,
        CancelKillHandler cancelKillHandler,
        CancelGracefulHandler cancelGracefulHandler)
    {
        _dbContextFactory = dbContextFactory;
        _logService = logService;
        _jobServiceFactory = jobServiceFactory;
        _bus = bus;
        _logger = logger;
        _authorizationService = authorizationService;
        _getModuleDefinitiveRevisionHandler = getModuleDefinitiveRevisionHandler;
        _getModuleHandler = getModuleHandler;
        _initHandler = initHandler;
        _validateHandler = validateHandler;
        _policyValidateHandler = policyValidateHandler;
        _variableHandler = variableHandler;
        _planHandler = planHandler;
        _planDestroyHandler = planDestroyHandler;
        _applyFromPlanHandler = applyFromPlanHandler;
        _destroyFromPlanHandler = destroyFromPlanHandler;
        _outputHandler = outputHandler;
        _sourceRefreshHandler = sourceRefreshHandler;
        _connectionValidator = connectionValidator;
        _serverSettings = serverSettings.Value;
        _connectionRepositoryFactory = connectionRepositoryFactory;
        _reportRunningTaskHandler = reportRunningTaskHandler;
        _cancelKillHandler = cancelKillHandler;
        _cancelGracefulHandler = cancelGracefulHandler;
    }


    public override async Task OnConnectedAsync()
    {
        try
        {
            _logger.LogDebug("New runner connection attempt from {ConnectionId}", Context.ConnectionId);

            // Extract parameters from query string
            var httpContext = Context.GetHttpContext();
            var organizationIdParam = httpContext?.Request.Query["organization_id"].ToString();
            var runnerIdParam = httpContext?.Request.Query["runner_id"].ToString();
            var runnerInstanceParam = httpContext?.Request.Query["runner_instance"].ToString();

            if (string.IsNullOrEmpty(organizationIdParam) ||
                string.IsNullOrEmpty(runnerIdParam))
            {
                _logger.LogWarning("Missing required query parameters. Provided: organization_id={OrganizationId}, runner_id={RunnerId}",
                    organizationIdParam ?? "(null)", runnerIdParam ?? "(null)");
                Context.Abort();
                return;
            }

            if (!Guid.TryParse(organizationIdParam, out var organizationId))
            {
                _logger.LogWarning("Invalid organization_id format: {OrganizationId}", organizationIdParam);
                Context.Abort();
                return;
            }

            if (!Guid.TryParse(runnerIdParam, out var runnerId))
            {
                _logger.LogWarning("Invalid runner_id format: {RunnerId}", runnerIdParam);
                Context.Abort();
                return;
            }

            await using var dbContext = await _dbContextFactory.CreateDbContextAsync();

            // Get the Runner by ID
            var runner = await dbContext.Runners
                .FirstOrDefaultAsync(rp => rp.Id == runnerId && rp.OrganizationId == organizationId);

            if (runner == null)
            {
                _logger.LogWarning("Runner {RunnerId} not found for organization {OrganizationId}",
                    runnerId, organizationId);
                Context.Abort();
                return;
            }

            // Validate instance name based on AllowMultipleInstances flag
            if (runner.AllowMultipleInstances)
            {
                if (string.IsNullOrEmpty(runnerInstanceParam))
                {
                    _logger.LogWarning("Runner {RunnerId} requires instance name (AllowMultipleInstances=true) but none provided",
                        runnerId);
                    throw new HubException("Runner requires an instance name because AllowMultipleInstances is enabled");
                }
            }
            else
            {
                // If AllowMultipleInstances is false, use runner name as instance name when not provided
                if (string.IsNullOrEmpty(runnerInstanceParam))
                {
                    runnerInstanceParam = runner.Name;
                    _logger.LogDebug("Runner {RunnerId} does not allow multiple instances, using runner name as instance: {InstanceName}",
                        runnerId, runnerInstanceParam);
                }
            }

            // Get principal information from JWT claims
            var principalIdClaim = Context.User?.FindFirst(ClaimTypeConstants.SubjectClaimType)?.Value;
            var principalDiscriminatorClaim = Context.User?.FindFirst(ClaimTypeConstants.PrincipalDiscriminatorClaimType)?.Value;

            if (string.IsNullOrEmpty(principalIdClaim) || string.IsNullOrEmpty(principalDiscriminatorClaim))
            {
                _logger.LogWarning("Missing principal claims in JWT token");
                Context.Abort();
                return;
            }

            if (!Guid.TryParse(principalIdClaim, out var principalId))
            {
                _logger.LogWarning("Invalid principal ID in JWT token");
                Context.Abort();
                return;
            }

            // Validate principal is a ServicePrincipal (Users can no longer connect as runners)
            if (principalDiscriminatorClaim != "ServicePrincipal")
            {
                _logger.LogWarning("Only ServicePrincipals can connect as runners. Attempted connection with discriminator: {Discriminator}",
                    principalDiscriminatorClaim);
                Context.Abort();
                return;
            }

            // Verify service principal belongs to the organization
            var servicePrincipal = await dbContext.ServicePrincipals
                .FirstOrDefaultAsync(sp => sp.Id == principalId && sp.OrganizationId == organizationId);

            if (servicePrincipal == null)
            {
                _logger.LogWarning("ServicePrincipal {PrincipalId} not found or not in organization {OrganizationId}",
                    principalId, organizationId);
                Context.Abort();
                return;
            }

            // Validate that the Runner's ServicePrincipalId matches the connecting principal
            if (runner.ServicePrincipalId != principalId)
            {
                _logger.LogWarning("ServicePrincipal {PrincipalId} does not match Runner's assigned ServicePrincipal {RunnerServicePrincipalId}",
                    principalId, runner.ServicePrincipalId);
                Context.Abort();
                return;
            }

            // Create connection repository for validation checks
            using var connectionRepository = _connectionRepositoryFactory.Create();

            // If AllowMultipleInstances is false, check if any instance is already connected
            if (!runner.AllowMultipleInstances)
            {
                var existingConnections = await connectionRepository.GetActiveConnectionsByRunnerId(runnerId, organizationId);

                if (existingConnections.Any( x => x.InstanceName != runnerInstanceParam))
                {
                    _logger.LogWarning(
                        "Runner {RunnerId} does not allow multiple instances. Instance '{ExistingInstance}' is already connected.",
                        runnerId, runnerInstanceParam);
                    throw new HubException($"Runner does not allow multiple instances. An instance is already connected as '{runnerInstanceParam}'");
                }
            }

            // Validate connection (duplicate detection and rate limiting)
            var validationResult = await _connectionValidator.ValidateConnection(organizationId, runnerId, runnerInstanceParam);
            if (!validationResult.IsAllowed)
            {
                _logger.LogWarning(
                    "Connection validation failed for runner {InstanceName} (ID: {RunnerId}): {Reason}",
                    runnerInstanceParam, runnerId, validationResult.RejectionReason);
                throw new HubException(validationResult.RejectionReason ?? "Connection validation failed");
            }

            // Create connection record in database
            var connectionRecord = new RunnerConnection
            {
                Id = Guid.NewGuid(),
                OrganizationId = organizationId,
                RunnerId = runnerId,
                InstanceName = runnerInstanceParam,
                SignalRConnectionId = Context.ConnectionId,
                ServerInstanceId = _serverSettings.InstanceId
            };
            await connectionRepository.Create(connectionRecord);

            // Publish event to notify system of runner availability change
            await _bus.Publish(new RunnerAvailabilityChangedEvent
            {
                RunnerId = runnerId,
                RunnerInstanceName = runnerInstanceParam
            });

            // Publish event to wake up any sagas waiting for this runner to reconnect
            await _bus.Publish(new RunnerReconnectedEvent
            {
                OrganizationId = organizationId,
                RunnerId = runnerId,
                InstanceName = runnerInstanceParam,
                ServerInstanceId = _serverSettings.InstanceId
            });

            // Trigger queued jobs waiting for this runner
            using var jobService = _jobServiceFactory.Create();
            await jobService.TriggerQueuedJobs(runnerId, runnerInstanceParam);

            _logger.LogInformation(
                "Runner '{RunnerId}' connected with instance name {runnerInstanceParam} in organization {OrganizationId}",
                runnerId, runnerInstanceParam, organizationId);

            await base.OnConnectedAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in runner connection for {ConnectionId}", Context.ConnectionId);
            Context.Abort();
        }
    }

    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        try
        {
            // Query database to get connection info by ConnectionId
            using var connectionRepository = _connectionRepositoryFactory.Create();
            var connection = await connectionRepository.GetBySignalRConnectionIdAsync(Context.ConnectionId, _authorizationService.GetValidatedOrganizationId(Context));

            if (connection != null)
            {
                // Delete connection record from database
                await connectionRepository.DeleteConnection(
                    connection.OrganizationId,
                    connection.RunnerId,
                    connection.InstanceName
                );

                // Publish event to notify system of runner availability change
                await _bus.Publish(new RunnerAvailabilityChangedEvent
                {
                    RunnerId = connection.RunnerId,
                    RunnerInstanceName = connection.InstanceName
                });

                _logger.LogInformation(
                    "Runner '{RunnerName}' disconnected from runner {RunnerId} in organization {OrganizationId}",
                    connection.InstanceName, connection.RunnerId, connection.OrganizationId);
            }

            await base.OnDisconnectedAsync(exception);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during disconnection for {ConnectionId}", Context.ConnectionId);
        }
    }

    /// <summary>
    /// Acknowledge a liveness ping. Invoking this proves the runner can still service hub calls,
    /// which an open connection alone does not.
    /// </summary>
    public Task Pong(Guid pingId)
    {
        Services.RunnerLivenessProbe.Acknowledge(pingId);
        return Task.CompletedTask;
    }

    /// <summary>
    /// Receive a batch of log entries from the runner
    /// </summary>
    public async Task AddLogs(List<LogEntryDto> logEntries)
    {
        if (logEntries == null || logEntries.Count == 0)
            return;

        try
        {
            // Query database to get connection info
            using var connectionRepository = _connectionRepositoryFactory.Create();
            var connection = await connectionRepository.GetBySignalRConnectionIdAsync(Context.ConnectionId, _authorizationService.GetValidatedOrganizationId(Context));

            if (connection == null)
            {
                _logger.LogWarning("No connection info found for connection {ConnectionId}", Context.ConnectionId);
                return;
            }

            // TODO: Add permission checking here (like LogsController.CheckHasPermission)
            // For now, skip permission checking

            // Add log entries to database
            await _logService.AddLogEntries(logEntries);

            // Publish LogReceivedEvent for the first correlation ID
            var firstEntry = logEntries.First();
            await _bus.Publish(new LogReceivedEvent
            {
                JobId = firstEntry.JobId,
                ModuleId = firstEntry.ModuleId
            }, context => { context.TimeToLive = TimeSpan.FromSeconds(60); });

            _logger.LogDebug("Processed log batch with {Count} entries from runner '{RunnerName}'",
                logEntries.Count, connection.InstanceName);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing log batch from connection {ConnectionId}", Context.ConnectionId);
            throw new HubException("Error processing logs");
        }
    }


    /// <summary>
    /// Called by runner when GetDefinitiveRevision task completes successfully.
    /// </summary>
    public async Task GetDefinitiveRevisionCompleted(Guid jobId, string definitiveRevision)
    {
        await _authorizationService.ValidateRunnerCanAccessJob(Context, jobId, TaskEndpoint.GetDefinitiveRevisionCompleted);

        await _getModuleDefinitiveRevisionHandler.Complete(jobId, definitiveRevision);
    }

    /// <summary>
    /// Called by runner when GetDefinitiveRevision task is cancelled.
    /// </summary>
    public async Task GetDefinitiveRevisionCancelled(Guid jobId)
    {
        await _authorizationService.ValidateRunnerCanAccessJob(Context, jobId, TaskEndpoint.GetDefinitiveRevisionCancelled);

        await _getModuleDefinitiveRevisionHandler.Cancel(jobId);
    }

    /// <summary>
    /// Called by runner when GetDefinitiveRevision task faults with an error.
    /// </summary>
    public async Task GetDefinitiveRevisionFaulted(Guid jobId, string? errorMessage, string? stackTrace)
    {
        await _authorizationService.ValidateRunnerCanAccessJob(Context, jobId, TaskEndpoint.GetDefinitiveRevisionFaulted);

        await _getModuleDefinitiveRevisionHandler.Fault(jobId, errorMessage, stackTrace);
    }

    /// <summary>
    /// Called by runner when GetModule task completes successfully.
    /// </summary>
    public async Task GetModuleCompleted(Guid jobId)
    {
        await _authorizationService.ValidateRunnerCanAccessJob(Context, jobId, TaskEndpoint.GetModuleCompleted);

        await _getModuleHandler.Complete(jobId);
    }

    /// <summary>
    /// Called by runner when GetModule task is cancelled.
    /// </summary>
    public async Task GetModuleCancelled(Guid jobId)
    {
        await _authorizationService.ValidateRunnerCanAccessJob(Context, jobId, TaskEndpoint.GetModuleCancelled);

        await _getModuleHandler.Cancel(jobId);
    }

    /// <summary>
    /// Called by runner when GetModule task faults with an error.
    /// </summary>
    public async Task GetModuleFaulted(Guid jobId, string? errorMessage, string? stackTrace)
    {
        await _authorizationService.ValidateRunnerCanAccessJob(Context, jobId, TaskEndpoint.GetModuleFaulted);

        await _getModuleHandler.Fault(jobId, errorMessage, stackTrace);
    }

    /// <summary>
    /// Called by runner when Init task completes successfully.
    /// </summary>
    public async Task InitCompleted(Guid jobId)
    {
        await _authorizationService.ValidateRunnerCanAccessJob(Context, jobId, TaskEndpoint.InitCompleted);

        await _initHandler.Complete(jobId);
    }

    /// <summary>
    /// Called by runner when Init task is cancelled.
    /// </summary>
    public async Task InitCancelled(Guid jobId)
    {
        await _authorizationService.ValidateRunnerCanAccessJob(Context, jobId, TaskEndpoint.InitCancelled);

        await _initHandler.Cancel(jobId);
    }

    /// <summary>
    /// Called by runner when Init task faults with an error.
    /// </summary>
    public async Task InitFaulted(Guid jobId, string? errorMessage, string? stackTrace)
    {
        await _authorizationService.ValidateRunnerCanAccessJob(Context, jobId, TaskEndpoint.InitFaulted);

        await _initHandler.Fault(jobId, errorMessage, stackTrace);
    }

    /// <summary>
    /// Called by runner when Validate task completes successfully.
    /// </summary>
    public async Task ValidateCompleted(Guid jobId)
    {
        await _authorizationService.ValidateRunnerCanAccessJob(Context, jobId, TaskEndpoint.ValidateCompleted);

        await _validateHandler.Complete(jobId);
    }

    /// <summary>
    /// Called by runner when Validate task is cancelled.
    /// </summary>
    public async Task ValidateCancelled(Guid jobId)
    {
        await _authorizationService.ValidateRunnerCanAccessJob(Context, jobId, TaskEndpoint.ValidateCancelled);

        await _validateHandler.Cancel(jobId);
    }


    public async Task ValidateFaulted(Guid jobId, string? errorMessage, string? stackTrace)
    {
        await _authorizationService.ValidateRunnerCanAccessJob(Context, jobId, TaskEndpoint.ValidateFaulted);

        await _validateHandler.Fault(jobId, errorMessage, stackTrace);
    }

    /// <summary>
    /// Called by runner when PolicyValidate task completes (any outcome, including a hard deny).
    /// </summary>
    public async Task PolicyValidateCompleted(Guid jobId, PolicyOutcome outcome)
    {
        await _authorizationService.ValidateRunnerCanAccessJob(Context, jobId, TaskEndpoint.PolicyValidateCompleted);

        await _policyValidateHandler.Complete(jobId, outcome);
    }

    /// <summary>
    /// Called by runner when PolicyValidate task is cancelled.
    /// </summary>
    public async Task PolicyValidateCancelled(Guid jobId)
    {
        await _authorizationService.ValidateRunnerCanAccessJob(Context, jobId, TaskEndpoint.PolicyValidateCancelled);

        await _policyValidateHandler.Cancel(jobId);
    }

    public async Task PolicyValidateFaulted(Guid jobId, string? errorMessage, string? stackTrace)
    {
        await _authorizationService.ValidateRunnerCanAccessJob(Context, jobId, TaskEndpoint.PolicyValidateFaulted);

        await _policyValidateHandler.Fault(jobId, errorMessage, stackTrace);
    }

    public async Task VariablesCompleted(Guid jobId, VariableSetCreateDto? variableSet)
    {
        await _authorizationService.ValidateRunnerCanAccessJob(Context, jobId, TaskEndpoint.VariablesCompleted);

        await _variableHandler.Complete(jobId, variableSet);
    }


    public async Task VariablesCancelled(Guid jobId)
    {
        await _authorizationService.ValidateRunnerCanAccessJob(
            Context, jobId, TaskEndpoint.VariablesCancelled);

        await _variableHandler.Cancel(jobId);
    }


    public async Task VariablesFaulted(Guid jobId, string? errorMessage, string? stackTrace)
    {
        await _authorizationService.ValidateRunnerCanAccessJob(
            Context, jobId, TaskEndpoint.VariablesFaulted);

        await _variableHandler.Fault(jobId, errorMessage, stackTrace);
    }


    public async Task PlanCompleted(Guid jobId, PlanCompletedData data)
    {
        await _authorizationService.ValidateRunnerCanAccessJob(Context, jobId, TaskEndpoint.PlanCompleted);

        await _planHandler.Complete(jobId, data);
    }


    public async Task PlanCancelled(Guid jobId)
    {
        await _authorizationService.ValidateRunnerCanAccessJob(Context, jobId, TaskEndpoint.PlanCancelled);

        await _planHandler.Cancel(jobId);
    }


    public async Task PlanFaulted(Guid jobId, string? errorMessage, string? stackTrace, PolicyOutcome? policyOutcome = null)
    {
        await _authorizationService.ValidateRunnerCanAccessJob(Context, jobId, TaskEndpoint.PlanFaulted);

        await _planHandler.Fault(jobId, errorMessage, stackTrace, policyOutcome);
    }

    public async Task PlanDestroyCompleted(Guid jobId, PlanCompletedData data)
    {
        await _authorizationService.ValidateRunnerCanAccessJob(Context, jobId, TaskEndpoint.PlanDestroyCompleted);

        await _planDestroyHandler.Complete(jobId, data);
    }

    public async Task PlanDestroyCancelled(Guid jobId)
    {
        await _authorizationService.ValidateRunnerCanAccessJob(Context, jobId, TaskEndpoint.PlanDestroyCancelled);

        await _planDestroyHandler.Cancel(jobId);
    }

    public async Task PlanDestroyFaulted(Guid jobId, string? errorMessage, string? stackTrace, PolicyOutcome? policyOutcome = null)
    {
        await _authorizationService.ValidateRunnerCanAccessJob(Context, jobId, TaskEndpoint.PlanDestroyFaulted);

        await _planDestroyHandler.Fault(jobId, errorMessage, stackTrace, policyOutcome);
    }

    public async Task ApplyFromPlanCompleted(Guid jobId, int? actualResourceCount)
    {
        await _authorizationService.ValidateRunnerCanAccessJob(Context, jobId, TaskEndpoint.ApplyFromPlanCompleted);

        await _applyFromPlanHandler.Complete(jobId, actualResourceCount);
    }

    public async Task ApplyFromPlanCancelled(Guid jobId)
    {
        await _authorizationService.ValidateRunnerCanAccessJob(Context, jobId, TaskEndpoint.ApplyFromPlanCancelled);

        await _applyFromPlanHandler.Cancel(jobId);
    }

    public async Task ApplyFromPlanFaulted(Guid jobId, string? errorMessage, string? stackTrace, int? actualResourceCount)
    {
        await _authorizationService.ValidateRunnerCanAccessJob(Context, jobId, TaskEndpoint.ApplyFromPlanFaulted);

        await _applyFromPlanHandler.Fault(jobId, errorMessage, stackTrace, actualResourceCount);
    }

    public async Task DestroyFromPlanCompleted(Guid jobId, int? actualResourceCount)
    {
        await _authorizationService.ValidateRunnerCanAccessJob(Context, jobId, TaskEndpoint.DestroyFromPlanCompleted);

        await _destroyFromPlanHandler.Complete(jobId, actualResourceCount);
    }

    public async Task DestroyFromPlanCancelled(Guid jobId)
    {
        await _authorizationService.ValidateRunnerCanAccessJob(Context, jobId, TaskEndpoint.DestroyFromPlanCancelled);

        await _destroyFromPlanHandler.Cancel(jobId);
    }

    public async Task DestroyFromPlanFaulted(Guid jobId, string? errorMessage, string? stackTrace, int? actualResourceCount)
    {
        await _authorizationService.ValidateRunnerCanAccessJob(Context, jobId, TaskEndpoint.DestroyFromPlanFaulted);

        await _destroyFromPlanHandler.Fault(jobId, errorMessage, stackTrace, actualResourceCount);
    }

    public async Task OutputCompleted(Guid jobId, OutputSetCreateDto? outputSet)
    {
        await _authorizationService.ValidateRunnerCanAccessJob(Context, jobId, TaskEndpoint.OutputCompleted);

        await _outputHandler.Complete(jobId, outputSet);
    }

    public async Task OutputCancelled(Guid jobId)
    {
        await _authorizationService.ValidateRunnerCanAccessJob(Context, jobId, TaskEndpoint.OutputCancelled);

        await _outputHandler.Cancel(jobId);
    }

    public async Task OutputFaulted(Guid jobId, string? errorMessage, string? stackTrace)
    {
        await _authorizationService.ValidateRunnerCanAccessJob(Context, jobId, TaskEndpoint.OutputFaulted);

        await _outputHandler.Fault(jobId, errorMessage, stackTrace);
    }

    public async Task SourceRefreshCompleted(
        string sourceUrl,
        string sourceRevision,
        SourceType sourceType,
        SourceRevisionType sourceRevisionType,
        string definitiveRevision)
    {
        await _sourceRefreshHandler.Complete(sourceUrl, sourceRevision, sourceType, sourceRevisionType, definitiveRevision);
    }

    public async Task SourceRefreshCompletedV2(
        string sourceUrl,
        string sourceRevision,
        SourceType sourceType,
        SourceRevisionType sourceRevisionType,
        SourceRefreshResult result)
    {
        await _sourceRefreshHandler.CompleteV2(sourceUrl, sourceRevision, sourceType, sourceRevisionType, result);
    }

    public async Task SourceRefreshFaulted(
        string sourceUrl,
        string sourceRevision,
        SourceType sourceType,
        SourceRevisionType sourceRevisionType,
        string? errorMessage,
        string? stackTrace)
    {
        await _sourceRefreshHandler.Fault(sourceUrl, sourceRevision, sourceType, sourceRevisionType, errorMessage, stackTrace);
    }

    public async Task ReportRunningTask(Guid jobId, string taskName, Guid runnerId, string? runnerInstanceName)
    {
        var organizationId = await _authorizationService.ValidateIsForCurrentConnection(Context, jobId);

        await _reportRunningTaskHandler.Report(organizationId, jobId, taskName, runnerId, runnerInstanceName);
    }

    /// <summary>
    /// Called by runner when kill cancellation completes.
    /// Publishes KillCancelCompleted event to MassTransit and completes the TCS if waiting.
    /// </summary>
    public async Task CancelKillCompleted(Guid jobId)
    {
        await _authorizationService.ValidateRunnerCanAccessJob(Context, jobId, TaskEndpoint.CancelKillCompleted);

        // Publish MassTransit event
        await _cancelKillHandler.Complete(jobId);
    }

    /// <summary>
    /// Called by runner when graceful cancellation completes.
    /// Publishes GracefulCancelCompleted event to MassTransit and completes the TCS if waiting.
    /// </summary>
    public async Task CancelGracefulCompleted(Guid jobId)
    {
        await _authorizationService.ValidateRunnerCanAccessJob(Context, jobId, TaskEndpoint.CancelGracefulCompleted);

        // Publish MassTransit event
        await _cancelGracefulHandler.Complete(jobId);
    }
}