using Microsoft.EntityFrameworkCore;
using SnapCd.Server.Core.Database;
using SnapCd.Server.Core.Misc.Exceptions;
using SnapCd.Server.Core.Views;

namespace SnapCd.Server.Core.Repositories.Custom.Nonsecured;

public class JobSagaRepositoryFactory(IDbContextFactory<SnapCdDbContext> dbFactory)
{
    public JobSagaRepository Create()
    {
        var dbContext = dbFactory.CreateDbContext();
        var applyJobSagaRepository = new ApplyJobSagaRepository(dbContext);
        var destroyJobSagaRepository = new DestroyJobSagaRepository(dbContext);
        return new JobSagaRepository(dbContext, applyJobSagaRepository, destroyJobSagaRepository);
    }
}

public class JobSagaRepository : IDisposable
{
    private readonly SnapCdDbContext _dbContext;
    private readonly ApplyJobSagaRepository _applyJobSagaRepository;
    private readonly DestroyJobSagaRepository _destroyJobSagaRepository;

    public JobSagaRepository(
        SnapCdDbContext dbContext,
        ApplyJobSagaRepository applyJobSagaRepository,
        DestroyJobSagaRepository destroyJobSagaRepository)
    {
        _dbContext = dbContext;
        _applyJobSagaRepository = applyJobSagaRepository;
        _destroyJobSagaRepository = destroyJobSagaRepository;
    }

    public virtual async Task<JobSagaMetaData> GetSagaMetaData(Guid correlationId, Guid organizationId)
    {
        var metaData = await _applyJobSagaRepository.GetSagaMetaDataOrNull(correlationId, organizationId);
        if (metaData == null)
            metaData = await _destroyJobSagaRepository.GetSagaMetaDataOrNull(correlationId, organizationId);

        if  (metaData == null)
            throw new EntityNotFoundException($"Could not find a Job with correlation id {correlationId} in Organization {organizationId}.");

        return metaData;
    }
    

    public void Dispose()
    {
        _applyJobSagaRepository?.Dispose();
        _destroyJobSagaRepository?.Dispose();
        _dbContext?.Dispose();
    }
}