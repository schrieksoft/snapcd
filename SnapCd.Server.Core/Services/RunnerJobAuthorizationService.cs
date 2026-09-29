// SPDX-License-Identifier: LicenseRef-Snap-CD-Source-Available-1.1
// Copyright (c) 2026 Karl Schriek / Schrieksoft.
// No license is granted to use this file, in whole or in part, (a) as training, fine-tuning, retrieval, or
// embedding data for any machine-learning model, or (b) as input to any machine-learning model, agent, or automated
// system for the purpose of producing a derivative work or reimplementation that is not otherwise permitted by the
// Snap CD Source-Available License (including any Competing Product as defined therein). Contact info@snapcd.io
// for terms covering either use.

using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using SnapCd.Server.Core.Database;
using SnapCd.Server.Core.Entities.Sagas;
using SnapCd.Server.Core.Entities.Sagas.Base;
using SnapCd.Server.Core.Misc.Constants;
using SnapCd.Server.Core.Repositories.Organizations.Nonsecured;

namespace SnapCd.Server.Core.Services;

/// <summary>
/// Provides authorization validation for runner SignalR hub method calls.
/// Ensures runners can only interact with jobs they are authorized to execute.
/// </summary>
public class RunnerJobAuthorizationService
{
    private readonly RunnerConnectionRepositoryFactory _connectionRepositoryFactory;
    private readonly ServicePrincipalRepositoryFactory _servicePrincipalRepositoryFactory;
    private readonly IDbContextFactory<SnapCdDbContext> _dbContextFactory;
    private readonly ILogger<RunnerJobAuthorizationService> _logger;

    public RunnerJobAuthorizationService(
        RunnerConnectionRepositoryFactory connectionRepositoryFactory,
        ServicePrincipalRepositoryFactory servicePrincipalRepositoryFactory,
        IDbContextFactory<SnapCdDbContext> dbContextFactory,
        ILogger<RunnerJobAuthorizationService> logger)
    {
        _connectionRepositoryFactory = connectionRepositoryFactory;
        _servicePrincipalRepositoryFactory = servicePrincipalRepositoryFactory;
        _dbContextFactory = dbContextFactory;
        _logger = logger;
    }


    public Guid GetValidatedOrganizationId(HubCallerContext hubCallerContext)
    {
        var organizationId = hubCallerContext.GetHttpContext()?.Request.Query["organization_id"].ToString().Trim();

        if (string.IsNullOrEmpty(organizationId))
            throw new HubException($"No organization_id query parameter set in HubCallerConext");

        if (!Guid.TryParse(organizationId, out _))
            throw new HubException($"Invalid organization_id format: must be a valid Guid");

        if (hubCallerContext.User == null)
            throw new HubException($"No principal found in HttpContext");

        var organizationsClaim = hubCallerContext.User.Claims.SingleOrDefault(c => c.Type == ClaimTypeConstants.OrganizationClaimType)
            ?.Value;

        if (organizationsClaim == null)
            throw new HubException($"AccessToken does not provide the required \"{ClaimTypeConstants.OrganizationClaimType}\" token");

        var organizations = organizationsClaim.Split(',', StringSplitOptions.RemoveEmptyEntries)
            .Select(org => org.Trim())
            .ToList();

        if (organizations.All(x => x != organizationId))
            throw new HubException($"Calling principal is not a member of organization with id {organizationId}");

        return new Guid(organizationId);
    }

    public async Task<Guid> ValidateIsForCurrentConnection(HubCallerContext hubCallerContext, Guid jobId)
    {
        var organizationId = GetValidatedOrganizationId(hubCallerContext);

        // 1. Get runner connection info from database
        using var connectionRepository = _connectionRepositoryFactory.Create();
        var connection = await connectionRepository.GetBySignalRConnectionIdAsync(hubCallerContext.ConnectionId, organizationId);
        if (connection == null)
        {
            _logger.LogWarning(
                "Authorization failed: No connection found for connection {ConnectionId}",
                hubCallerContext.ConnectionId);
            throw new HubException("Unauthorized: Runner connection not found");
        }

        return organizationId;
    }

    public async Task ValidateRunnerAssignedToModule(
        Guid runnerId,
        Guid moduleId,
        Guid organizationId)
    {
        // Get all ServicePrincipalIds for runners in this pool
        await using var dbContext = await _dbContextFactory.CreateDbContextAsync();
        var servicePrincipalId = dbContext.Runners
            .Where(r => r.Id == runnerId && r.OrganizationId == organizationId)
            .Select(r => r.ServicePrincipalId)
            .SingleOrDefault();

        // Check if any runner in the pool has access to the module
        using var servicePrincipalRepository = _servicePrincipalRepositoryFactory.Create();

        var canRunModule = await servicePrincipalRepository.CanRunModule(
            servicePrincipalId,
            moduleId,
            organizationId);

        if (canRunModule)
        {
            _logger.LogDebug(
                "Validation succeeded: Module {ModuleId} is allowed to run jobs on {RunnerId} " +
                "(via ServicePrincipal {ServicePrincipalId})",
                runnerId, moduleId, servicePrincipalId);
            return; // At least one runner can access the module
        }


        // No runners in the pool have access to this module
        _logger.LogWarning(
            "Validation failed: Module {ModuleId} is not allowed to run jobs on Runner {RunnerId}. You must first assign the Runner to this Module (or to its parent Namespace or Stack), or you must set the IsSuppliedToAllModules flag to 'true' on the Runner itself." +
            "(Organization: {OrganizationId})",
            runnerId, moduleId, organizationId);
        throw new InvalidOperationException(
            $"Module {moduleId} is not allowed to run jobs on Runner {runnerId}. You must first assign the Runner to this Module (or to its parent Namespace or Stack), or you must set the IsSuppliedToAllModules flag to 'true' on the Runner itself.");
    }

    /// <summary>
    /// Authorizes a runner callback that every family answers on the same endpoint, so the family
    /// is not known from the endpoint and each candidate saga is tried in turn.
    /// </summary>
    public async Task<Guid> ValidateRunnerCanCancelJob(
        HubCallerContext hubCallerContext,
        Guid jobId,
        string expectedState)
    {
        foreach (var attempt in new Func<Task<Guid>>[]
                 {
                     () => ValidateRunnerCanAccessJob<ApplyJobSaga>(hubCallerContext, jobId, expectedState),
                     () => ValidateRunnerCanAccessJob<DestroyJobSaga>(hubCallerContext, jobId, expectedState),
                     () => ValidateRunnerCanAccessJob<SplitMigrateSaga>(hubCallerContext, jobId, expectedState)
                 })
        {
            try
            {
                return await attempt();
            }
            catch (HubException)
            {
            }
        }

        _logger.LogWarning(
            "Authorization failed: no job {JobId} in state {ExpectedState} for connection {ConnectionId}",
            jobId, expectedState, hubCallerContext.ConnectionId);
        throw new HubException($"Could not find a Job with correlation id {jobId}.");
    }

    /// <summary>
    /// Authorizes a runner callback for one job family. The endpoint the reply arrived on says
    /// which family it belongs to, so the saga is read from that family's own table rather than
    /// searched for across all of them. A transfer runs two Modules under one job, so the Module
    /// is named as well and the runner must be the one that Module was pinned to.
    /// </summary>
    public async Task<Guid> ValidateRunnerCanAccessJob<TSaga>(
        HubCallerContext hubCallerContext,
        Guid jobId,
        string expectedState,
        Guid? moduleId = null)
        where TSaga : JobSagaBase
    {
        var family = typeof(TSaga).Name;
        var organizationId = GetValidatedOrganizationId(hubCallerContext);

        using var connectionRepository = _connectionRepositoryFactory.Create();
        var connection = await connectionRepository.GetBySignalRConnectionIdAsync(
            hubCallerContext.ConnectionId, organizationId);

        if (connection == null)
        {
            _logger.LogWarning(
                "Authorization failed: No connection found for connection {ConnectionId}",
                hubCallerContext.ConnectionId);
            throw new HubException("Unauthorized: Runner connection not found");
        }

        await using var dbContext = await _dbContextFactory.CreateDbContextAsync();

        var query = dbContext.Set<TSaga>().AsNoTracking()
            .Where(x => x.CorrelationId == jobId && x.OrganizationId == connection.OrganizationId);

        if (moduleId.HasValue)
            query = query.Where(x => x.ModuleId == moduleId.Value);

        var saga = await query
            .Select(x => new
            {
                x.CurrentState, x.PreviousStateBeforeCancelling,
                x.RunnerId, x.RunnerInstanceName, x.OrganizationId
            })
            .FirstOrDefaultAsync();

        if (saga == null)
        {
            _logger.LogWarning(
                "Authorization failed: {Family} job {JobId} not found (Module: {ModuleId}, Connection: {ConnectionId})",
                family, jobId, moduleId, hubCallerContext.ConnectionId);
            throw new HubException($"Could not find a Job with correlation id {jobId}.");
        }

        // A step dispatched before a cancellation arrived still reports back on the state it was
        // dispatched in, so that state is accepted while the saga is cancelling.
        var isStateValid = saga.CurrentState == expectedState
                           || (SagaStates.Cancelling.Contains(saga.CurrentState)
                               && saga.PreviousStateBeforeCancelling == expectedState);

        if (!isStateValid)
        {
            _logger.LogWarning(
                "Authorization failed: {Family} job {JobId} is in state {CurrentState}, expected {ExpectedState} " +
                "(Runner: {RunnerId}/{RunnerName})",
                family, jobId, saga.CurrentState, expectedState,
                connection.RunnerId, connection.InstanceName);
            throw new HubException(
                $"Unauthorized: Job is in state '{saga.CurrentState}', expected '{expectedState}'");
        }

        if (saga.RunnerId != connection.RunnerId)
        {
            _logger.LogWarning(
                "Authorization failed: {Family} job {JobId} requires Runner {RequiredRunnerId}, but the caller is {SelectedRunnerId}",
                family, jobId, saga.RunnerId, connection.RunnerId);
            throw new HubException("Unauthorized: This runner's pool is not authorized for this job");
        }

        if (!string.IsNullOrEmpty(saga.RunnerInstanceName) &&
            saga.RunnerInstanceName != connection.InstanceName)
        {
            _logger.LogWarning(
                "Authorization failed: {Family} job {JobId} requires specific runner {RequiredRunner}, but caller is {ActualRunner}",
                family, jobId, saga.RunnerInstanceName, connection.InstanceName);
            throw new HubException("Unauthorized: This job requires a specific runner");
        }

        _logger.LogDebug(
            "Authorization succeeded: Runner {RunnerId}/{RunnerName} authorized for {Family} job {JobId} in state {State}",
            connection.RunnerId, connection.InstanceName, family, jobId, expectedState);

        return saga.OrganizationId;
    }
}
