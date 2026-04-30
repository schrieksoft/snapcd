using Microsoft.EntityFrameworkCore;
using SnapCd.Server.Core.Database;
using SnapCd.Server.Core.Entities.Sagas;
using SnapCd.Server.Core.Misc.Exceptions;
using SnapCd.Server.Core.Views;

namespace SnapCd.Server.Core.Repositories.Custom.Nonsecured;

public class DestroyJobSagaRepositoryFactory(IDbContextFactory<SnapCdDbContext> dbFactory)
{
    public DestroyJobSagaRepository Create()
    {
        var dbContext = dbFactory.CreateDbContext();
        return new DestroyJobSagaRepository(dbContext);
    }
}

public class DestroyJobSagaRepository : IDisposable
{
    private readonly SnapCdDbContext _dbContext;

    public DestroyJobSagaRepository(SnapCdDbContext dbContext)
    {
        _dbContext = dbContext;
    }


    public virtual async Task<bool> Any(Guid correlationId, Guid organizationId)
    {
        return _dbContext.Set<DestroyJobSaga>().Any(i => i.CorrelationId == correlationId && i.OrganizationId == organizationId);
    }
    
    public virtual async Task<JobSagaMetaData?> GetSagaMetaDataOrNull(Guid correlationId, Guid organizationId)
    {
        var metaData = await _dbContext.Set<DestroyJobSaga>()
            .Where(i => i.CorrelationId == correlationId && i.OrganizationId == organizationId)
            .Select(x => new JobSagaMetaData
            {
                CurrentState = x.CurrentState,
                RunnerId = x.RunnerId,
                RunnerInstanceName = x.RunnerInstanceName,
                OrganizationId = x.OrganizationId,
                PreviousStateBeforeCancelling = x.PreviousStateBeforeCancelling
            })
            .FirstOrDefaultAsync();
        return metaData;
    }

    public virtual async Task<DestroyJobSaga> Get(Guid correlationId, Guid organizationId)
    {
        var query = _dbContext.Set<DestroyJobSaga>().AsQueryable();

        var entity = await query
            .FirstOrDefaultAsync(i => i.CorrelationId == correlationId && i.OrganizationId == organizationId);

        if (entity == null)
            throw new EntityNotFoundException(
                $"{nameof(DestroyJobSaga)} with CorrelationId {correlationId} in Organization {organizationId} not found.");

        return entity;
    }

    public virtual async Task<TProjection> Get<TProjection>(
        Guid correlationId,
        Guid organizationId,
        Func<IQueryable<DestroyJobSaga>, IQueryable<TProjection>>? query = null)
    {
        // Start with the base query and filter by the provided ID and OrganizationId
        var filteredQuery = _dbContext.Set<DestroyJobSaga>()
            .Where(e => e.CorrelationId == correlationId && e.OrganizationId == organizationId);

        if (query != null)
        {
            // Apply the projection query to the filtered base query
            var projectedQuery = query(filteredQuery);
            var result = await projectedQuery.FirstOrDefaultAsync();

            if (result == null)
                throw new EntityNotFoundException($"{typeof(DestroyJobSaga).Name} with CorrelationId {correlationId} in Organization {organizationId} not found.");

            return result;
        }
        else
        {
            // When no projection is provided, directly fetch the entity and cast to TProjection
            // Note: This assumes TProjection is TEntity when no query is supplied
            var entity = await filteredQuery.FirstOrDefaultAsync();

            if (entity == null)
                throw new EntityNotFoundException($"{typeof(DestroyJobSaga).Name} with CorrelationId {correlationId} in Organization {organizationId} not found.");

            return (TProjection)(object)entity;
        }
    }

    public void Dispose()
    {
        _dbContext.Dispose();
    }
}