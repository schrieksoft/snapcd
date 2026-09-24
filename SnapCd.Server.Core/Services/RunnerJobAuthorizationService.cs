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
using SnapCd.Server.Core.Enums;
using SnapCd.Server.Core.Misc.Constants;
using SnapCd.Server.Core.Misc.Exceptions;
using SnapCd.Server.Core.Misc.Helpers;
using SnapCd.Server.Core.Misc.Helpers.SplitMigrate;
using SnapCd.Server.Core.Repositories.Custom.Nonsecured;
using SnapCd.Server.Core.Repositories.Organizations.Nonsecured;
using SnapCd.Server.Core.Views;

namespace SnapCd.Server.Core.Services;

/// <summary>
/// Provides authorization validation for runner SignalR hub method calls.
/// Ensures runners can only interact with jobs they are authorized to execute.
/// </summary>
public class RunnerJobAuthorizationService
{
    private readonly JobSagaRepositoryFactory _jobSagaRepositoryFactory;
    private readonly SplitMigrateSagaRepositoryFactory _splitMonolithSagaRepositoryFactory;
    private readonly TransferMigrateSagaRepositoryFactory _transferMigrateSagaRepositoryFactory;
    private readonly RunnerConnectionRepositoryFactory _connectionRepositoryFactory;
    private readonly ServicePrincipalRepositoryFactory _servicePrincipalRepositoryFactory;
    private readonly IDbContextFactory<SnapCdDbContext> _dbContextFactory;
    private readonly ILogger<RunnerJobAuthorizationService> _logger;

    public RunnerJobAuthorizationService(
        JobSagaRepositoryFactory jobSagaRepositoryFactory,
        SplitMigrateSagaRepositoryFactory splitMonolithSagaRepositoryFactory,
        TransferMigrateSagaRepositoryFactory transferMigrateSagaRepositoryFactory,
        RunnerConnectionRepositoryFactory connectionRepositoryFactory,
        ServicePrincipalRepositoryFactory servicePrincipalRepositoryFactory,
        IDbContextFactory<SnapCdDbContext> dbContextFactory,
        ILogger<RunnerJobAuthorizationService> logger)
    {
        _jobSagaRepositoryFactory = jobSagaRepositoryFactory;
        _splitMonolithSagaRepositoryFactory = splitMonolithSagaRepositoryFactory;
        _transferMigrateSagaRepositoryFactory = transferMigrateSagaRepositoryFactory;
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

    /// <summary>
    /// Authorizes a runner callback for a job in any saga family. <paramref name="splitTaskEndpoint"/> is
    /// supplied only for the steps a split job shares with a deployment job; without it, a split job's id
    /// is refused, which is what confines the other steps to deployment jobs.
    /// </summary>
    public async Task<JobAuthorization> ValidateRunnerCanAccessJob(
        HubCallerContext hubCallerContext,
        Guid jobId,
        TaskEndpoint taskEndpoint,
        SplitMigrateTaskEndpoint? splitTaskEndpoint = null)
    {
        var expectedState = StateHelper.Lookup(taskEndpoint);

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

        // 2. Load saga metadata using JobSagaRepository (checks both ApplyJobSaga and DestroyJobSaga)
        using var jobSagaRepository = _jobSagaRepositoryFactory.Create();
        JobSagaMetaData sagaMetaData;
        try
        {
            sagaMetaData = await jobSagaRepository.GetSagaMetaData(jobId, connection.OrganizationId);
        }
        catch (EntityNotFoundException e)
        {
            _logger.LogWarning(
                "Authorization failed: Job {JobId} not found (Connection: {ConnectionId})",
                jobId, hubCallerContext.ConnectionId);
            throw new HubException(e.Message);
        }

        // 3. Validate saga state matches expected state, in the resolved family's vocabulary
        if (sagaMetaData.Family == JobSagaFamily.SplitMigrate)
        {
            if (splitTaskEndpoint == null)
            {
                _logger.LogWarning(
                    "Authorization failed: Job {JobId} is a SplitMigrate job, but {TaskEndpoint} is not a step it runs " +
                    "(Connection: {ConnectionId})",
                    jobId, taskEndpoint, hubCallerContext.ConnectionId);
                throw new HubException("Unauthorized: This callback does not apply to this job");
            }

            var splitOrgId = await ValidateRunnerCanAccessSplitMigrateJob(hubCallerContext, jobId, splitTaskEndpoint.Value);
            return new JobAuthorization(JobSagaFamily.SplitMigrate, splitOrgId);
        }

        var currentState = Enum.Parse<ModuleJobSagaState>(sagaMetaData.CurrentState);
        var cancellingStates = StateHelper.GetCancellingStates();

        var isStateValid = currentState == expectedState;

        // If in cancellation state, also check if PreviousStateBeforeCancelling matches expected state
        if (!isStateValid && cancellingStates.Contains(currentState))
        {
            var previousState = !string.IsNullOrEmpty(sagaMetaData.PreviousStateBeforeCancelling)
                ? Enum.Parse<ModuleJobSagaState>(sagaMetaData.PreviousStateBeforeCancelling)
                : (ModuleJobSagaState?)null;

            if (previousState == expectedState)
            {
                isStateValid = true;
                _logger.LogDebug(
                    "Authorization: Job {JobId} is in cancellation state {CurrentState}, but PreviousStateBeforeCancelling " +
                    "matches expected {ExpectedState} - allowing completion message",
                    jobId, currentState, expectedState);
            }
        }

        if (!isStateValid)
        {
            _logger.LogWarning(
                "Authorization failed: Job {JobId} is in state {CurrentState}, expected {ExpectedState} " +
                "(Runner: {RunnerId}/{RunnerName}, Connection: {ConnectionId})",
                jobId, sagaMetaData.CurrentState, expectedState,
                connection.RunnerId, connection.InstanceName, hubCallerContext.ConnectionId);
            throw new HubException(
                $"Unauthorized: Job is in state '{sagaMetaData.CurrentState}', expected '{expectedState}'");
        }

        // 4. Validate RunnerId matches
        if (sagaMetaData.RunnerId != connection.RunnerId)
        {
            _logger.LogWarning(
                "Authorization failed: Job {JobId} requires Runner {RequiredRunnerId}, " +
                "but the selected runner is {SelectedRunnerId} " +
                "(Runner: {RunnerName}, Connection: {ConnectionId})",
                jobId, sagaMetaData.RunnerId, connection.RunnerId,
                connection.InstanceName, hubCallerContext.ConnectionId);
            throw new HubException(
                "Unauthorized: This runner's pool is not authorized for this job");
        }

        // 5. If specific runner is required, validate runner name matches
        if (!string.IsNullOrEmpty(sagaMetaData.RunnerInstanceName) &&
            sagaMetaData.RunnerInstanceName != connection.InstanceName)
        {
            _logger.LogWarning(
                "Authorization failed: Job {JobId} requires specific runner {RequiredRunner}, " +
                "but caller is {ActualRunner} (Pool: {RunnerId}, Connection: {ConnectionId})",
                jobId, sagaMetaData.RunnerInstanceName, connection.InstanceName,
                connection.RunnerId, hubCallerContext.ConnectionId);
            throw new HubException(
                "Unauthorized: This job requires a specific runner");
        }

        // 6. Validate organization matches (defense in depth)
        if (sagaMetaData.OrganizationId != connection.OrganizationId)
        {
            _logger.LogWarning(
                "Authorization failed: Job {JobId} belongs to organization {JobOrgId}, " +
                "but runner is in organization {RunnerOrgId} " +
                "(Runner: {RunnerId}/{RunnerName}, Connection: {ConnectionId})",
                jobId, sagaMetaData.OrganizationId, connection.OrganizationId,
                connection.RunnerId, connection.InstanceName, hubCallerContext.ConnectionId);
            throw new HubException(
                "Unauthorized: Organization mismatch");
        }

        _logger.LogDebug(
            "Authorization succeeded: Runner {RunnerId}/{RunnerName} authorized for job {JobId} in state {State}",
            connection.RunnerId, connection.InstanceName, jobId, expectedState);

        // The transfer family runs the same preamble steps under the same state names, so it
        // validates identically; only the handler the reply goes to differs.
        return new JobAuthorization(sagaMetaData.Family, connection.OrganizationId);
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
    /// The SplitMigrate equivalent of <see cref="ValidateRunnerCanAccessJob"/>. Kept separate
    /// because the deployment path resolves its saga from the apply and destroy tables and parses
    /// the state as a deployment enum, neither of which fits a manual job.
    /// </summary>
    /// <summary>
    /// Authorizes a runner callback for one Module of a transfer job. Two Modules run under one
    /// job, so the Module is named as well, and the runner must be the one that Module was pinned
    /// to rather than any runner on the job.
    /// </summary>
    public async Task<Guid> ValidateRunnerCanAccessTransferJob(
        HubCallerContext hubCallerContext,
        Guid jobId,
        Guid moduleId,
        string task)
    {
        var expectedState = task + "Pending";

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

        using var sagaRepository = _transferMigrateSagaRepositoryFactory.Create();
        JobSagaMetaData sagaMetaData;
        try
        {
            sagaMetaData = await sagaRepository.GetSagaMetaData(jobId, moduleId, connection.OrganizationId);
        }
        catch (EntityNotFoundException e)
        {
            _logger.LogWarning(
                "Authorization failed: Module {ModuleId} of transfer job {JobId} not found (Connection: {ConnectionId})",
                moduleId, jobId, hubCallerContext.ConnectionId);
            throw new HubException(e.Message);
        }

        if (sagaMetaData.CurrentState != expectedState)
        {
            _logger.LogWarning(
                "Authorization failed: Module {ModuleId} of transfer job {JobId} is in state {CurrentState}, expected {ExpectedState} " +
                "(Runner: {RunnerId}/{RunnerName})",
                moduleId, jobId, sagaMetaData.CurrentState, expectedState,
                connection.RunnerId, connection.InstanceName);
            throw new HubException(
                $"Unauthorized: Module is in state '{sagaMetaData.CurrentState}', expected '{expectedState}'");
        }

        if (sagaMetaData.RunnerId != connection.RunnerId)
        {
            _logger.LogWarning(
                "Authorization failed: Module {ModuleId} of transfer job {JobId} requires Runner {RequiredRunnerId}, but the caller is {SelectedRunnerId}",
                moduleId, jobId, sagaMetaData.RunnerId, connection.RunnerId);
            throw new HubException("Unauthorized: This runner's pool is not authorized for this job");
        }

        if (!string.IsNullOrEmpty(sagaMetaData.RunnerInstanceName) &&
            sagaMetaData.RunnerInstanceName != connection.InstanceName)
        {
            _logger.LogWarning(
                "Authorization failed: Module {ModuleId} of transfer job {JobId} requires specific runner {RequiredRunner}, but caller is {ActualRunner}",
                moduleId, jobId, sagaMetaData.RunnerInstanceName, connection.InstanceName);
            throw new HubException("Unauthorized: This job requires a specific runner");
        }

        if (sagaMetaData.OrganizationId != connection.OrganizationId)
        {
            _logger.LogWarning(
                "Authorization failed: Transfer job {JobId} belongs to organization {JobOrgId}, but runner is in {RunnerOrgId}",
                jobId, sagaMetaData.OrganizationId, connection.OrganizationId);
            throw new HubException("Unauthorized: Organization mismatch");
        }

        return sagaMetaData.OrganizationId;
    }

    public async Task<Guid> ValidateRunnerCanAccessSplitMigrateJob(
        HubCallerContext hubCallerContext,
        Guid jobId,
        SplitMigrateTaskEndpoint taskEndpoint)
    {
        var expectedState = SplitMigrateStateHelper.Lookup(taskEndpoint);

        var organizationId = GetValidatedOrganizationId(hubCallerContext);

        using var connectionRepository = _connectionRepositoryFactory.Create();
        var connection = await connectionRepository.GetBySignalRConnectionIdAsync(hubCallerContext.ConnectionId, organizationId);
        if (connection == null)
        {
            _logger.LogWarning(
                "Authorization failed: No connection found for connection {ConnectionId}",
                hubCallerContext.ConnectionId);
            throw new HubException("Unauthorized: Runner connection not found");
        }

        using var sagaRepository = _splitMonolithSagaRepositoryFactory.Create();
        JobSagaMetaData sagaMetaData;
        try
        {
            sagaMetaData = await sagaRepository.GetSagaMetaData(jobId, connection.OrganizationId);
        }
        catch (EntityNotFoundException e)
        {
            _logger.LogWarning(
                "Authorization failed: SplitMigrate job {JobId} not found (Connection: {ConnectionId})",
                jobId, hubCallerContext.ConnectionId);
            throw new HubException(e.Message);
        }

        var currentState = Enum.Parse<SplitMigrateSagaState>(sagaMetaData.CurrentState);
        var isStateValid = currentState == expectedState;

        // A step that reports back mid-cancellation is still the step that was dispatched.
        if (!isStateValid && SplitMigrateStateHelper.GetCancellingStates().Contains(currentState))
        {
            var previousState = !string.IsNullOrEmpty(sagaMetaData.PreviousStateBeforeCancelling)
                ? Enum.Parse<SplitMigrateSagaState>(sagaMetaData.PreviousStateBeforeCancelling)
                : (SplitMigrateSagaState?)null;

            if (previousState == expectedState) isStateValid = true;
        }

        if (!isStateValid)
        {
            _logger.LogWarning(
                "Authorization failed: SplitMigrate job {JobId} is in state {CurrentState}, expected {ExpectedState} " +
                "(Runner: {RunnerId}/{RunnerName}, Connection: {ConnectionId})",
                jobId, sagaMetaData.CurrentState, expectedState,
                connection.RunnerId, connection.InstanceName, hubCallerContext.ConnectionId);
            throw new HubException(
                $"Unauthorized: Job is in state '{sagaMetaData.CurrentState}', expected '{expectedState}'");
        }

        if (sagaMetaData.RunnerId != connection.RunnerId)
        {
            _logger.LogWarning(
                "Authorization failed: SplitMigrate job {JobId} requires Runner {RequiredRunnerId}, but the caller is {SelectedRunnerId}",
                jobId, sagaMetaData.RunnerId, connection.RunnerId);
            throw new HubException("Unauthorized: This runner's pool is not authorized for this job");
        }

        if (!string.IsNullOrEmpty(sagaMetaData.RunnerInstanceName) &&
            sagaMetaData.RunnerInstanceName != connection.InstanceName)
        {
            _logger.LogWarning(
                "Authorization failed: SplitMigrate job {JobId} requires specific runner {RequiredRunner}, but caller is {ActualRunner}",
                jobId, sagaMetaData.RunnerInstanceName, connection.InstanceName);
            throw new HubException("Unauthorized: This job requires a specific runner");
        }

        if (sagaMetaData.OrganizationId != connection.OrganizationId)
        {
            _logger.LogWarning(
                "Authorization failed: SplitMigrate job {JobId} belongs to organization {JobOrgId}, but runner is in {RunnerOrgId}",
                jobId, sagaMetaData.OrganizationId, connection.OrganizationId);
            throw new HubException("Unauthorized: Organization mismatch");
        }

        _logger.LogDebug(
            "Authorization succeeded: Runner {RunnerId}/{RunnerName} authorized for SplitMigrate job {JobId} in state {State}",
            connection.RunnerId, connection.InstanceName, jobId, sagaMetaData.CurrentState);

        return connection.OrganizationId;
    }

}