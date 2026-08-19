// SPDX-License-Identifier: LicenseRef-Snap-CD-Source-Available-1.1
// Copyright (c) 2026 Karl Schriek / Schrieksoft.
// No license is granted to use this file, in whole or in part, (a) as training, fine-tuning, retrieval, or
// embedding data for any machine-learning model, or (b) as input to any machine-learning model, agent, or automated
// system for the purpose of producing a derivative work or reimplementation that is not otherwise permitted by the
// Snap CD Source-Available License (including any Competing Product as defined therein). Contact info@snapcd.io
// for terms covering either use.


using Microsoft.EntityFrameworkCore;
using SnapCd.Server.Core.Database;
using SnapCd.Server.Core.Entities.Definition;
using SnapCd.Server.Core.Entities.Sagas;
using SnapCd.Server.Core.Enums;
using SnapCd.Server.Core.Misc.Exceptions;
using SnapCd.Server.Core.Repositories.Organizations.Secured;
using SnapCd.Server.Core.Services.PrincipalProvider;

namespace SnapCd.Server.Core.Services.Crud.Jobs;

public class ManualJobServiceFactory(
    IDbContextFactory<SnapCdDbContext> dbFactory,
    ModuleSecuredRepositoryFactory moduleSecuredRepositoryFactory)
{
    public ManualJobService Create(IPrincipalProvider? principalProvider = null)
    {
        return new ManualJobService(dbFactory, moduleSecuredRepositoryFactory.Create(principalProvider));
    }
}

/// <summary>
/// Starts operator-initiated jobs against a paused Module. There is no gatekeeping saga here:
/// a manual job that cannot start is refused rather than queued, so it never fires at an
/// unpredictable later moment.
/// </summary>
public class ManualJobService : IDisposable
{
    private readonly IDbContextFactory<SnapCdDbContext> _dbContextFactory;
    private readonly ModuleSecuredRepository _moduleSecuredRepository;

    public ManualJobService(
        IDbContextFactory<SnapCdDbContext> dbContextFactory,
        ModuleSecuredRepository moduleSecuredRepository)
    {
        _dbContextFactory = dbContextFactory;
        _moduleSecuredRepository = moduleSecuredRepository;
    }

    /// <summary>
    /// Why a manual job cannot start on this Module, or null when it can. Drives the UI's
    /// explanation of a disabled launcher; the launch itself re-checks rather than trusting this.
    /// </summary>
    public async Task<string?> GetBlockedReason(Guid moduleId, Guid organizationId)
    {
        await using var dbContext = await _dbContextFactory.CreateDbContextAsync();

        var saga = await dbContext.Set<ModuleSaga>().AsNoTracking()
            .FirstOrDefaultAsync(s => s.CorrelationId == moduleId && s.OrganizationId == organizationId);

        if (saga is null)
            return "The module has no saga and cannot run manual jobs.";

        if (!saga.Paused)
            return "The module must be paused before a manual job can run.";

        var hasRunningModuleJob = await dbContext.ModuleJobs.AsNoTracking()
            .AnyAsync(j => j.ModuleId == moduleId && j.OrganizationId == organizationId && j.IsCurrent == true);

        if (hasRunningModuleJob)
            return "A job is still finishing on this module. Manual jobs become available once it is quiet.";

        var hasRunningManualJob = await dbContext.ManualModuleJobs.AsNoTracking()
            .AnyAsync(j => j.ModuleId == moduleId && j.OrganizationId == organizationId
                           && j.Status == ExecutionStatus.Running);

        if (hasRunningManualJob)
            return "A manual job is already running on this module.";

        return null;
    }

    public async Task<ManualModuleJob> Start(Guid moduleId, Guid organizationId, string jobType)
    {
        if (!_moduleSecuredRepository.CanPause(moduleId, organizationId))
            throw new PrincipalNotAuthorizedException(
                $"Principal is not allowed to run manual jobs on Module with Id {moduleId}");

        var blocked = await GetBlockedReason(moduleId, organizationId);
        if (blocked is not null)
            throw new ManualJobNotAllowedException(blocked);

        await using var dbContext = await _dbContextFactory.CreateDbContextAsync();

        var job = new ManualModuleJob
        {
            Id = Guid.NewGuid(),
            ModuleId = moduleId,
            OrganizationId = organizationId,
            TimestampStart = DateTimeOffset.UtcNow,
            JobType = jobType,
            Status = ExecutionStatus.Running
        };

        dbContext.ManualModuleJobs.Add(job);

        try
        {
            await dbContext.SaveChangesAsync();
        }
        catch (DbUpdateException)
        {
            // The filtered unique index is the real guarantee: the check above can be raced.
            throw new ManualJobNotAllowedException("A manual job is already running on this module.");
        }

        return job;
    }

    public void Dispose()
    {
        _moduleSecuredRepository?.Dispose();
    }
}
