// SPDX-License-Identifier: LicenseRef-Snap-CD-Source-Available-1.1
// Copyright (c) 2026 Karl Schriek / Schrieksoft.
// No license is granted to use this file, in whole or in part, (a) as training, fine-tuning, retrieval, or
// embedding data for any machine-learning model, or (b) as input to any machine-learning model, agent, or automated
// system for the purpose of producing a derivative work or reimplementation that is not otherwise permitted by the
// Snap CD Source-Available License (including any Competing Product as defined therein). Contact info@snapcd.io
// for terms covering either use.

using Microsoft.AspNetCore.SignalR;
using SnapCd.Contracts.Constants;
using Microsoft.EntityFrameworkCore;
using SnapCd.Server.Core.Database;
using SnapCd.Server.Core.Entities.Sagas;
using SnapCd.Server.Core.Entities.Sagas.Base;
using SnapCd.Server.Core.Enums;
using SnapCd.Server.Core.Misc.Constants;
using SnapCd.Server.Core.Misc.Helpers;
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
    private readonly SnapCdDbContext _dbContext;
    private readonly ILogger<RunnerJobAuthorizationService> _logger;

    public RunnerJobAuthorizationService(
        RunnerConnectionRepositoryFactory connectionRepositoryFactory,
        ServicePrincipalRepositoryFactory servicePrincipalRepositoryFactory,
        SnapCdDbContext dbContext,
        ILogger<RunnerJobAuthorizationService> logger)
    {
        _connectionRepositoryFactory = connectionRepositoryFactory;
        _servicePrincipalRepositoryFactory = servicePrincipalRepositoryFactory;
        _dbContext = dbContext;
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

    /// <summary>
    /// A connection with no registration can never be authorized, so it is closed: the runner reconnects,
    /// registers again and repeats the call.
    /// </summary>
    private HubException ConnectionNotRegistered(HubCallerContext hubCallerContext)
    {
        _logger.LogWarning(
            "No connection record for connection {ConnectionId}; closing it so the runner reconnects and registers",
            hubCallerContext.ConnectionId);
        hubCallerContext.Abort();
        return new HubException($"{HubErrorCodes.ConnectionNotRegistered}: Runner connection not found");
    }

    public async Task<Guid> ValidateIsForCurrentConnection(HubCallerContext hubCallerContext, Guid jobId)
    {
        var organizationId = GetValidatedOrganizationId(hubCallerContext);

        // 1. Get runner connection info from database
        using var connectionRepository = _connectionRepositoryFactory.Create();
        var connection = await connectionRepository.GetBySignalRConnectionIdAsync(hubCallerContext.ConnectionId, organizationId);
        if (connection == null)
        {
            throw ConnectionNotRegistered(hubCallerContext);
        }

        return organizationId;
    }

    public async Task ValidateRunnerAssignedToModule(
        Guid runnerId,
        Guid moduleId,
        Guid organizationId)
    {
        // Get all ServicePrincipalIds for runners in this pool
        var servicePrincipalId = _dbContext.Runners
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

    private const string LateReportNote = "The runner reported on this job after the server had ended it";

    /// <summary>
    /// Notes on an ended job that its runner was still working on it, so a result the server can no longer accept is not
    /// silently lost. Only the runner the job's module runs on can leave the note, and only once.
    /// </summary>
    public static async Task RecordLateReport(SnapCdDbContext _dbContext, Guid jobId, Guid organizationId, Guid runnerId)
    {
        var note = $"{LateReportNote}, at {DateTimeOffset.UtcNow:u}. Its result was not recorded; check the runner's logs for what it did.";

        var moduleJob = await _dbContext.ModuleJobs
            .Where(j => j.Id == jobId && j.OrganizationId == organizationId && j.Status != ExecutionStatus.Running && j.Module.RunnerId == runnerId)
            .FirstOrDefaultAsync();
        if (moduleJob != null)
        {
            if (moduleJob.ServerSideError?.Contains(LateReportNote) == true) return;
            moduleJob.ServerSideErrorHeader ??= LateReportNote;
            moduleJob.ServerSideError = ErrorText.Body(string.IsNullOrEmpty(moduleJob.ServerSideError) ? note : $"{moduleJob.ServerSideError}\n\n{note}");
            await _dbContext.SaveChangesAsync();
            return;
        }

        var stateMigrationJob = await _dbContext.StateMigrationJobs
            .Where(j => j.Id == jobId && j.OrganizationId == organizationId && j.Status != ExecutionStatus.Running && j.Module.RunnerId == runnerId)
            .FirstOrDefaultAsync();
        if (stateMigrationJob == null || stateMigrationJob.ServerSideError?.Contains(LateReportNote) == true) return;
        stateMigrationJob.ServerSideErrorHeader ??= LateReportNote;
        stateMigrationJob.ServerSideError = ErrorText.Body(string.IsNullOrEmpty(stateMigrationJob.ServerSideError) ? note : $"{stateMigrationJob.ServerSideError}\n\n{note}");
        await _dbContext.SaveChangesAsync();
    }

    /// <summary>
    /// Authorizes a runner callback for one job family: that the connection is known, the job
    /// exists, and the caller is the runner instance the job was pinned to. Whether the saga is in
    /// a state that accepts the reply is the saga's own decision, raised as an unhandled event and
    /// retried by the endpoint, so it is not answered here.
    ///
    /// The endpoint the reply arrived on says which family it belongs to, so the saga is read from
    /// that family's own table rather than searched for across all of them. A transfer runs two
    /// Modules under one job, so the Module is named as well.
    /// </summary>
    public async Task<Guid> ValidateRunnerCanAccessJob<TSaga>(
        HubCallerContext hubCallerContext,
        Guid jobId,
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
            throw ConnectionNotRegistered(hubCallerContext);
        }


        var query = _dbContext.Set<TSaga>().AsNoTracking()
            .Where(x => x.CorrelationId == jobId && x.OrganizationId == connection.OrganizationId);

        if (moduleId.HasValue)
            query = query.Where(x => x.ModuleId == moduleId.Value);

        var saga = await query
            .Select(x => new { x.RunnerId, x.RunnerInstanceName, x.OrganizationId })
            .FirstOrDefaultAsync();

        if (saga == null)
        {
            _logger.LogWarning(
                "Authorization failed: {Family} job {JobId} not found (Module: {ModuleId}, Connection: {ConnectionId})",
                family, jobId, moduleId, hubCallerContext.ConnectionId);
            await RecordLateReport(_dbContext, jobId, connection.OrganizationId, connection.RunnerId);
            throw new HubException($"Could not find a Job with correlation id {jobId}.");
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
            "Authorization succeeded: Runner {RunnerId}/{RunnerName} authorized for {Family} job {JobId}",
            connection.RunnerId, connection.InstanceName, family, jobId);

        return saga.OrganizationId;
    }
}
