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
using SnapCd.Contracts.RunnerRequests.StateMigrations;
using SnapCd.Server.Core.Events.Steps.StateMigrations;
using SnapCd.Contracts.Dto.VariableSets;
using SnapCd.Contracts.RunnerRequests;
using SnapCd.Contracts.RunnerRequests.HelperClasses;
using SnapCd.Server.Core.Database;
using SnapCd.Server.Core.Entities.Definition;
using SnapCd.Server.Core.Enums;
using SnapCd.Server.Core.Events.Handlers;
using SnapCd.Server.Core.Events.Runners;
using SnapCd.Server.Core.Events.System;
using SnapCd.Server.Core.Hubs.Handlers;
using SnapCd.Server.Core.Entities.Sagas;
using SnapCd.Server.Core.Hubs.Handlers.StateMigrations;
using SnapCd.Server.Core.Hubs.Handlers.SplitMigrate;
using SnapCd.Server.Core.Hubs.Handlers.Transfers;
using SnapCd.Server.Core.Services.Crud.Transfers;
using SnapCd.Server.Core.Events.Steps.Transfer;
using SnapCd.Server.Core.Misc.Constants;
using SnapCd.Server.Core.Repositories.Organizations.Nonsecured;
using SnapCd.Server.Core.Services;
using SnapCd.Server.Core.Services.Crud.Jobs;
using SnapCd.Server.Core.Services.RunnerConnectionValidator;
using SnapCd.Server.Core.Settings;
using SnapCd.Server.Core.Views;

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
    private readonly ApplyGetDefinitiveRevisionHandler _applyGetDefinitiveRevisionHandler;
    private readonly DestroyGetDefinitiveRevisionHandler _destroyGetDefinitiveRevisionHandler;
    private readonly ApplyPolicyValidateHandler _applyPolicyValidateHandler;
    private readonly DestroyPolicyValidateHandler _destroyPolicyValidateHandler;
    private readonly ApplyOutputHandler _applyOutputHandler;
    private readonly DestroyOutputHandler _destroyOutputHandler;
    private readonly PlanHandler _planHandler;
    private readonly SplitGetModuleHandler _splitGetModuleHandler;
    private readonly TransferStepHandler _transferStepHandler;
    private readonly ApplyGetModuleHandler _applyGetModuleHandler;
    private readonly ApplyInitHandler _applyInitHandler;
    private readonly ApplyValidateHandler _applyValidateHandler;
    private readonly ApplyVariablesHandler _applyVariablesHandler;
    private readonly DestroyGetModuleHandler _destroyGetModuleHandler;
    private readonly DestroyInitHandler _destroyInitHandler;
    private readonly DestroyValidateHandler _destroyValidateHandler;
    private readonly DestroyVariablesHandler _destroyVariablesHandler;
    private readonly LookupAddressesGetModuleHandler _stateListFilteredGetModuleHandler;
    private readonly LookupAddressesInitHandler _stateListFilteredInitHandler;
    private readonly MoveGetModuleHandler _moveGetModuleHandler;
    private readonly MoveInitHandler _moveInitHandler;
    private readonly ImportGetModuleHandler _importGetModuleHandler;
    private readonly ImportInitHandler _importInitHandler;
    private readonly RemoveGetModuleHandler _removeGetModuleHandler;
    private readonly RemoveInitHandler _removeInitHandler;
    private readonly SplitInitHandler _splitInitHandler;
    private readonly SplitValidateHandler _splitValidateHandler;
    private readonly SplitPlanHandler _splitPlanHandler;
    private readonly SplitPlanEmptyVerifyHandler _planEmptyVerifyHandler;
    private readonly SplitRefactorValidateHandler _refactorValidateHandler;
    private readonly SplitRefactorDiffHandler _refactorDiffHandler;
    private readonly SplitMigrateMapHandler _migrateMapHandler;
    private readonly SplitMigrateProveHandler _migrateProveHandler;
    private readonly SplitMigrateRunHandler _migrateRunHandler;
    private readonly SplitMigrateVerifyHandler _migrateVerifyHandler;
    private readonly PlanDestroyHandler _planDestroyHandler;
    private readonly ApplyFromPlanHandler _applyFromPlanHandler;
    private readonly DestroyFromPlanHandler _destroyFromPlanHandler;
    private readonly SourceRefreshHandler _sourceRefreshHandler;
    private readonly RunnerConnectionValidator _connectionValidator;
    private readonly ServerSettings _serverSettings;
    private readonly RunnerConnectionRepositoryFactory _connectionRepositoryFactory;
    private readonly ReportRunningTaskHandler _reportRunningTaskHandler;
    private readonly CancelKillHandler _cancelKillHandler;

    public RunnerHub(
        IDbContextFactory<SnapCdDbContext> dbContextFactory,
        LogService logService,
        JobServiceFactory jobServiceFactory,
        IBus bus,
        ILogger<RunnerHub> logger,
        RunnerJobAuthorizationService authorizationService,
        ApplyGetDefinitiveRevisionHandler applyGetDefinitiveRevisionHandler,
        DestroyGetDefinitiveRevisionHandler destroyGetDefinitiveRevisionHandler,
        ApplyPolicyValidateHandler applyPolicyValidateHandler,
        DestroyPolicyValidateHandler destroyPolicyValidateHandler,
        ApplyOutputHandler applyOutputHandler,
        DestroyOutputHandler destroyOutputHandler,
        PlanHandler planHandler,
        SplitGetModuleHandler splitGetModuleHandler,
        TransferStepHandler transferStepHandler,
        ApplyGetModuleHandler applyGetModuleHandler,
        ApplyInitHandler applyInitHandler,
        ApplyValidateHandler applyValidateHandler,
        ApplyVariablesHandler applyVariablesHandler,
        DestroyGetModuleHandler destroyGetModuleHandler,
        DestroyInitHandler destroyInitHandler,
        DestroyValidateHandler destroyValidateHandler,
        DestroyVariablesHandler destroyVariablesHandler,
        LookupAddressesGetModuleHandler stateListFilteredGetModuleHandler,
        LookupAddressesInitHandler stateListFilteredInitHandler,
        MoveGetModuleHandler moveGetModuleHandler,
        MoveInitHandler moveInitHandler,
        ImportGetModuleHandler importGetModuleHandler,
        ImportInitHandler importInitHandler,
        RemoveGetModuleHandler removeGetModuleHandler,
        RemoveInitHandler removeInitHandler,
        SplitInitHandler splitInitHandler,
        SplitValidateHandler splitValidateHandler,
        SplitPlanHandler splitPlanHandler,
        SplitPlanEmptyVerifyHandler planEmptyVerifyHandler,
        SplitRefactorValidateHandler refactorValidateHandler,
        SplitRefactorDiffHandler refactorDiffHandler,
        SplitMigrateMapHandler migrateMapHandler,
        SplitMigrateProveHandler migrateProveHandler,
        SplitMigrateRunHandler migrateRunHandler,
        SplitMigrateVerifyHandler migrateVerifyHandler,
        PlanDestroyHandler planDestroyHandler,
        ApplyFromPlanHandler applyFromPlanHandler,
        DestroyFromPlanHandler destroyFromPlanHandler,
        SourceRefreshHandler sourceRefreshHandler,
        RunnerConnectionValidator connectionValidator,
        IOptions<ServerSettings> serverSettings,
        RunnerConnectionRepositoryFactory connectionRepositoryFactory,
        ReportRunningTaskHandler reportRunningTaskHandler,
        CancelKillHandler cancelKillHandler)
    {
        _dbContextFactory = dbContextFactory;
        _logService = logService;
        _jobServiceFactory = jobServiceFactory;
        _bus = bus;
        _logger = logger;
        _authorizationService = authorizationService;
        _applyGetDefinitiveRevisionHandler = applyGetDefinitiveRevisionHandler;
        _destroyGetDefinitiveRevisionHandler = destroyGetDefinitiveRevisionHandler;
        _applyPolicyValidateHandler = applyPolicyValidateHandler;
        _destroyPolicyValidateHandler = destroyPolicyValidateHandler;
        _applyOutputHandler = applyOutputHandler;
        _destroyOutputHandler = destroyOutputHandler;
        _planHandler = planHandler;
        _splitGetModuleHandler = splitGetModuleHandler;
        _transferStepHandler = transferStepHandler;
        _applyGetModuleHandler = applyGetModuleHandler;
        _applyInitHandler = applyInitHandler;
        _applyValidateHandler = applyValidateHandler;
        _applyVariablesHandler = applyVariablesHandler;
        _destroyGetModuleHandler = destroyGetModuleHandler;
        _destroyInitHandler = destroyInitHandler;
        _destroyValidateHandler = destroyValidateHandler;
        _destroyVariablesHandler = destroyVariablesHandler;
        _stateListFilteredGetModuleHandler = stateListFilteredGetModuleHandler;
        _stateListFilteredInitHandler = stateListFilteredInitHandler;
        _moveGetModuleHandler = moveGetModuleHandler;
        _moveInitHandler = moveInitHandler;
        _importGetModuleHandler = importGetModuleHandler;
        _importInitHandler = importInitHandler;
        _removeGetModuleHandler = removeGetModuleHandler;
        _removeInitHandler = removeInitHandler;
        _splitInitHandler = splitInitHandler;
        _splitValidateHandler = splitValidateHandler;
        _splitPlanHandler = splitPlanHandler;
        _planEmptyVerifyHandler = planEmptyVerifyHandler;
        _refactorValidateHandler = refactorValidateHandler;
        _refactorDiffHandler = refactorDiffHandler;
        _migrateMapHandler = migrateMapHandler;
        _migrateProveHandler = migrateProveHandler;
        _migrateRunHandler = migrateRunHandler;
        _migrateVerifyHandler = migrateVerifyHandler;
        _planDestroyHandler = planDestroyHandler;
        _applyFromPlanHandler = applyFromPlanHandler;
        _destroyFromPlanHandler = destroyFromPlanHandler;
        _sourceRefreshHandler = sourceRefreshHandler;
        _connectionValidator = connectionValidator;
        _serverSettings = serverSettings.Value;
        _connectionRepositoryFactory = connectionRepositoryFactory;
        _reportRunningTaskHandler = reportRunningTaskHandler;
        _cancelKillHandler = cancelKillHandler;
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














    public async Task SplitValidateFaulted(Guid jobId, string? errorMessage, string? stackTrace)
    {
        var organizationId = await _authorizationService
            .ValidateRunnerCanAccessJob<SplitMigrateSaga>(Context, jobId);

        await _splitValidateHandler.Fault(jobId, organizationId, errorMessage, stackTrace);
    }

    public async Task TransferValidateCancelled(Guid jobId, Guid moduleId)
    {
        var organizationId = await _authorizationService.ValidateRunnerCanAccessJob<TransferMigrateSaga>(
            Context, jobId, moduleId);

        await _transferStepHandler.Cancel<TransferValidateCancelled>(jobId, moduleId, organizationId);
    }

    public async Task TransferValidateFaulted(Guid jobId, Guid moduleId, string? errorMessage, string? stackTrace)
    {
        var organizationId = await _authorizationService
            .ValidateRunnerCanAccessJob<TransferMigrateSaga>(
                Context, jobId, moduleId);

        await _transferStepHandler.Fault<TransferValidateFaulted>(
            jobId, moduleId, organizationId, errorMessage, stackTrace);
    }










    public async Task ApplyPlanCompleted(Guid jobId, PlanCompletedData data)
    {
        await _authorizationService
            .ValidateRunnerCanAccessJob<ApplyJobSaga>(Context, jobId);

        await _planHandler.Complete(jobId, data);
    }

    public async Task SplitPlanCompleted(Guid jobId, PlanCompletedData data)
    {
        var organizationId = await _authorizationService
            .ValidateRunnerCanAccessJob<SplitMigrateSaga>(Context, jobId);

        await _splitPlanHandler.Complete(jobId, organizationId, data.TotalChangedCount);
    }

    public async Task TransferPlanCompleted(Guid jobId, Guid moduleId, PlanCompletedData data)
    {
        var organizationId = await _authorizationService
            .ValidateRunnerCanAccessJob<TransferMigrateSaga>(
                Context, jobId, moduleId);

        await _transferStepHandler.Complete<TransferPlanCompleted>(
            jobId, moduleId, organizationId, c => c.TotalChangedCount = data.TotalChangedCount);
    }


    public async Task ApplyPlanCancelled(Guid jobId)
    {
        await _authorizationService
            .ValidateRunnerCanAccessJob<ApplyJobSaga>(Context, jobId);

        await _planHandler.Cancel(jobId);
    }

    public async Task SplitPlanCancelled(Guid jobId)
    {
        var organizationId = await _authorizationService
            .ValidateRunnerCanAccessJob<SplitMigrateSaga>(Context, jobId);

        await _splitPlanHandler.Cancel(jobId, organizationId);
    }


    public async Task ApplyPlanFaulted(Guid jobId, string? errorMessage, string? stackTrace, PolicyOutcome? policyOutcome = null)
    {
        await _authorizationService
            .ValidateRunnerCanAccessJob<ApplyJobSaga>(Context, jobId);

        await _planHandler.Fault(jobId, errorMessage, stackTrace, policyOutcome);
    }

    public async Task SplitPlanFaulted(Guid jobId, string? errorMessage, string? stackTrace)
    {
        var organizationId = await _authorizationService
            .ValidateRunnerCanAccessJob<SplitMigrateSaga>(Context, jobId);

        await _splitPlanHandler.Fault(jobId, organizationId, errorMessage, stackTrace);
    }

    public async Task TransferPlanCancelled(Guid jobId, Guid moduleId)
    {
        var organizationId = await _authorizationService.ValidateRunnerCanAccessJob<TransferMigrateSaga>(
            Context, jobId, moduleId);

        await _transferStepHandler.Cancel<TransferPlanCancelled>(jobId, moduleId, organizationId);
    }

    public async Task TransferPlanFaulted(Guid jobId, Guid moduleId, string? errorMessage, string? stackTrace)
    {
        var organizationId = await _authorizationService
            .ValidateRunnerCanAccessJob<TransferMigrateSaga>(
                Context, jobId, moduleId);

        await _transferStepHandler.Fault<TransferPlanFaulted>(
            jobId, moduleId, organizationId, errorMessage, stackTrace);
    }













    public async Task SplitPlanEmptyVerifyCompleted(Guid jobId)
    {
        var organizationId = await _authorizationService.ValidateRunnerCanAccessJob<SplitMigrateSaga>(
            Context, jobId);

        await _planEmptyVerifyHandler.Complete(jobId, organizationId);
    }

    public async Task SplitPlanEmptyVerifyCancelled(Guid jobId)
    {
        var organizationId = await _authorizationService.ValidateRunnerCanAccessJob<SplitMigrateSaga>(
            Context, jobId);

        await _planEmptyVerifyHandler.Cancel(jobId, organizationId);
    }

    public async Task SplitPlanEmptyVerifyFaulted(Guid jobId, string? errorMessage, string? stackTrace)
    {
        var organizationId = await _authorizationService.ValidateRunnerCanAccessJob<SplitMigrateSaga>(
            Context, jobId);

        await _planEmptyVerifyHandler.Fault(jobId, organizationId, errorMessage, stackTrace);
    }


    public async Task SplitRefactorValidateCompleted(Guid jobId)
    {
        var organizationId = await _authorizationService.ValidateRunnerCanAccessJob<SplitMigrateSaga>(
            Context, jobId);

        await _refactorValidateHandler.Complete(jobId, organizationId);
    }

    public async Task SplitRefactorValidateCancelled(Guid jobId)
    {
        var organizationId = await _authorizationService.ValidateRunnerCanAccessJob<SplitMigrateSaga>(
            Context, jobId);

        await _refactorValidateHandler.Cancel(jobId, organizationId);
    }

    public async Task SplitRefactorValidateFaulted(Guid jobId, string? errorMessage, string? stackTrace)
    {
        var organizationId = await _authorizationService.ValidateRunnerCanAccessJob<SplitMigrateSaga>(
            Context, jobId);

        await _refactorValidateHandler.Fault(jobId, organizationId, errorMessage, stackTrace);
    }

    public async Task SplitRefactorDiffCompleted(Guid jobId)
    {
        var organizationId = await _authorizationService.ValidateRunnerCanAccessJob<SplitMigrateSaga>(
            Context, jobId);

        await _refactorDiffHandler.Complete(jobId, organizationId);
    }

    public async Task SplitRefactorDiffCancelled(Guid jobId)
    {
        var organizationId = await _authorizationService.ValidateRunnerCanAccessJob<SplitMigrateSaga>(
            Context, jobId);

        await _refactorDiffHandler.Cancel(jobId, organizationId);
    }

    public async Task SplitRefactorDiffFaulted(Guid jobId, string? errorMessage, string? stackTrace)
    {
        var organizationId = await _authorizationService.ValidateRunnerCanAccessJob<SplitMigrateSaga>(
            Context, jobId);

        await _refactorDiffHandler.Fault(jobId, organizationId, errorMessage, stackTrace);
    }


    // Transfers: one job, two Modules, so every reply names the Module that is answering and is
    // authorized against that Module's own pinned runner.









    public async Task TransferMigrateMapCompleted(Guid jobId, Guid moduleId, List<string> needsOutputs)
    {
        var organizationId = await _authorizationService.ValidateRunnerCanAccessJob<TransferMigrateSaga>(
            Context, jobId, moduleId);

        await _transferStepHandler.Complete<TransferMigrateMapCompleted>(
            jobId, moduleId, organizationId, c => c.NeedsOutputs = needsOutputs);
    }

    public async Task TransferMigrateMapCancelled(Guid jobId, Guid moduleId)
    {
        var organizationId = await _authorizationService.ValidateRunnerCanAccessJob<TransferMigrateSaga>(
            Context, jobId, moduleId);

        await _transferStepHandler.Cancel<TransferMigrateMapCancelled>(jobId, moduleId, organizationId);
    }

    public async Task TransferMigrateMapFaulted(Guid jobId, Guid moduleId, string? errorMessage, string? stackTrace)
    {
        var organizationId = await _authorizationService.ValidateRunnerCanAccessJob<TransferMigrateSaga>(
            Context, jobId, moduleId);

        await _transferStepHandler.Fault<TransferMigrateMapFaulted>(
            jobId, moduleId, organizationId, errorMessage, stackTrace);
    }

    public async Task TransferMigrateProveCompleted(
        Guid jobId, Guid moduleId, int exitCode, Dictionary<string, string> outputs, string? verdict)
    {
        var organizationId = await _authorizationService.ValidateRunnerCanAccessJob<TransferMigrateSaga>(
            Context, jobId, moduleId);

        await _transferStepHandler.Complete<TransferMigrateProveCompleted>(
            jobId, moduleId, organizationId, c =>
            {
                c.ExitCode = exitCode;
                c.Outputs = outputs;
                c.Verdict = verdict;
            });
    }

    public async Task TransferMigrateProveCancelled(Guid jobId, Guid moduleId)
    {
        var organizationId = await _authorizationService.ValidateRunnerCanAccessJob<TransferMigrateSaga>(
            Context, jobId, moduleId);

        await _transferStepHandler.Cancel<TransferMigrateProveCancelled>(jobId, moduleId, organizationId);
    }

    public async Task TransferMigrateProveFaulted(Guid jobId, Guid moduleId, string? errorMessage, string? stackTrace)
    {
        var organizationId = await _authorizationService.ValidateRunnerCanAccessJob<TransferMigrateSaga>(
            Context, jobId, moduleId);

        await _transferStepHandler.Fault<TransferMigrateProveFaulted>(
            jobId, moduleId, organizationId, errorMessage, stackTrace);
    }

    public async Task TransferMigrateRunCompleted(
        Guid jobId, Guid moduleId, List<string> transferredAddresses, bool gaveUp)
    {
        var organizationId = await _authorizationService.ValidateRunnerCanAccessJob<TransferMigrateSaga>(
            Context, jobId, moduleId);

        await _transferStepHandler.Complete<TransferMigrateRunCompleted>(
            jobId, moduleId, organizationId, c =>
            {
                c.TransferredAddresses = transferredAddresses;
                c.GaveUp = gaveUp;
            });
    }

    public async Task MoveCompleted(Guid jobId, List<StateAddressResult> results)
    {
        var auth = await _authorizationService.ValidateIsForCurrentConnection(Context, jobId);

        await _bus.Publish(new MoveCompleted
        {
            CorrelationId = jobId,
            OrganizationId = auth,
            Results = Addresses(results)
        });
    }

    public async Task MoveCancelled(Guid jobId)
    {
        var auth = await _authorizationService.ValidateIsForCurrentConnection(Context, jobId);

        await _bus.Publish(new MoveCancelled
        {
            CorrelationId = jobId,
            OrganizationId = auth
        });
    }

    public async Task MoveFaulted(Guid jobId, string? errorMessage, string? stackTrace)
    {
        var auth = await _authorizationService.ValidateIsForCurrentConnection(Context, jobId);

        await _bus.Publish(new MoveFaulted
        {
            CorrelationId = jobId,
            OrganizationId = auth,
            ErrorMessage = errorMessage,
            StackTrace = stackTrace
        });
    }

    public async Task ImportCompleted(Guid jobId, List<StateAddressResult> results)
    {
        var auth = await _authorizationService.ValidateIsForCurrentConnection(Context, jobId);

        await _bus.Publish(new ImportCompleted
        {
            CorrelationId = jobId,
            OrganizationId = auth,
            Results = Addresses(results)
        });
    }

    public async Task ImportCancelled(Guid jobId)
    {
        var auth = await _authorizationService.ValidateIsForCurrentConnection(Context, jobId);

        await _bus.Publish(new ImportCancelled
        {
            CorrelationId = jobId,
            OrganizationId = auth
        });
    }

    public async Task ImportFaulted(Guid jobId, string? errorMessage, string? stackTrace)
    {
        var auth = await _authorizationService.ValidateIsForCurrentConnection(Context, jobId);

        await _bus.Publish(new ImportFaulted
        {
            CorrelationId = jobId,
            OrganizationId = auth,
            ErrorMessage = errorMessage,
            StackTrace = stackTrace
        });
    }

    public async Task RemoveCompleted(Guid jobId, List<StateAddressResult> results)
    {
        var auth = await _authorizationService.ValidateIsForCurrentConnection(Context, jobId);

        await _bus.Publish(new RemoveCompleted
        {
            CorrelationId = jobId,
            OrganizationId = auth,
            Results = Addresses(results)
        });
    }

    public async Task RemoveCancelled(Guid jobId)
    {
        var auth = await _authorizationService.ValidateIsForCurrentConnection(Context, jobId);

        await _bus.Publish(new RemoveCancelled
        {
            CorrelationId = jobId,
            OrganizationId = auth
        });
    }

    public async Task RemoveFaulted(Guid jobId, string? errorMessage, string? stackTrace)
    {
        var auth = await _authorizationService.ValidateIsForCurrentConnection(Context, jobId);

        await _bus.Publish(new RemoveFaulted
        {
            CorrelationId = jobId,
            OrganizationId = auth,
            ErrorMessage = errorMessage,
            StackTrace = stackTrace
        });
    }

    public async Task MoveDryRunCompleted(Guid jobId, List<StateAddressResult> results)
    {
        var auth = await _authorizationService.ValidateIsForCurrentConnection(Context, jobId);

        await _bus.Publish(new MoveDryRunCompleted
        {
            CorrelationId = jobId,
            OrganizationId = auth,
            Results = Addresses(results)
        });
    }

    public async Task MoveDryRunCancelled(Guid jobId)
    {
        var auth = await _authorizationService.ValidateIsForCurrentConnection(Context, jobId);

        await _bus.Publish(new MoveDryRunCancelled
        {
            CorrelationId = jobId,
            OrganizationId = auth
        });
    }

    public async Task MoveDryRunFaulted(Guid jobId, string? errorMessage, string? stackTrace)
    {
        var auth = await _authorizationService.ValidateIsForCurrentConnection(Context, jobId);

        await _bus.Publish(new MoveDryRunFaulted
        {
            CorrelationId = jobId,
            OrganizationId = auth,
            ErrorMessage = errorMessage,
            StackTrace = stackTrace
        });
    }

    public async Task RemoveDryRunCompleted(Guid jobId, List<StateAddressResult> results)
    {
        var auth = await _authorizationService.ValidateIsForCurrentConnection(Context, jobId);

        await _bus.Publish(new RemoveDryRunCompleted
        {
            CorrelationId = jobId,
            OrganizationId = auth,
            Results = Addresses(results)
        });
    }

    public async Task RemoveDryRunCancelled(Guid jobId)
    {
        var auth = await _authorizationService.ValidateIsForCurrentConnection(Context, jobId);

        await _bus.Publish(new RemoveDryRunCancelled
        {
            CorrelationId = jobId,
            OrganizationId = auth
        });
    }

    public async Task RemoveDryRunFaulted(Guid jobId, string? errorMessage, string? stackTrace)
    {
        var auth = await _authorizationService.ValidateIsForCurrentConnection(Context, jobId);

        await _bus.Publish(new RemoveDryRunFaulted
        {
            CorrelationId = jobId,
            OrganizationId = auth,
            ErrorMessage = errorMessage,
            StackTrace = stackTrace
        });
    }

    public async Task ImportPreCheckCompleted(Guid jobId, List<StateAddressResult> results)
    {
        var auth = await _authorizationService.ValidateIsForCurrentConnection(Context, jobId);

        await _bus.Publish(new ImportPreCheckCompleted
        {
            CorrelationId = jobId,
            OrganizationId = auth,
            Results = Addresses(results)
        });
    }

    public async Task ImportPreCheckCancelled(Guid jobId)
    {
        var auth = await _authorizationService.ValidateIsForCurrentConnection(Context, jobId);

        await _bus.Publish(new ImportPreCheckCancelled
        {
            CorrelationId = jobId,
            OrganizationId = auth
        });
    }

    public async Task ImportPreCheckFaulted(Guid jobId, string? errorMessage, string? stackTrace)
    {
        var auth = await _authorizationService.ValidateIsForCurrentConnection(Context, jobId);

        await _bus.Publish(new ImportPreCheckFaulted
        {
            CorrelationId = jobId,
            OrganizationId = auth,
            ErrorMessage = errorMessage,
            StackTrace = stackTrace
        });
    }

    /// <summary>One address and what became of it, as the runner reported it.</summary>
    private static List<AddressResult> Addresses(List<StateAddressResult> results) =>
        results
            .Select(r => new AddressResult
            {
                Address = r.Address,
                Target = r.Target,
                Outcome = Enum.Parse<AddressOutcome>(r.Outcome)
            })
            .ToList();

    public async Task LookupAddressesCompleted(Guid jobId, List<StateAddressResult> results)
    {
        var auth = await _authorizationService.ValidateIsForCurrentConnection(Context, jobId);

        await _bus.Publish(new LookupAddressesCompleted
        {
            CorrelationId = jobId,
            OrganizationId = auth,
            Results = results.Select(r => new AddressResult
            {
                Address = r.Address,
                Target = r.Target,
                Outcome = Enum.Parse<AddressOutcome>(r.Outcome)
            }).ToList()
        });
    }

    public async Task LookupAddressesCancelled(Guid jobId)
    {
        var auth = await _authorizationService.ValidateIsForCurrentConnection(Context, jobId);

        await _bus.Publish(new LookupAddressesCancelled
        {
            CorrelationId = jobId,
            OrganizationId = auth
        });
    }

    public async Task LookupAddressesFaulted(Guid jobId, string? errorMessage, string? stackTrace)
    {
        var auth = await _authorizationService.ValidateIsForCurrentConnection(Context, jobId);

        await _bus.Publish(new LookupAddressesFaulted
        {
            CorrelationId = jobId,
            OrganizationId = auth,
            ErrorMessage = errorMessage,
            StackTrace = stackTrace
        });
    }

    public async Task TransferOutputsCompleted(Guid jobId, Guid moduleId, OutputSetCreateDto? outputSet)
    {
        var organizationId = await _authorizationService.ValidateRunnerCanAccessJob<TransferMigrateSaga>(
            Context, jobId, moduleId);

        // Storing opens its own transaction, so it happens in a consumer rather than in the
        // saga, whose context is already in one.
        await _bus.Publish(new TransferOutputsCompletedInvoked
        {
            JobId = jobId,
            ModuleId = moduleId,
            OrganizationId = organizationId,
            OutputSet = outputSet
        });
    }

    public async Task TransferOutputsCancelled(Guid jobId, Guid moduleId)
    {
        var organizationId = await _authorizationService.ValidateRunnerCanAccessJob<TransferMigrateSaga>(
            Context, jobId, moduleId);

        await _transferStepHandler.Cancel<TransferOutputsCancelled>(jobId, moduleId, organizationId);
    }

    public async Task TransferOutputsFaulted(Guid jobId, Guid moduleId, string? errorMessage, string? stackTrace)
    {
        var organizationId = await _authorizationService.ValidateRunnerCanAccessJob<TransferMigrateSaga>(
            Context, jobId, moduleId);

        await _transferStepHandler.Fault<TransferOutputsFaulted>(
            jobId, moduleId, organizationId, errorMessage, stackTrace);
    }

    public async Task TransferMigrateRunCancelled(Guid jobId, Guid moduleId)
    {
        var organizationId = await _authorizationService.ValidateRunnerCanAccessJob<TransferMigrateSaga>(
            Context, jobId, moduleId);

        await _transferStepHandler.Cancel<TransferMigrateRunCancelled>(jobId, moduleId, organizationId);
    }

    public async Task TransferMigrateRunFaulted(Guid jobId, Guid moduleId, string? errorMessage, string? stackTrace)
    {
        var organizationId = await _authorizationService.ValidateRunnerCanAccessJob<TransferMigrateSaga>(
            Context, jobId, moduleId);

        await _transferStepHandler.Fault<TransferMigrateRunFaulted>(
            jobId, moduleId, organizationId, errorMessage, stackTrace);
    }

    public async Task TransferMigrateVerifyCompleted(Guid jobId, Guid moduleId)
    {
        var organizationId = await _authorizationService.ValidateRunnerCanAccessJob<TransferMigrateSaga>(
            Context, jobId, moduleId);

        await _transferStepHandler.Complete<TransferMigrateVerifyCompleted>(jobId, moduleId, organizationId);
    }

    public async Task TransferMigrateVerifyCancelled(Guid jobId, Guid moduleId)
    {
        var organizationId = await _authorizationService.ValidateRunnerCanAccessJob<TransferMigrateSaga>(
            Context, jobId, moduleId);

        await _transferStepHandler.Cancel<TransferMigrateVerifyCancelled>(jobId, moduleId, organizationId);
    }

    public async Task TransferMigrateVerifyFaulted(Guid jobId, Guid moduleId, string? errorMessage, string? stackTrace)
    {
        var organizationId = await _authorizationService.ValidateRunnerCanAccessJob<TransferMigrateSaga>(
            Context, jobId, moduleId);

        await _transferStepHandler.Fault<TransferMigrateVerifyFaulted>(
            jobId, moduleId, organizationId, errorMessage, stackTrace);
    }

    public async Task SplitMigrateMapCompleted(Guid jobId, string? refactorMapHash, List<string> carvedModuleNames, int resourcesMoved)
    {
        var organizationId = await _authorizationService.ValidateRunnerCanAccessJob<SplitMigrateSaga>(
            Context, jobId);

        await _migrateMapHandler.Complete(jobId, organizationId, refactorMapHash, carvedModuleNames, resourcesMoved);
    }

    public async Task SplitMigrateMapCancelled(Guid jobId)
    {
        var organizationId = await _authorizationService.ValidateRunnerCanAccessJob<SplitMigrateSaga>(
            Context, jobId);

        await _migrateMapHandler.Cancel(jobId, organizationId);
    }

    public async Task SplitMigrateMapFaulted(Guid jobId, string? errorMessage, string? stackTrace)
    {
        var organizationId = await _authorizationService.ValidateRunnerCanAccessJob<SplitMigrateSaga>(
            Context, jobId);

        await _migrateMapHandler.Fault(jobId, organizationId, errorMessage, stackTrace);
    }

    public async Task SplitMigrateProveCompleted(Guid jobId, int modulesProven, int modulesPlanningClean)
    {
        var organizationId = await _authorizationService.ValidateRunnerCanAccessJob<SplitMigrateSaga>(
            Context, jobId);

        await _migrateProveHandler.Complete(jobId, organizationId, modulesProven, modulesPlanningClean);
    }

    public async Task SplitMigrateProveCancelled(Guid jobId)
    {
        var organizationId = await _authorizationService.ValidateRunnerCanAccessJob<SplitMigrateSaga>(
            Context, jobId);

        await _migrateProveHandler.Cancel(jobId, organizationId);
    }

    public async Task SplitMigrateProveFaulted(Guid jobId, string? errorMessage, string? stackTrace)
    {
        var organizationId = await _authorizationService.ValidateRunnerCanAccessJob<SplitMigrateSaga>(
            Context, jobId);

        await _migrateProveHandler.Fault(jobId, organizationId, errorMessage, stackTrace);
    }

    public async Task SplitMigrateRunCompleted(Guid jobId)
    {
        var organizationId = await _authorizationService.ValidateRunnerCanAccessJob<SplitMigrateSaga>(
            Context, jobId);

        await _migrateRunHandler.Complete(jobId, organizationId);
    }

    public async Task SplitMigrateRunCancelled(Guid jobId)
    {
        var organizationId = await _authorizationService.ValidateRunnerCanAccessJob<SplitMigrateSaga>(
            Context, jobId);

        await _migrateRunHandler.Cancel(jobId, organizationId);
    }

    public async Task SplitMigrateRunFaulted(Guid jobId, string? errorMessage, string? stackTrace)
    {
        var organizationId = await _authorizationService.ValidateRunnerCanAccessJob<SplitMigrateSaga>(
            Context, jobId);

        await _migrateRunHandler.Fault(jobId, organizationId, errorMessage, stackTrace);
    }

    public async Task SplitMigrateVerifyCompleted(Guid jobId, int modulesProven, int modulesPlanningClean)
    {
        var organizationId = await _authorizationService.ValidateRunnerCanAccessJob<SplitMigrateSaga>(
            Context, jobId);

        await _migrateVerifyHandler.Complete(jobId, organizationId, modulesProven, modulesPlanningClean);
    }

    public async Task SplitMigrateVerifyCancelled(Guid jobId)
    {
        var organizationId = await _authorizationService.ValidateRunnerCanAccessJob<SplitMigrateSaga>(
            Context, jobId);

        await _migrateVerifyHandler.Cancel(jobId, organizationId);
    }

    public async Task SplitMigrateVerifyFaulted(Guid jobId, string? errorMessage, string? stackTrace)
    {
        var organizationId = await _authorizationService.ValidateRunnerCanAccessJob<SplitMigrateSaga>(
            Context, jobId);

        await _migrateVerifyHandler.Fault(jobId, organizationId, errorMessage, stackTrace);
    }

    public async Task DestroyPlanCompleted(Guid jobId, PlanCompletedData data)
    {
        await _authorizationService
            .ValidateRunnerCanAccessJob<DestroyJobSaga>(Context, jobId);

        await _planDestroyHandler.Complete(jobId, data);
    }

    public async Task DestroyPlanCancelled(Guid jobId)
    {
        await _authorizationService
            .ValidateRunnerCanAccessJob<DestroyJobSaga>(Context, jobId);

        await _planDestroyHandler.Cancel(jobId);
    }

    public async Task DestroyPlanFaulted(Guid jobId, string? errorMessage, string? stackTrace, PolicyOutcome? policyOutcome = null)
    {
        await _authorizationService
            .ValidateRunnerCanAccessJob<DestroyJobSaga>(Context, jobId);

        await _planDestroyHandler.Fault(jobId, errorMessage, stackTrace, policyOutcome);
    }

    public async Task ApplyFromPlanCompleted(Guid jobId, int? actualResourceCount)
    {
        await _authorizationService
            .ValidateRunnerCanAccessJob<ApplyJobSaga>(Context, jobId);

        await _applyFromPlanHandler.Complete(jobId, actualResourceCount);
    }

    public async Task ApplyFromPlanCancelled(Guid jobId)
    {
        await _authorizationService
            .ValidateRunnerCanAccessJob<ApplyJobSaga>(Context, jobId);

        await _applyFromPlanHandler.Cancel(jobId);
    }

    public async Task ApplyFromPlanFaulted(Guid jobId, string? errorMessage, string? stackTrace, int? actualResourceCount)
    {
        await _authorizationService
            .ValidateRunnerCanAccessJob<ApplyJobSaga>(Context, jobId);

        await _applyFromPlanHandler.Fault(jobId, errorMessage, stackTrace, actualResourceCount);
    }

    public async Task DestroyFromPlanCompleted(Guid jobId, int? actualResourceCount)
    {
        await _authorizationService
            .ValidateRunnerCanAccessJob<DestroyJobSaga>(Context, jobId);

        await _destroyFromPlanHandler.Complete(jobId, actualResourceCount);
    }

    public async Task DestroyFromPlanCancelled(Guid jobId)
    {
        await _authorizationService
            .ValidateRunnerCanAccessJob<DestroyJobSaga>(Context, jobId);

        await _destroyFromPlanHandler.Cancel(jobId);
    }

    public async Task DestroyFromPlanFaulted(Guid jobId, string? errorMessage, string? stackTrace, int? actualResourceCount)
    {
        await _authorizationService
            .ValidateRunnerCanAccessJob<DestroyJobSaga>(Context, jobId);

        await _destroyFromPlanHandler.Fault(jobId, errorMessage, stackTrace, actualResourceCount);
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

    /// <summary>Called by the runner when a kill cancellation completes on a apply job.</summary>
    public async Task ApplyCancelKillCompleted(Guid jobId)
    {
        await _authorizationService.ValidateRunnerCanAccessJob<ApplyJobSaga>(
            Context, jobId);

        await _cancelKillHandler.Complete<Events.Steps.ApplyCancelKillCompleted>(jobId);
    }

    /// <summary>Called by the runner when a kill cancellation completes on a destroy job.</summary>
    public async Task DestroyCancelKillCompleted(Guid jobId)
    {
        await _authorizationService.ValidateRunnerCanAccessJob<DestroyJobSaga>(
            Context, jobId);

        await _cancelKillHandler.Complete<Events.Steps.DestroyCancelKillCompleted>(jobId);
    }

    /// <summary>Called by the runner when a kill cancellation completes on a split job.</summary>
    public async Task SplitCancelKillCompleted(Guid jobId)
    {
        await _authorizationService.ValidateRunnerCanAccessJob<SplitMigrateSaga>(
            Context, jobId);

        await _cancelKillHandler.Complete<Events.Steps.SplitCancelKillCompleted>(jobId);
    }

    public async Task MoveCancelKillCompleted(Guid jobId)
    {
        await _authorizationService.ValidateRunnerCanAccessJob<MoveSaga>(Context, jobId);

        await _cancelKillHandler.Complete<Events.Steps.MoveCancelKillCompleted>(jobId);
    }

    public async Task ImportCancelKillCompleted(Guid jobId)
    {
        await _authorizationService.ValidateRunnerCanAccessJob<ImportSaga>(Context, jobId);

        await _cancelKillHandler.Complete<Events.Steps.ImportCancelKillCompleted>(jobId);
    }

    public async Task RemoveCancelKillCompleted(Guid jobId)
    {
        await _authorizationService.ValidateRunnerCanAccessJob<RemoveSaga>(Context, jobId);

        await _cancelKillHandler.Complete<Events.Steps.RemoveCancelKillCompleted>(jobId);
    }


    // Each manual family answers on its own endpoints. The endpoint names the family, so the
    // saga is read from that family's table rather than searched for across all of them.

    public async Task LookupAddressesGetModuleCompleted(Guid jobId)
    {
        var organizationId = await _authorizationService
            .ValidateRunnerCanAccessJob<LookupAddressesSaga>(Context, jobId);

        await _stateListFilteredGetModuleHandler.Complete(jobId, organizationId);
    }

    public async Task LookupAddressesGetModuleCancelled(Guid jobId)
    {
        var organizationId = await _authorizationService
            .ValidateRunnerCanAccessJob<LookupAddressesSaga>(Context, jobId);

        await _stateListFilteredGetModuleHandler.Cancel(jobId, organizationId);
    }

    public async Task LookupAddressesGetModuleFaulted(Guid jobId, string? errorMessage, string? stackTrace)
    {
        var organizationId = await _authorizationService
            .ValidateRunnerCanAccessJob<LookupAddressesSaga>(Context, jobId);

        await _stateListFilteredGetModuleHandler.Fault(jobId, organizationId, errorMessage, stackTrace);
    }

    public async Task LookupAddressesInitCompleted(Guid jobId)
    {
        var organizationId = await _authorizationService
            .ValidateRunnerCanAccessJob<LookupAddressesSaga>(Context, jobId);

        await _stateListFilteredInitHandler.Complete(jobId, organizationId);
    }

    public async Task LookupAddressesInitCancelled(Guid jobId)
    {
        var organizationId = await _authorizationService
            .ValidateRunnerCanAccessJob<LookupAddressesSaga>(Context, jobId);

        await _stateListFilteredInitHandler.Cancel(jobId, organizationId);
    }

    public async Task LookupAddressesInitFaulted(Guid jobId, string? errorMessage, string? stackTrace)
    {
        var organizationId = await _authorizationService
            .ValidateRunnerCanAccessJob<LookupAddressesSaga>(Context, jobId);

        await _stateListFilteredInitHandler.Fault(jobId, organizationId, errorMessage, stackTrace);
    }

    public async Task MoveGetModuleCompleted(Guid jobId)
    {
        var organizationId = await _authorizationService
            .ValidateRunnerCanAccessJob<MoveSaga>(Context, jobId);

        await _moveGetModuleHandler.Complete(jobId, organizationId);
    }

    public async Task MoveGetModuleCancelled(Guid jobId)
    {
        var organizationId = await _authorizationService
            .ValidateRunnerCanAccessJob<MoveSaga>(Context, jobId);

        await _moveGetModuleHandler.Cancel(jobId, organizationId);
    }

    public async Task MoveGetModuleFaulted(Guid jobId, string? errorMessage, string? stackTrace)
    {
        var organizationId = await _authorizationService
            .ValidateRunnerCanAccessJob<MoveSaga>(Context, jobId);

        await _moveGetModuleHandler.Fault(jobId, organizationId, errorMessage, stackTrace);
    }

    public async Task MoveInitCompleted(Guid jobId)
    {
        var organizationId = await _authorizationService
            .ValidateRunnerCanAccessJob<MoveSaga>(Context, jobId);

        await _moveInitHandler.Complete(jobId, organizationId);
    }

    public async Task MoveInitCancelled(Guid jobId)
    {
        var organizationId = await _authorizationService
            .ValidateRunnerCanAccessJob<MoveSaga>(Context, jobId);

        await _moveInitHandler.Cancel(jobId, organizationId);
    }

    public async Task MoveInitFaulted(Guid jobId, string? errorMessage, string? stackTrace)
    {
        var organizationId = await _authorizationService
            .ValidateRunnerCanAccessJob<MoveSaga>(Context, jobId);

        await _moveInitHandler.Fault(jobId, organizationId, errorMessage, stackTrace);
    }

    public async Task ImportGetModuleCompleted(Guid jobId)
    {
        var organizationId = await _authorizationService
            .ValidateRunnerCanAccessJob<ImportSaga>(Context, jobId);

        await _importGetModuleHandler.Complete(jobId, organizationId);
    }

    public async Task ImportGetModuleCancelled(Guid jobId)
    {
        var organizationId = await _authorizationService
            .ValidateRunnerCanAccessJob<ImportSaga>(Context, jobId);

        await _importGetModuleHandler.Cancel(jobId, organizationId);
    }

    public async Task ImportGetModuleFaulted(Guid jobId, string? errorMessage, string? stackTrace)
    {
        var organizationId = await _authorizationService
            .ValidateRunnerCanAccessJob<ImportSaga>(Context, jobId);

        await _importGetModuleHandler.Fault(jobId, organizationId, errorMessage, stackTrace);
    }

    public async Task ImportInitCompleted(Guid jobId)
    {
        var organizationId = await _authorizationService
            .ValidateRunnerCanAccessJob<ImportSaga>(Context, jobId);

        await _importInitHandler.Complete(jobId, organizationId);
    }

    public async Task ImportInitCancelled(Guid jobId)
    {
        var organizationId = await _authorizationService
            .ValidateRunnerCanAccessJob<ImportSaga>(Context, jobId);

        await _importInitHandler.Cancel(jobId, organizationId);
    }

    public async Task ImportInitFaulted(Guid jobId, string? errorMessage, string? stackTrace)
    {
        var organizationId = await _authorizationService
            .ValidateRunnerCanAccessJob<ImportSaga>(Context, jobId);

        await _importInitHandler.Fault(jobId, organizationId, errorMessage, stackTrace);
    }

    public async Task RemoveGetModuleCompleted(Guid jobId)
    {
        var organizationId = await _authorizationService
            .ValidateRunnerCanAccessJob<RemoveSaga>(Context, jobId);

        await _removeGetModuleHandler.Complete(jobId, organizationId);
    }

    public async Task RemoveGetModuleCancelled(Guid jobId)
    {
        var organizationId = await _authorizationService
            .ValidateRunnerCanAccessJob<RemoveSaga>(Context, jobId);

        await _removeGetModuleHandler.Cancel(jobId, organizationId);
    }

    public async Task RemoveGetModuleFaulted(Guid jobId, string? errorMessage, string? stackTrace)
    {
        var organizationId = await _authorizationService
            .ValidateRunnerCanAccessJob<RemoveSaga>(Context, jobId);

        await _removeGetModuleHandler.Fault(jobId, organizationId, errorMessage, stackTrace);
    }

    public async Task RemoveInitCompleted(Guid jobId)
    {
        var organizationId = await _authorizationService
            .ValidateRunnerCanAccessJob<RemoveSaga>(Context, jobId);

        await _removeInitHandler.Complete(jobId, organizationId);
    }

    public async Task RemoveInitCancelled(Guid jobId)
    {
        var organizationId = await _authorizationService
            .ValidateRunnerCanAccessJob<RemoveSaga>(Context, jobId);

        await _removeInitHandler.Cancel(jobId, organizationId);
    }

    public async Task RemoveInitFaulted(Guid jobId, string? errorMessage, string? stackTrace)
    {
        var organizationId = await _authorizationService
            .ValidateRunnerCanAccessJob<RemoveSaga>(Context, jobId);

        await _removeInitHandler.Fault(jobId, organizationId, errorMessage, stackTrace);
    }

    // Apply and destroy answer on their own endpoints: the endpoint names the job kind, so the
    // saga is read from that kind's own table.

    public async Task ApplyGetModuleCompleted(Guid jobId)
    {
        var organizationId = await _authorizationService
            .ValidateRunnerCanAccessJob<ApplyJobSaga>(Context, jobId);

        await _applyGetModuleHandler.Complete(jobId);
    }

    public async Task ApplyGetModuleCancelled(Guid jobId)
    {
        var organizationId = await _authorizationService
            .ValidateRunnerCanAccessJob<ApplyJobSaga>(Context, jobId);

        await _applyGetModuleHandler.Cancel(jobId);
    }

    public async Task ApplyGetModuleFaulted(Guid jobId, string? errorMessage, string? stackTrace)
    {
        var organizationId = await _authorizationService
            .ValidateRunnerCanAccessJob<ApplyJobSaga>(Context, jobId);

        await _applyGetModuleHandler.Fault(jobId, errorMessage, stackTrace);
    }

    public async Task ApplyInitCompleted(Guid jobId)
    {
        var organizationId = await _authorizationService
            .ValidateRunnerCanAccessJob<ApplyJobSaga>(Context, jobId);

        await _applyInitHandler.Complete(jobId);
    }

    public async Task ApplyInitCancelled(Guid jobId)
    {
        var organizationId = await _authorizationService
            .ValidateRunnerCanAccessJob<ApplyJobSaga>(Context, jobId);

        await _applyInitHandler.Cancel(jobId);
    }

    public async Task ApplyInitFaulted(Guid jobId, string? errorMessage, string? stackTrace)
    {
        var organizationId = await _authorizationService
            .ValidateRunnerCanAccessJob<ApplyJobSaga>(Context, jobId);

        await _applyInitHandler.Fault(jobId, errorMessage, stackTrace);
    }

    public async Task ApplyValidateCompleted(Guid jobId)
    {
        var organizationId = await _authorizationService
            .ValidateRunnerCanAccessJob<ApplyJobSaga>(Context, jobId);

        await _applyValidateHandler.Complete(jobId);
    }

    public async Task ApplyValidateCancelled(Guid jobId)
    {
        var organizationId = await _authorizationService
            .ValidateRunnerCanAccessJob<ApplyJobSaga>(Context, jobId);

        await _applyValidateHandler.Cancel(jobId);
    }

    public async Task ApplyValidateFaulted(Guid jobId, string? errorMessage, string? stackTrace)
    {
        var organizationId = await _authorizationService
            .ValidateRunnerCanAccessJob<ApplyJobSaga>(Context, jobId);

        await _applyValidateHandler.Fault(jobId, errorMessage, stackTrace);
    }

    public async Task ApplyVariablesCompleted(Guid jobId, VariableSetCreateDto? variableSet)
    {
        var organizationId = await _authorizationService
            .ValidateRunnerCanAccessJob<ApplyJobSaga>(Context, jobId);

        await _applyVariablesHandler.Complete(jobId, variableSet);
    }

    public async Task ApplyVariablesCancelled(Guid jobId)
    {
        var organizationId = await _authorizationService
            .ValidateRunnerCanAccessJob<ApplyJobSaga>(Context, jobId);

        await _applyVariablesHandler.Cancel(jobId);
    }

    public async Task ApplyVariablesFaulted(Guid jobId, string? errorMessage, string? stackTrace)
    {
        var organizationId = await _authorizationService
            .ValidateRunnerCanAccessJob<ApplyJobSaga>(Context, jobId);

        await _applyVariablesHandler.Fault(jobId, errorMessage, stackTrace);
    }

    public async Task DestroyGetModuleCompleted(Guid jobId)
    {
        var organizationId = await _authorizationService
            .ValidateRunnerCanAccessJob<DestroyJobSaga>(Context, jobId);

        await _destroyGetModuleHandler.Complete(jobId);
    }

    public async Task DestroyGetModuleCancelled(Guid jobId)
    {
        var organizationId = await _authorizationService
            .ValidateRunnerCanAccessJob<DestroyJobSaga>(Context, jobId);

        await _destroyGetModuleHandler.Cancel(jobId);
    }

    public async Task DestroyGetModuleFaulted(Guid jobId, string? errorMessage, string? stackTrace)
    {
        var organizationId = await _authorizationService
            .ValidateRunnerCanAccessJob<DestroyJobSaga>(Context, jobId);

        await _destroyGetModuleHandler.Fault(jobId, errorMessage, stackTrace);
    }

    public async Task DestroyInitCompleted(Guid jobId)
    {
        var organizationId = await _authorizationService
            .ValidateRunnerCanAccessJob<DestroyJobSaga>(Context, jobId);

        await _destroyInitHandler.Complete(jobId);
    }

    public async Task DestroyInitCancelled(Guid jobId)
    {
        var organizationId = await _authorizationService
            .ValidateRunnerCanAccessJob<DestroyJobSaga>(Context, jobId);

        await _destroyInitHandler.Cancel(jobId);
    }

    public async Task DestroyInitFaulted(Guid jobId, string? errorMessage, string? stackTrace)
    {
        var organizationId = await _authorizationService
            .ValidateRunnerCanAccessJob<DestroyJobSaga>(Context, jobId);

        await _destroyInitHandler.Fault(jobId, errorMessage, stackTrace);
    }

    public async Task DestroyValidateCompleted(Guid jobId)
    {
        var organizationId = await _authorizationService
            .ValidateRunnerCanAccessJob<DestroyJobSaga>(Context, jobId);

        await _destroyValidateHandler.Complete(jobId);
    }

    public async Task DestroyValidateCancelled(Guid jobId)
    {
        var organizationId = await _authorizationService
            .ValidateRunnerCanAccessJob<DestroyJobSaga>(Context, jobId);

        await _destroyValidateHandler.Cancel(jobId);
    }

    public async Task DestroyValidateFaulted(Guid jobId, string? errorMessage, string? stackTrace)
    {
        var organizationId = await _authorizationService
            .ValidateRunnerCanAccessJob<DestroyJobSaga>(Context, jobId);

        await _destroyValidateHandler.Fault(jobId, errorMessage, stackTrace);
    }

    public async Task DestroyVariablesCompleted(Guid jobId, VariableSetCreateDto? variableSet)
    {
        var organizationId = await _authorizationService
            .ValidateRunnerCanAccessJob<DestroyJobSaga>(Context, jobId);

        await _destroyVariablesHandler.Complete(jobId, variableSet);
    }

    public async Task DestroyVariablesCancelled(Guid jobId)
    {
        var organizationId = await _authorizationService
            .ValidateRunnerCanAccessJob<DestroyJobSaga>(Context, jobId);

        await _destroyVariablesHandler.Cancel(jobId);
    }

    public async Task DestroyVariablesFaulted(Guid jobId, string? errorMessage, string? stackTrace)
    {
        var organizationId = await _authorizationService
            .ValidateRunnerCanAccessJob<DestroyJobSaga>(Context, jobId);

        await _destroyVariablesHandler.Fault(jobId, errorMessage, stackTrace);
    }

    // Apply and destroy answer on their own endpoints for these steps too.

    public async Task ApplyGetDefinitiveRevisionCompleted(Guid jobId, string definitiveRevision)
    {
        await _authorizationService
            .ValidateRunnerCanAccessJob<ApplyJobSaga>(Context, jobId);

        await _applyGetDefinitiveRevisionHandler.Complete(jobId, definitiveRevision);
    }

    public async Task ApplyGetDefinitiveRevisionCancelled(Guid jobId)
    {
        await _authorizationService
            .ValidateRunnerCanAccessJob<ApplyJobSaga>(Context, jobId);

        await _applyGetDefinitiveRevisionHandler.Cancel(jobId);
    }

    public async Task ApplyGetDefinitiveRevisionFaulted(Guid jobId, string? errorMessage, string? stackTrace)
    {
        await _authorizationService
            .ValidateRunnerCanAccessJob<ApplyJobSaga>(Context, jobId);

        await _applyGetDefinitiveRevisionHandler.Fault(jobId, errorMessage, stackTrace);
    }

    public async Task DestroyGetDefinitiveRevisionCompleted(Guid jobId, string definitiveRevision)
    {
        await _authorizationService
            .ValidateRunnerCanAccessJob<DestroyJobSaga>(Context, jobId);

        await _destroyGetDefinitiveRevisionHandler.Complete(jobId, definitiveRevision);
    }

    public async Task DestroyGetDefinitiveRevisionCancelled(Guid jobId)
    {
        await _authorizationService
            .ValidateRunnerCanAccessJob<DestroyJobSaga>(Context, jobId);

        await _destroyGetDefinitiveRevisionHandler.Cancel(jobId);
    }

    public async Task DestroyGetDefinitiveRevisionFaulted(Guid jobId, string? errorMessage, string? stackTrace)
    {
        await _authorizationService
            .ValidateRunnerCanAccessJob<DestroyJobSaga>(Context, jobId);

        await _destroyGetDefinitiveRevisionHandler.Fault(jobId, errorMessage, stackTrace);
    }

    public async Task ApplyPolicyValidateCompleted(Guid jobId, PolicyOutcome outcome)
    {
        await _authorizationService
            .ValidateRunnerCanAccessJob<ApplyJobSaga>(Context, jobId);

        await _applyPolicyValidateHandler.Complete(jobId, outcome);
    }

    public async Task ApplyPolicyValidateCancelled(Guid jobId)
    {
        await _authorizationService
            .ValidateRunnerCanAccessJob<ApplyJobSaga>(Context, jobId);

        await _applyPolicyValidateHandler.Cancel(jobId);
    }

    public async Task ApplyPolicyValidateFaulted(Guid jobId, string? errorMessage, string? stackTrace)
    {
        await _authorizationService
            .ValidateRunnerCanAccessJob<ApplyJobSaga>(Context, jobId);

        await _applyPolicyValidateHandler.Fault(jobId, errorMessage, stackTrace);
    }

    public async Task DestroyPolicyValidateCompleted(Guid jobId, PolicyOutcome outcome)
    {
        await _authorizationService
            .ValidateRunnerCanAccessJob<DestroyJobSaga>(Context, jobId);

        await _destroyPolicyValidateHandler.Complete(jobId, outcome);
    }

    public async Task DestroyPolicyValidateCancelled(Guid jobId)
    {
        await _authorizationService
            .ValidateRunnerCanAccessJob<DestroyJobSaga>(Context, jobId);

        await _destroyPolicyValidateHandler.Cancel(jobId);
    }

    public async Task DestroyPolicyValidateFaulted(Guid jobId, string? errorMessage, string? stackTrace)
    {
        await _authorizationService
            .ValidateRunnerCanAccessJob<DestroyJobSaga>(Context, jobId);

        await _destroyPolicyValidateHandler.Fault(jobId, errorMessage, stackTrace);
    }

    public async Task ApplyOutputCompleted(Guid jobId, OutputSetCreateDto? outputSet)
    {
        await _authorizationService
            .ValidateRunnerCanAccessJob<ApplyJobSaga>(Context, jobId);

        await _applyOutputHandler.Complete(jobId, outputSet);
    }

    public async Task ApplyOutputCancelled(Guid jobId)
    {
        await _authorizationService
            .ValidateRunnerCanAccessJob<ApplyJobSaga>(Context, jobId);

        await _applyOutputHandler.Cancel(jobId);
    }

    public async Task ApplyOutputFaulted(Guid jobId, string? errorMessage, string? stackTrace)
    {
        await _authorizationService
            .ValidateRunnerCanAccessJob<ApplyJobSaga>(Context, jobId);

        await _applyOutputHandler.Fault(jobId, errorMessage, stackTrace);
    }

    public async Task DestroyOutputCompleted(Guid jobId, OutputSetCreateDto? outputSet)
    {
        await _authorizationService
            .ValidateRunnerCanAccessJob<DestroyJobSaga>(Context, jobId);

        await _destroyOutputHandler.Complete(jobId, outputSet);
    }

    public async Task DestroyOutputCancelled(Guid jobId)
    {
        await _authorizationService
            .ValidateRunnerCanAccessJob<DestroyJobSaga>(Context, jobId);

        await _destroyOutputHandler.Cancel(jobId);
    }

    public async Task DestroyOutputFaulted(Guid jobId, string? errorMessage, string? stackTrace)
    {
        await _authorizationService
            .ValidateRunnerCanAccessJob<DestroyJobSaga>(Context, jobId);

        await _destroyOutputHandler.Fault(jobId, errorMessage, stackTrace);
    }

    // Split answers on its own endpoints for the setup steps too.

    public async Task SplitGetModuleCompleted(Guid jobId)
    {
        var organizationId = await _authorizationService
            .ValidateRunnerCanAccessJob<SplitMigrateSaga>(Context, jobId);

        await _splitGetModuleHandler.Complete(jobId, organizationId);
    }

    public async Task SplitGetModuleCancelled(Guid jobId)
    {
        var organizationId = await _authorizationService
            .ValidateRunnerCanAccessJob<SplitMigrateSaga>(Context, jobId);

        await _splitGetModuleHandler.Cancel(jobId, organizationId);
    }

    public async Task SplitGetModuleFaulted(Guid jobId, string? errorMessage, string? stackTrace)
    {
        var organizationId = await _authorizationService
            .ValidateRunnerCanAccessJob<SplitMigrateSaga>(Context, jobId);

        await _splitGetModuleHandler.Fault(jobId, organizationId, errorMessage, stackTrace);
    }

    public async Task SplitInitCompleted(Guid jobId)
    {
        var organizationId = await _authorizationService
            .ValidateRunnerCanAccessJob<SplitMigrateSaga>(Context, jobId);

        await _splitInitHandler.Complete(jobId, organizationId);
    }

    public async Task SplitInitCancelled(Guid jobId)
    {
        var organizationId = await _authorizationService
            .ValidateRunnerCanAccessJob<SplitMigrateSaga>(Context, jobId);

        await _splitInitHandler.Cancel(jobId, organizationId);
    }

    public async Task SplitInitFaulted(Guid jobId, string? errorMessage, string? stackTrace)
    {
        var organizationId = await _authorizationService
            .ValidateRunnerCanAccessJob<SplitMigrateSaga>(Context, jobId);

        await _splitInitHandler.Fault(jobId, organizationId, errorMessage, stackTrace);
    }

    public async Task SplitValidateCompleted(Guid jobId)
    {
        var organizationId = await _authorizationService
            .ValidateRunnerCanAccessJob<SplitMigrateSaga>(Context, jobId);

        await _splitValidateHandler.Complete(jobId, organizationId);
    }

    public async Task SplitValidateCancelled(Guid jobId)
    {
        var organizationId = await _authorizationService
            .ValidateRunnerCanAccessJob<SplitMigrateSaga>(Context, jobId);

        await _splitValidateHandler.Cancel(jobId, organizationId);
    }

    // Transfer answers on its own endpoints for the setup steps too.

    public async Task TransferGetModuleCompleted(Guid jobId, Guid moduleId)
    {
        var organizationId = await _authorizationService
            .ValidateRunnerCanAccessJob<TransferMigrateSaga>(
                Context, jobId, moduleId);

        await _transferStepHandler.Complete<TransferGetModuleCompleted>(jobId, moduleId, organizationId);
    }

    public async Task TransferGetModuleCancelled(Guid jobId, Guid moduleId)
    {
        var organizationId = await _authorizationService.ValidateRunnerCanAccessJob<TransferMigrateSaga>(
            Context, jobId, moduleId);

        await _transferStepHandler.Cancel<TransferGetModuleCancelled>(jobId, moduleId, organizationId);
    }

    public async Task TransferGetModuleFaulted(Guid jobId, Guid moduleId, string? errorMessage, string? stackTrace)
    {
        var organizationId = await _authorizationService
            .ValidateRunnerCanAccessJob<TransferMigrateSaga>(
                Context, jobId, moduleId);

        await _transferStepHandler.Fault<TransferGetModuleFaulted>(jobId, moduleId, organizationId, errorMessage, stackTrace);
    }

    public async Task TransferInitCompleted(Guid jobId, Guid moduleId)
    {
        var organizationId = await _authorizationService
            .ValidateRunnerCanAccessJob<TransferMigrateSaga>(
                Context, jobId, moduleId);

        await _transferStepHandler.Complete<TransferInitCompleted>(jobId, moduleId, organizationId);
    }

    public async Task TransferInitCancelled(Guid jobId, Guid moduleId)
    {
        var organizationId = await _authorizationService.ValidateRunnerCanAccessJob<TransferMigrateSaga>(
            Context, jobId, moduleId);

        await _transferStepHandler.Cancel<TransferInitCancelled>(jobId, moduleId, organizationId);
    }

    public async Task TransferInitFaulted(Guid jobId, Guid moduleId, string? errorMessage, string? stackTrace)
    {
        var organizationId = await _authorizationService
            .ValidateRunnerCanAccessJob<TransferMigrateSaga>(
                Context, jobId, moduleId);

        await _transferStepHandler.Fault<TransferInitFaulted>(jobId, moduleId, organizationId, errorMessage, stackTrace);
    }

    public async Task TransferValidateCompleted(Guid jobId, Guid moduleId)
    {
        var organizationId = await _authorizationService
            .ValidateRunnerCanAccessJob<TransferMigrateSaga>(
                Context, jobId, moduleId);

        await _transferStepHandler.Complete<TransferValidateCompleted>(jobId, moduleId, organizationId);
    }
}
