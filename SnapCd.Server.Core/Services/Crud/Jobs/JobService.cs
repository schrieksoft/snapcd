// SPDX-License-Identifier: LicenseRef-Snap-CD-Source-Available-1.1
// Copyright (c) 2026 Karl Schriek / Schrieksoft.
// No license is granted to use this file, in whole or in part, (a) as training, fine-tuning, retrieval, or
// embedding data for any machine-learning model, or (b) as input to any machine-learning model, agent, or automated
// system for the purpose of producing a derivative work or reimplementation that is not otherwise permitted by the
// Snap CD Source-Available License (including any Competing Product as defined therein). Contact info@snapcd.io
// for terms covering either use.

using MassTransit;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using SnapCd.Contracts;
using SnapCd.Server.Core.Database;
using SnapCd.Server.Core.Entities.Definition;
using SnapCd.Server.Core.Entities.Sagas;
using SnapCd.Server.Core.Enums;
using SnapCd.Server.Core.Events.Jobs.Base;
using SnapCd.Server.Core.Events.Jobs.Module;
using SnapCd.Server.Core.Events.System;
using SnapCd.Server.Core.Licensing;
using SnapCd.Server.Core.Licensing.Models;
using SnapCd.Server.Core.Licensing.Services;
using SnapCd.Server.Core.Misc.Exceptions;
using SnapCd.Server.Core.Repositories.Organizations.Nonsecured;
using SnapCd.Server.Core.Services.DependencyGraph;
using SnapCd.Server.Core.Services.PrincipalProvider;
using SnapCd.Server.Core.Services.ResolvedConfiguration;
using SnapCd.Server.Core.Services.ResolvedConfiguration.HelperClasses;
using SnapCd.Server.Core.Settings.Repositories;

namespace SnapCd.Server.Core.Services.Crud.Jobs;

public class JobServiceFactory
{
    private readonly IBus _bus;
    private readonly IDbContextFactory<SnapCdDbContext> _dbFactory;
    private readonly ResolvedConfigurationServiceFactory _resolvedConfigurationServiceFactory;
    private readonly DependencyGraphServiceFactory _dependencyServiceFactory;
    private readonly RunnerConnectionRepositoryFactory _connectionRepositoryFactory;
    private readonly IOptions<ModuleJobRepositorySettings> _moduleJobOptions;
    private readonly IOptions<ModuleRepositorySettings> _moduleOptions;
    private readonly IPremiumMessageBrokerPolicy _messageBrokerPolicy;
    private readonly QuotaEnforcementService _quotaEnforcementService;

    public JobServiceFactory(
        IBus bus,
        IDbContextFactory<SnapCdDbContext> dbFactory,
        ResolvedConfigurationServiceFactory resolvedConfigurationServiceFactory,
        DependencyGraphServiceFactory dependencyServiceFactory,
        RunnerConnectionRepositoryFactory connectionRepositoryFactory,
        IOptions<ModuleJobRepositorySettings> moduleJobOptions,
        IOptions<ModuleRepositorySettings> moduleOptions,
        IPremiumMessageBrokerPolicy messageBrokerPolicy,
        QuotaEnforcementService quotaEnforcementService
    )
    {
        _bus = bus;
        _dbFactory = dbFactory;
        _resolvedConfigurationServiceFactory = resolvedConfigurationServiceFactory;
        _dependencyServiceFactory = dependencyServiceFactory;
        _connectionRepositoryFactory = connectionRepositoryFactory;
        _moduleJobOptions = moduleJobOptions;
        _moduleOptions = moduleOptions;
        _messageBrokerPolicy = messageBrokerPolicy;
        _quotaEnforcementService = quotaEnforcementService;
    }

    public JobService Create(IPrincipalProvider? principalProvider = null)
    {
        if (principalProvider == null)
            principalProvider = new HttpContextPrincipalProvider(new HttpContextAccessor());

        // Create a single shared DbContext
        var dbContext = _dbFactory.CreateDbContext();

        var resolvedConfigurationService = _resolvedConfigurationServiceFactory.Create();
        var moduleJobRepository = new ModuleJobRepository(dbContext, principalProvider, _bus, _moduleJobOptions);
        var moduleRepository = new ModuleRepository(dbContext, principalProvider, _bus, _moduleOptions);
        var dependencyService = _dependencyServiceFactory.Create();

        return new JobService(_bus, dbContext, resolvedConfigurationService, moduleJobRepository, moduleRepository, dependencyService, _dbFactory, _connectionRepositoryFactory, _messageBrokerPolicy, _quotaEnforcementService);
    }
}

public class JobService : IDisposable
{
    private readonly IBus _bus;
    private readonly SnapCdDbContext _dbContext;
    private readonly ResolvedConfigurationService _resolvedConfigurationService;
    private readonly ModuleJobRepository _moduleJobRepository;
    private readonly ModuleRepository _moduleRepository;
    private readonly DependencyGraphService _dependencyService;
    private readonly IDbContextFactory<SnapCdDbContext> _dbContextFactory;
    private readonly RunnerConnectionRepositoryFactory _connectionRepositoryFactory;
    private readonly IPremiumMessageBrokerPolicy _messageBrokerPolicy;
    private readonly QuotaEnforcementService _quotaEnforcementService;

    public JobService(
        IBus bus,
        SnapCdDbContext dbContext,
        ResolvedConfigurationService resolvedConfigurationService,
        ModuleJobRepository moduleJobRepository,
        ModuleRepository moduleRepository,
        DependencyGraphService dependencyService,
        IDbContextFactory<SnapCdDbContext> dbContextFactory,
        RunnerConnectionRepositoryFactory connectionRepositoryFactory,
        IPremiumMessageBrokerPolicy messageBrokerPolicy,
        QuotaEnforcementService quotaEnforcementService
    )
    {
        _bus = bus;
        _dbContext = dbContext;
        _resolvedConfigurationService = resolvedConfigurationService;
        _moduleJobRepository = moduleJobRepository;
        _moduleRepository = moduleRepository;
        _dependencyService = dependencyService;
        _dbContextFactory = dbContextFactory;
        _connectionRepositoryFactory = connectionRepositoryFactory;
        _messageBrokerPolicy = messageBrokerPolicy;
        _quotaEnforcementService = quotaEnforcementService;
    }

    public void Dispose()
    {
        _resolvedConfigurationService.Dispose();
        _dbContext.Dispose();
        _dependencyService.Dispose();
    }


    public async Task Cancel(Guid jobId, Guid organizationId, CancellationType cancellationType)
    {
        await _bus.Publish(new CancelModuleRequested
        {
            CorrelationId = jobId,
            OrganizationId = organizationId,
            CancellationType = cancellationType
        });
    }

    public async Task Apply(Guid moduleId, Guid organizationId, Guid? optionalCorrelationId = null, string? runnerInstanceNameOverride = null)
    {
        if (organizationId == Guid.Empty)
            throw new ArgumentException("OrganizationId cannot be empty", nameof(organizationId));

        var correlationId = optionalCorrelationId ?? Guid.NewGuid();
        try
        {
            await EnforceLicenceAndQuota(organizationId);

            var declared = await _resolvedConfigurationService.GetDeclared(moduleId, organizationId);

            // Override runner name if provided
            if (!string.IsNullOrEmpty(runnerInstanceNameOverride)) declared.RunnerInstanceName = runnerInstanceNameOverride;

            // Create the ModuleJob first
            await CreateModuleJob(correlationId, moduleId, organizationId, nameof(ApplyJobSaga));

            // Then publish the event
            await _bus.Publish(BuildEvent<ApplyModuleRequested>(declared, correlationId));
        }
        catch (Exception ex)
        {
            await CreateFailedJob(correlationId, moduleId, organizationId, ex.Message, nameof(ApplyJobSaga));
        }
    }

    public async Task Destroy(Guid moduleId, Guid organizationId, Guid? optionalCorrelationId = null, string? runnerInstanceNameOverride = null)
    {
        if (organizationId == Guid.Empty)
            throw new ArgumentException("OrganizationId cannot be empty", nameof(organizationId));

        var correlationId = optionalCorrelationId ?? Guid.NewGuid();
        try
        {
            await EnforceLicenceAndQuota(organizationId);

            var declared = await _resolvedConfigurationService.GetDeclared(moduleId, organizationId);

            // Override runner name if provided
            if (!string.IsNullOrEmpty(runnerInstanceNameOverride)) declared.RunnerInstanceName = runnerInstanceNameOverride;

            // Create the ModuleJob first
            await CreateModuleJob(correlationId, moduleId, organizationId, nameof(DestroyJobSaga));

            // Then publish the event
            await _bus.Publish(BuildEvent<DestroyModuleRequested>(declared, correlationId));
        }
        catch (Exception ex)
        {
            await CreateFailedJob(correlationId, moduleId, organizationId, ex.Message, nameof(DestroyJobSaga));
        }
    }

    public async Task RunQueued(Guid moduleId, Guid organizationId)
    {
        await _bus.Publish(new RunQueueNowRequested
        {
            ModuleId = moduleId,
            OrganizationId = organizationId
        });
    }

    public async Task ClearQueue(Guid moduleId, Guid organizationId)
    {
        await _bus.Publish(new ClearQueueRequested
        {
            ModuleId = moduleId,
            OrganizationId = organizationId
        });
    }

    public async Task<bool> CheckDependenciesAsync(Guid moduleId, Guid organizationId, DesiredStateHeadline desiredState)
    {
        // Get module dependency settings
        await using var dbContext = await _dbContextFactory.CreateDbContextAsync();
        var moduleSettings = await dbContext.Modules
            .Where(m => m.Id == moduleId)
            .Select(x => new
            {
                x.WaitForApplyDependencies,
                x.WaitForDestroyDependencies
            })
            .SingleAsync();

        // Determine which BlockOn setting to use based on desired state
        var shouldCheckDependencies = desiredState switch
        {
            DesiredStateHeadline.Applied => await ShouldCheckApplyDependencies(moduleSettings.WaitForApplyDependencies, moduleId, organizationId),
            DesiredStateHeadline.Destroyed => ShouldCheckDestroyDependencies(moduleSettings.WaitForDestroyDependencies),
            _ => false
        };

        if (!shouldCheckDependencies)
            return true;

        // Get dependencies and check their states
        switch (desiredState)
        {
            case DesiredStateHeadline.Applied:
                return await CheckApplyDependenciesAsync(moduleId);
            case DesiredStateHeadline.Destroyed:
                return await CheckDestroyDependenciesAsync(moduleId);
            default:
                return true;
        }
    }

    /// <summary>
    /// Checks if a runner is available for the specified module by querying the database.
    /// A runner is considered available if it's currently connected (has an active connection).
    /// </summary>
    public async Task<bool> CheckRunnerAvailabilityAsync(Guid moduleId)
    {
        try
        {
            // Get the module to access organizationId and runner info
            var module = await _dbContext.Modules.FirstOrDefaultAsync(m => m.Id == moduleId);
            if (module == null) return false;

            // Get runner selection (which runner and optionally which specific instance)
            var runnerSelection = await _moduleRepository.GetRunnerSelection(moduleId, module.OrganizationId);

            // Look up the RunnerId from the runner name
            var runnerId = await _dbContext.Runners
                .Where(rp => rp.Name == runnerSelection.RunnerName && rp.OrganizationId == module.OrganizationId)
                .Select(rp => rp.Id)
                .FirstOrDefaultAsync();

            if (runnerId == Guid.Empty) return false;

            using var connectionRepository = _connectionRepositoryFactory.Create();

            // If a specific runner instance is declared, check if that specific instance is available
            if (!string.IsNullOrEmpty(runnerSelection.RunnerInstanceName))
            {
                var connection = await connectionRepository.GetActiveConnection(
                    module.OrganizationId,
                    runnerId,
                    runnerSelection.RunnerInstanceName
                );

                return connection != null; // Connection exists in database = connected/available
            }

            // No specific runner instance declared - check if ANY instance of this runner is available
            var connections = await connectionRepository.GetActiveConnectionsByRunnerId(runnerId, module.OrganizationId);
            return connections.Count > 0; // At least one instance is connected
        }
        catch (Exception)
        {
            // On error, assume no runners available
            return false;
        }
    }

    private async Task EnforceLicenceAndQuota(Guid organizationId)
    {
        if (!await _messageBrokerPolicy.IsAllowedAsync())
            throw new LicenceFeatureUnavailableException(Feature.PremiumMessageBroker,
                "Cannot create new jobs: configured message-broker backend requires the PremiumMessageBroker feature. " +
                "Either set ServiceBus:BusType=SqlServer or upgrade to a Lite/Enterprise licence.");

        var (canCreate, reason) = await _quotaEnforcementService.CanCreateModuleJobAsync(organizationId);
        if (!canCreate)
            throw new QuotaExceededException("Module", 0, 0, reason!);
    }

    private async Task<bool> ShouldCheckApplyDependencies(WaitForApplyDependencies waitForApplyDependencies, Guid moduleId, Guid organizationId)
    {
        return waitForApplyDependencies switch
        {
            WaitForApplyDependencies.Always => true,
            WaitForApplyDependencies.Never => false,
            WaitForApplyDependencies.OnFirstApply => await IsFirstApplyAttempt(moduleId, organizationId),
            _ => false
        };
    }

    private static bool ShouldCheckDestroyDependencies(WaitForDestroyDependencies waitForDestroyDependencies)
    {
        return waitForDestroyDependencies switch
        {
            WaitForDestroyDependencies.Always => true,
            WaitForDestroyDependencies.Never => false,
            _ => false
        };
    }

    private async Task<bool> IsFirstApplyAttempt(Guid moduleId, Guid organizationId)
    {
        return await _moduleJobRepository.IsFirstApply(moduleId, organizationId);
    }

    private async Task<bool> CheckApplyDependenciesAsync(Guid moduleId)
    {
        var dependencies = await _dependencyService.ListForDefinedModule(moduleId);

        // vw_Dependencies emits a placeholder row with a NULL referenced module for
        // standalone modules so they still appear in the dependency graph; only rows
        // with an actual referenced module are real dependencies.
        if (dependencies.Any(x => x.ReferencedModuleId != null && x.ReferencedLatestActualState != ActualStateHeadline.Applied))
            return false;

        return true;
    }

    private async Task<bool> CheckDestroyDependenciesAsync(Guid moduleId)
    {
        var dependencies = await _dependencyService.ListForReferencedModule(moduleId);

        if (dependencies.Any(x => x.DefinedLatestActualState != ActualStateHeadline.Destroyed))
            return false;

        return true;
    }

    private async Task CreateModuleJob(Guid correlationId, Guid moduleId, Guid organizationId, string jobType, bool runInTransaction = false)
    {
        // First, unset any existing current job for this module
        var existingCurrentJob = await _dbContext.ModuleJobs
            .Where(j => j.ModuleId == moduleId && j.OrganizationId == organizationId && j.IsCurrent == true)
            .FirstOrDefaultAsync();

        if (existingCurrentJob != null) existingCurrentJob.IsCurrent = false;

        if (runInTransaction)
            await _moduleJobRepository.Create(new ModuleJob
            {
                Id = correlationId,
                ModuleId = moduleId,
                OrganizationId = organizationId,
                TimestampStart = DateTimeOffset.UtcNow,
                JobType = jobType,
                Status = ExecutionStatus.Running,
                IsCurrent = true
            });
        else
            await _moduleJobRepository.ExecuteCreate(new ModuleJob
            {
                Id = correlationId,
                ModuleId = moduleId,
                OrganizationId = organizationId,
                TimestampStart = DateTimeOffset.UtcNow,
                JobType = jobType,
                Status = ExecutionStatus.Running,
                IsCurrent = true
            });
    }

    public async Task CreateFailedJob(Guid correlationId, Guid moduleId, Guid organizationId, string errorMessage, string jobType)
    {
        // First, unset any existing current job for this module
        var existingCurrentJob = await _dbContext.ModuleJobs
            .Where(j => j.ModuleId == moduleId && j.OrganizationId == organizationId && j.IsCurrent == true)
            .FirstOrDefaultAsync();

        if (existingCurrentJob != null) existingCurrentJob.IsCurrent = false;

        var timeStamp = DateTimeOffset.UtcNow;
        await _moduleJobRepository.ExecuteCreate(new ModuleJob
            {
                Id = correlationId,
                ModuleId = moduleId,
                OrganizationId = organizationId,
                TimestampStart = timeStamp,
                TimestampEnd = timeStamp,
                ServerSideError = errorMessage,
                ServerSideErrorHeader = "This job failed due to a error occuring on the Server. The full error can be seen below.",
                FailedOnServerSideStep = ServerSideStep.Start,
                JobType = jobType,
                Status = ExecutionStatus.Failed,
                IsCurrent = false // Failed jobs shouldn't be current
            }
        );
    }


    public async Task CreateServerSideFailure(Guid correlationId, Guid moduleId, string errorMessage, ServerSideStep failedOnServerSideStep)
    {
        // First, unset any existing current job for this module
        var existingJob = await _dbContext.ModuleJobs
            .Where(j => j.Id == correlationId)
            .FirstOrDefaultAsync();

        if (existingJob != null)
        {
            var timeStamp = DateTimeOffset.UtcNow;
            existingJob.ServerSideError = errorMessage;
            existingJob.ServerSideErrorHeader = "This job failed due to a error occuring on the Server. The full error can be seen below.";
            existingJob.FailedOnServerSideStep = failedOnServerSideStep;
            existingJob.IsCurrent = false;
            existingJob.Status = ExecutionStatus.Failed;
            existingJob.TimestampEnd = timeStamp;

            await _dbContext.SaveChangesAsync();
            
            await _moduleJobRepository.ExecuteUpdate(existingJob);
        }
    }

    /// <summary>
    /// Triggers re-evaluation of queued jobs waiting for runner availability.
    /// Called when a runner connects to the system.
    /// </summary>
    public async Task TriggerQueuedJobs(Guid runnerId, string runnerName)
    {
        // Find modules that are queued waiting for a runner checkin for this specific Runner/name combination
        var queuedModules = await _dbContext.ModuleSagas
            .Include(ms => ms.Module)
            .Where(ms => ms.QueuedDesiredStateHeadline != null
                         && ms.QueuedReason == QueuedReason.WaitingOnRunnerCheckin
                         && ms.Module.RunnerId == runnerId
                         && (ms.Module.RunnerInstanceName == null || ms.Module.RunnerInstanceName == runnerName))
            .Select(x => new { x.CorrelationId, x.OrganizationId })
            .ToListAsync();

        foreach (var module in queuedModules)
            // Publish ModuleDependencyCheckRequested to trigger re-evaluation
            await _bus.Publish(new ModuleDependencyCheckRequested
            {
                ModuleId = module.CorrelationId,
                OrganizationId = module.OrganizationId
            });
    }

    private TEvent BuildEvent<TEvent>(ResolvedModule declared, Guid correlationId)
        where TEvent : ModuleJobEventBase, new()
    {
        return new TEvent
        {
            CorrelationId = correlationId,
            Declared = declared
        };
    }
}