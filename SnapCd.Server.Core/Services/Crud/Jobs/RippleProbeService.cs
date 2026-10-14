// SPDX-License-Identifier: LicenseRef-Snap-CD-Source-Available-1.1
// Copyright (c) 2026 Karl Schriek / Schrieksoft.
// No license is granted to use this file, in whole or in part, (a) as training, fine-tuning, retrieval, or
// embedding data for any machine-learning model, or (b) as input to any machine-learning model, agent, or automated
// system for the purpose of producing a derivative work or reimplementation that is not otherwise permitted by the
// Snap CD Source-Available License (including any Competing Product as defined therein). Contact info@snapcd.io
// for terms covering either use.


using MassTransit;
using Microsoft.EntityFrameworkCore;
using SnapCd.Server.Core.Database;
using SnapCd.Server.Core.Entities.Definition;
using SnapCd.Server.Core.Enums;
using SnapCd.Server.Core.Events.Jobs.Module;
using SnapCd.Server.Core.Misc.Exceptions;
using SnapCd.Server.Core.Misc.Helpers;
using SnapCd.Server.Core.Repositories.Organizations.Secured;
using SnapCd.Server.Core.Services.PrincipalProvider;
using SnapCd.Server.Core.Services.ResolvedConfiguration;

namespace SnapCd.Server.Core.Services.Crud.Jobs;

public class RippleProbeServiceFactory(
    IDbContextFactory<SnapCdDbContext> dbFactory,
    ModuleSecuredRepositoryFactory moduleSecuredRepositoryFactory,
    ResolvedConfigurationServiceFactory resolvedConfigurationServiceFactory,
    IBus bus)
{
    public RippleProbeService Create(IPrincipalProvider? principalProvider = null)
    {
        return new RippleProbeService(
            dbFactory,
            moduleSecuredRepositoryFactory.Create(principalProvider),
            resolvedConfigurationServiceFactory.Create(),
            bus);
    }
}

/// <summary>
/// Starts ripple probes: a read-only plan of a Module, run with an upstream Module's changed
/// outputs supplied as the values they will become. Separate from StateMigrationService because
/// nothing here pauses a Module or refuses to run alongside its work.
/// </summary>
public class RippleProbeService : IDisposable
{
    private readonly IDbContextFactory<SnapCdDbContext> _dbContextFactory;
    private readonly ModuleSecuredRepository _moduleSecuredRepository;
    private readonly ResolvedConfigurationService? _resolvedConfigurationService;
    private readonly IBus? _bus;

    public RippleProbeService(
        IDbContextFactory<SnapCdDbContext> dbContextFactory,
        ModuleSecuredRepository moduleSecuredRepository,
        ResolvedConfigurationService? resolvedConfigurationService = null,
        IBus? bus = null)
    {
        _dbContextFactory = dbContextFactory;
        _moduleSecuredRepository = moduleSecuredRepository;
        _resolvedConfigurationService = resolvedConfigurationService;
        _bus = bus;
    }

    /// <summary>
    /// Creates the job row and asks for the plan. The consumer Module is read, not changed, so
    /// read permission on it is the gate; it is never paused and never queued behind its own work.
    /// </summary>
    public async Task<RippleProbeJob> Start(
        Guid moduleId,
        Guid organizationId,
        Guid upstreamModuleId,
        string inputsJson,
        Guid? upstreamModuleJobId = null,
        Guid? optionalCorrelationId = null)
    {
        if (!_moduleSecuredRepository.CanRead(moduleId, organizationId))
            throw new PrincipalNotAuthorizedException(
                $"Principal is not allowed to read Module with Id {moduleId}");

        if (_resolvedConfigurationService is null || _bus is null)
            throw new InvalidOperationException(
                $"{nameof(RippleProbeService)} was constructed without the dependencies needed to start a job.");

        var correlationId = optionalCorrelationId ?? Guid.NewGuid();

        await using var dbContext = await _dbContextFactory.CreateDbContextAsync();

        var job = new RippleProbeJob
        {
            Id = correlationId,
            ModuleId = moduleId,
            OrganizationId = organizationId,
            UpstreamModuleId = upstreamModuleId,
            UpstreamModuleJobId = upstreamModuleJobId,
            TimestampStart = DateTimeOffset.UtcNow,
            Status = ExecutionStatus.Running,
            InputsJson = inputsJson
        };

        dbContext.RippleProbeJobs.Add(job);
        await dbContext.SaveChangesAsync();

        try
        {
            var declared = await _resolvedConfigurationService.GetDeclared(moduleId, organizationId, null);

            await _bus.Publish(new RippleProbeRequested
            {
                CorrelationId = job.Id,
                Declared = declared,
                UpstreamModuleId = upstreamModuleId,
                UpstreamModuleJobId = upstreamModuleJobId,
                InputsJson = inputsJson
            });
        }
        catch (Exception ex)
        {
            // The row is already Running, and no request went out to end it.
            await Fail(job.Id, organizationId, ex.Message);
            throw;
        }

        return job;
    }

    private async Task Fail(Guid id, Guid organizationId, string error)
    {
        await using var dbContext = await _dbContextFactory.CreateDbContextAsync();

        var job = await dbContext.RippleProbeJobs
            .SingleOrDefaultAsync(j => j.Id == id && j.OrganizationId == organizationId);

        if (job is null) return;

        job.Status = ExecutionStatus.Failed;
        job.TimestampEnd = DateTimeOffset.UtcNow;
        job.ServerSideError = ErrorText.Body(error);

        await dbContext.SaveChangesAsync();
    }

    public void Dispose()
    {
        _resolvedConfigurationService?.Dispose();
        _moduleSecuredRepository.Dispose();
    }
}
