using Microsoft.EntityFrameworkCore;
using SnapCd.Server.Core.Database;
using SnapCd.Server.Core.Entities.Sagas;

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
}
