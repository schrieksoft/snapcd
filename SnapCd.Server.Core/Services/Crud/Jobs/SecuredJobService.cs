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
using SnapCd.Server.Core.Events.Gatekeeping;
using SnapCd.Server.Core.Licensing;
using SnapCd.Server.Core.Licensing.Models;
using SnapCd.Server.Core.Licensing.Services;
using SnapCd.Server.Core.Misc.Exceptions;
using SnapCd.Server.Core.Repositories.Organizations.Nonsecured;
using SnapCd.Server.Core.Repositories.Organizations.Secured;
using SnapCd.Server.Core.Services.PrincipalProvider;
using SnapCd.Server.Core.Settings.Repositories;

namespace SnapCd.Server.Core.Services.Crud.Jobs;

public class ModuleNamespaceIdTuple
{
    public Guid ModuleId { get; set; }
    public Guid NamespaceId { get; set; }
}

public class RunJobPermission
{
    public Guid ModuleId { get; set; }
    public Guid NamespaceId { get; set; }
    public bool HasPermission { get; set; }
}

public class SecuredJobServiceFactory
{
    private readonly JobServiceFactory _jobServiceFactory;
    private readonly IDbContextFactory<SnapCdDbContext> _dbContextFactory;
    private readonly IBus _bus;
    private readonly IOptions<ModuleRepositorySettings> _moduleOptions;
    private readonly IOptions<ModuleJobRepositorySettings> _moduleJobOptions;
    private readonly QuotaEnforcementService _quotaEnforcementService;
    private readonly IPremiumMessageBrokerPolicy _messageBrokerPolicy;


    public SecuredJobServiceFactory(
        JobServiceFactory jobServiceFactory,
        IDbContextFactory<SnapCdDbContext> dbContextFactory,
        IBus bus,
        IOptions<ModuleRepositorySettings> moduleOptions,
        IOptions<ModuleJobRepositorySettings> moduleJobOptions,
        QuotaEnforcementService quotaEnforcementService,
        IPremiumMessageBrokerPolicy messageBrokerPolicy)
    {
        _jobServiceFactory = jobServiceFactory;
        _dbContextFactory = dbContextFactory;
        _bus = bus;
        _moduleOptions = moduleOptions;
        _moduleJobOptions = moduleJobOptions;
        _quotaEnforcementService = quotaEnforcementService;
        _messageBrokerPolicy = messageBrokerPolicy;
    }

    public SecuredJobService Create(IPrincipalProvider? principalProvider = null)
    {
        if (principalProvider == null)
            principalProvider = new HttpContextPrincipalProvider(new HttpContextAccessor());

        // Create a single shared DbContext
        var dbContext = _dbContextFactory.CreateDbContext();

        var jobService = _jobServiceFactory.Create(principalProvider);
        var moduleRepository = new ModuleRepository(dbContext, principalProvider, _bus, _moduleOptions);
        var moduleJobRepository = new ModuleJobRepository(dbContext, principalProvider, _bus, _moduleJobOptions);
        var moduleJobSecuredRepository = new ModuleJobSecuredRepository(moduleJobRepository, principalProvider);

        return new SecuredJobService(jobService, moduleRepository, moduleJobRepository, moduleJobSecuredRepository, _bus, _quotaEnforcementService, _messageBrokerPolicy);
    }
}

public class SecuredJobService : IDisposable
{
    private readonly JobService _jobService;
    private readonly ModuleJobRepository _moduleJobRepository;
    private readonly ModuleRepository _moduleRepository;
    private readonly ModuleJobSecuredRepository _moduleJobSecuredRepository;
    private readonly IBus _bus;
    private readonly QuotaEnforcementService _quotaEnforcementService;
    private readonly IPremiumMessageBrokerPolicy _messageBrokerPolicy;

    public SecuredJobService(
        JobService jobService,
        ModuleRepository moduleRepository,
        ModuleJobRepository moduleJobRepository,
        ModuleJobSecuredRepository moduleJobSecuredRepository,
        IBus bus,
        QuotaEnforcementService quotaEnforcementService,
        IPremiumMessageBrokerPolicy messageBrokerPolicy)
    {
        _jobService = jobService;
        _moduleJobRepository = moduleJobRepository;
        _moduleJobSecuredRepository = moduleJobSecuredRepository;
        _moduleRepository = moduleRepository;
        _bus = bus;
        _quotaEnforcementService = quotaEnforcementService;
        _messageBrokerPolicy = messageBrokerPolicy;
    }

    public async Task Apply(Guid moduleId, Guid organizationId, Guid jobId, string? runnerInstanceNameOverride = null)
    {
        if (!_moduleJobSecuredRepository.CanRunJob(moduleId, organizationId))
            throw new PrincipalNotAuthorizedException($"Principal is not allowed to run Jobs on Module with Id {moduleId}");

        // Check module quota - block jobs if organization exceeds module limit
        var (canCreate, reason) = await _quotaEnforcementService.CanCreateModuleJobAsync(organizationId);
        if (!canCreate)
            throw new QuotaExceededException("Module", 0, 0, reason!);

        if (!await _messageBrokerPolicy.IsAllowedAsync())
            throw new LicenceFeatureUnavailableException(Feature.PremiumMessageBroker,
                "Cannot create new Apply jobs: configured message-broker backend requires the PremiumMessageBroker feature. " +
                "Either set ServiceBus:BusType=SqlServer or upgrade to a Lite/Enterprise licence.");

        await _bus.Publish(new GatekeepingJobRequested
        {
            ModuleId = moduleId,
            OrganizationId = organizationId,
            DesiredStateHeadline = DesiredStateHeadline.Applied,
            SetNewDesiredState = true,
            JobId = jobId,
            RunnerInstanceNameOverride = runnerInstanceNameOverride
        }, publishContext => { publishContext.TimeToLive = TimeSpan.FromMinutes(5); });
    }

    public async Task Destroy(Guid moduleId, Guid organizationId, Guid jobId, string? runnerInstanceNameOverride = null)
    {
        if (!_moduleJobSecuredRepository.CanRunJob(moduleId, organizationId))
            throw new PrincipalNotAuthorizedException($"Principal is not allowed to run Jobs on Module with Id {moduleId}");

        // Check module quota - block jobs if organization exceeds module limit
        var (canCreate, reason) = await _quotaEnforcementService.CanCreateModuleJobAsync(organizationId);
        if (!canCreate)
            throw new QuotaExceededException("Module", 0, 0, reason!);

        if (!await _messageBrokerPolicy.IsAllowedAsync())
            throw new LicenceFeatureUnavailableException(Feature.PremiumMessageBroker,
                "Cannot create new Destroy jobs: configured message-broker backend requires the PremiumMessageBroker feature. " +
                "Either set ServiceBus:BusType=SqlServer or upgrade to a Lite/Enterprise licence.");

        await _bus.Publish(new GatekeepingJobRequested
        {
            ModuleId = moduleId,
            OrganizationId = organizationId,
            DesiredStateHeadline = DesiredStateHeadline.Destroyed,
            SetNewDesiredState = true,
            JobId = jobId,
            RunnerInstanceNameOverride = runnerInstanceNameOverride
        }, publishContext => { publishContext.TimeToLive = TimeSpan.FromMinutes(5); });
    }

    public async Task RunQueued(Guid moduleId, Guid namespaceId, Guid organizationId)
    {
        if (!_moduleJobSecuredRepository.CanRunJob(moduleId, organizationId))
            throw new PrincipalNotAuthorizedException($"Principal is not allowed to manage Jobs on Module with Id {moduleId}");
        await _jobService.RunQueued(moduleId, organizationId);
    }

    public async Task ClearQueue(Guid moduleId, Guid namespaceId, Guid organizationId)
    {
        if (!_moduleJobSecuredRepository.CanRunJob(moduleId, organizationId))
            throw new PrincipalNotAuthorizedException($"Principal is not allowed to manage Jobs on Module with Id {moduleId}");
        await _jobService.ClearQueue(moduleId, organizationId);
    }

    public async Task Cancel(Guid jobId, Guid moduleId, Guid namespaceId, Guid organizationId, CancellationType cancellationType)
    {
        if (!_moduleJobSecuredRepository.CanRunJob(moduleId, organizationId))
            throw new PrincipalNotAuthorizedException($"Principal is not allowed to manage Jobs on Module with Id {moduleId}");
        await _jobService.Cancel(jobId, organizationId, cancellationType);
    }

    public void Dispose()
    {
        _jobService?.Dispose();
        _moduleJobRepository?.Dispose();
        _moduleRepository?.Dispose();
    }
}