using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using SnapCd.Server.Core.Database;
using SnapCd.Server.Core.Enums;
using SnapCd.Server.Core.Misc.Constants;
using SnapCd.Server.Core.Misc.Exceptions;
using SnapCd.Server.Core.Misc.Helpers;
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
    private readonly RunnerConnectionRepositoryFactory _connectionRepositoryFactory;
    private readonly ServicePrincipalRepositoryFactory _servicePrincipalRepositoryFactory;
    private readonly IDbContextFactory<SnapCdDbContext> _dbContextFactory;
    private readonly ILogger<RunnerJobAuthorizationService> _logger;

    public RunnerJobAuthorizationService(
        JobSagaRepositoryFactory jobSagaRepositoryFactory,
        RunnerConnectionRepositoryFactory connectionRepositoryFactory,
        ServicePrincipalRepositoryFactory servicePrincipalRepositoryFactory,
        IDbContextFactory<SnapCdDbContext> dbContextFactory,
        ILogger<RunnerJobAuthorizationService> logger)
    {
        _jobSagaRepositoryFactory = jobSagaRepositoryFactory;
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

    public async Task ValidateRunnerCanAccessJob(
        HubCallerContext hubCallerContext,
        Guid jobId,
        TaskEndpoint taskEndpoint)
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

        // 3. Validate saga state matches expected state
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
            "Validation failed: Module {ModuleId} is not allowed to run jobs on Runner {RunnerId}. You must first assign the Runner to this Module (or to its parent Namespace or Stack), or you must set the IsAssignedToAllModules flag to 'true' on the Runner itself." +
            "(Organization: {OrganizationId})",
            runnerId, moduleId, organizationId);
        throw new InvalidOperationException(
            $"Module {moduleId} is not allowed to run jobs on Runner {runnerId}. You must first assign the Runner to this Module (or to its parent Namespace or Stack), or you must set the IsAssignedToAllModules flag to 'true' on the Runner itself.");
    }
}