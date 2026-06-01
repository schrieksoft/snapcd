// SPDX-License-Identifier: LicenseRef-Snap-CD-Source-Available-1.1
// Copyright (c) 2026 Karl Schriek / Schrieksoft.
// No license is granted to use this file, in whole or in part, (a) as training, fine-tuning, retrieval, or
// embedding data for any machine-learning model, or (b) as input to any machine-learning model, agent, or automated
// system for the purpose of producing a derivative work or reimplementation that is not otherwise permitted by the
// Snap CD Source-Available License (including any Competing Product as defined therein). Contact info@snapcd.io
// for terms covering either use.

using Microsoft.EntityFrameworkCore;
using SnapCd.Server.Core.Database;
using SnapCd.Server.Core.Entities.Sagas;
using SnapCd.Server.Core.Misc.Exceptions;
using SnapCd.Server.Core.Views;

namespace SnapCd.Server.Core.Repositories.Custom.Nonsecured;

public class ApplyJobSagaRepositoryFactory(IDbContextFactory<SnapCdDbContext> dbFactory)
{
    public ApplyJobSagaRepository Create()
    {
        var dbContext = dbFactory.CreateDbContext();
        return new ApplyJobSagaRepository(dbContext);
    }
}

public class ApplyJobSagaRepository : IDisposable
{
    private readonly SnapCdDbContext _dbContext;

    public ApplyJobSagaRepository(SnapCdDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public virtual async Task<bool> Any(Guid correlationId, Guid organizationId)
    {
        return _dbContext.Set<ApplyJobSaga>().Any(i => i.CorrelationId == correlationId && i.OrganizationId == organizationId);
    }
    
    
    public virtual async Task<JobSagaMetaData?> GetSagaMetaDataOrNull(Guid correlationId, Guid organizationId)
    {
        var metaData = await _dbContext.Set<ApplyJobSaga>()
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

    public virtual async Task<ApplyJobSaga> Get(Guid correlationId, Guid organizationId)
    {
        var query = _dbContext.Set<ApplyJobSaga>().AsQueryable();

        var entity = await query
            .FirstOrDefaultAsync(i => i.CorrelationId == correlationId && i.OrganizationId == organizationId);

        if (entity == null)
            throw new EntityNotFoundException(
                $"{nameof(ApplyJobSaga)} with CorrelationId {correlationId} in Organization {organizationId} not found.");

        return entity;
    }

    public virtual async Task<TProjection> Get<TProjection>(
        Guid correlationId,
        Guid organizationId,
        Func<IQueryable<ApplyJobSaga>, IQueryable<TProjection>>? query = null)
    {
        // Start with the base query and filter by the provided ID and OrganizationId
        var filteredQuery = _dbContext.Set<ApplyJobSaga>()
            .Where(e => e.CorrelationId == correlationId && e.OrganizationId == organizationId);

        if (query != null)
        {
            // Apply the projection query to the filtered base query
            var projectedQuery = query(filteredQuery);
            var result = await projectedQuery.FirstOrDefaultAsync();

            if (result == null)
                throw new EntityNotFoundException($"{typeof(ApplyJobSaga).Name} with CorrelationId {correlationId} in Organization {organizationId} not found.");

            return result;
        }
        else
        {
            // When no projection is provided, directly fetch the entity and cast to TProjection
            // Note: This assumes TProjection is TEntity when no query is supplied
            var entity = await filteredQuery.FirstOrDefaultAsync();

            if (entity == null)
                throw new EntityNotFoundException($"{typeof(ApplyJobSaga).Name} with CorrelationId {correlationId} in Organization {organizationId} not found.");

            return (TProjection)(object)entity;
        }
    }

    public void Dispose()
    {
        _dbContext.Dispose();
    }
}