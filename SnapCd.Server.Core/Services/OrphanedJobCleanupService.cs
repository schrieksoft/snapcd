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
using SnapCd.Server.Core.Entities.Sagas.Base;

namespace SnapCd.Server.Core.Services;

public class OrphanedJobInfo
{
    public Guid Id { get; set; }
    public Guid OrganizationId { get; set; }
    public string JobType { get; set; } = null!;
}

public class OrphanedJobCleanupService
{
    private readonly IDbContextFactory<SnapCdDbContext> _dbContextFactory;

    public OrphanedJobCleanupService(IDbContextFactory<SnapCdDbContext> dbContextFactory)
    {
        _dbContextFactory = dbContextFactory;
    }

    public async Task<List<OrphanedJobInfo>> ListOrphanedJobs()
    {
        await using var dbContext = await _dbContextFactory.CreateDbContextAsync();

        // Get orphaned Apply jobs (unfinalized with no saga)
        var orphanedApplyJobs = await (
            from job in dbContext.ModuleJobs
            where job.TimestampEnd == null && job.JobType == nameof(ApplyJobSaga)
            join saga in dbContext.Set<ApplyJobSaga>()
                on new { job.Id, job.OrganizationId }
                equals new { Id = saga.CorrelationId, saga.OrganizationId }
                into sagas
            from saga in sagas.DefaultIfEmpty()
            where saga == null
            select new OrphanedJobInfo { Id = job.Id, OrganizationId = job.OrganizationId, JobType = job.JobType }
        ).ToListAsync();

        // Get orphaned Destroy jobs (unfinalized with no saga)
        var orphanedDestroyJobs = await (
            from job in dbContext.ModuleJobs
            where job.TimestampEnd == null && job.JobType == nameof(DestroyJobSaga)
            join saga in dbContext.Set<DestroyJobSaga>()
                on new { job.Id, job.OrganizationId }
                equals new { Id = saga.CorrelationId, saga.OrganizationId }
                into sagas
            from saga in sagas.DefaultIfEmpty()
            where saga == null
            select new OrphanedJobInfo { Id = job.Id, OrganizationId = job.OrganizationId, JobType = job.JobType }
        ).ToListAsync();

        return orphanedApplyJobs.Concat(orphanedDestroyJobs).ToList();
    }

    /// <summary>
    /// Manual jobs left Running with no saga behind them. Worse than an orphaned deployment job,
    /// which merely looks wrong: the filtered unique index keys on Running, so one of these blocks
    /// every future manual job on that Module until it is closed.
    /// </summary>
    public async Task<List<OrphanedJobInfo>> ListOrphanedStateMigrations()
    {
        await using var dbContext = await _dbContextFactory.CreateDbContextAsync();

        // JobType names the saga table the job's state lives in, so each type is checked against
        // its own.
        var orphaned = await OrphansOf<SplitMigrateSaga>(dbContext,
            StateMigrationTypes.SplitMigrate, StateMigrationTypes.SplitProve);

        orphaned.AddRange(await OrphansOf<MoveSaga>(dbContext, StateMigrationTypes.Move));
        orphaned.AddRange(await OrphansOf<ImportSaga>(dbContext, StateMigrationTypes.Import));
        orphaned.AddRange(await OrphansOf<RemoveSaga>(dbContext, StateMigrationTypes.Remove));
        orphaned.AddRange(await OrphansOf<LookupAddressesSaga>(dbContext, StateMigrationTypes.LookupAddresses));

        return orphaned;
    }

    /// <summary>The open jobs of these types whose saga row is gone.</summary>
    private static async Task<List<OrphanedJobInfo>> OrphansOf<TSaga>(
        SnapCdDbContext dbContext, params string[] jobTypes)
        where TSaga : StateMigrationSagaBase =>
        await (
            from job in dbContext.StateMigrationJobs
            where job.TimestampEnd == null && jobTypes.Contains(job.JobType)
            join saga in dbContext.Set<TSaga>()
                on new { job.Id, job.OrganizationId }
                equals new { Id = saga.CorrelationId, saga.OrganizationId }
                into sagas
            from saga in sagas.DefaultIfEmpty()
            where saga == null
            select new OrphanedJobInfo { Id = job.Id, OrganizationId = job.OrganizationId, JobType = job.JobType }
        ).ToListAsync();
}
