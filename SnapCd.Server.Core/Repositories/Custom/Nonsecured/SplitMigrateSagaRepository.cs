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

public class SplitMigrateSagaRepositoryFactory(IDbContextFactory<SnapCdDbContext> dbFactory)
{
    public SplitMigrateSagaRepository Create() => new(dbFactory.CreateDbContext());
}

/// <summary>
/// Reads SplitMigrateSagas for runner authorization. Separate from JobSagaRepository, which looks
/// only in the deployment saga tables.
/// </summary>
public class SplitMigrateSagaRepository : IDisposable
{
    private readonly SnapCdDbContext _dbContext;

    public SplitMigrateSagaRepository(SnapCdDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public virtual async Task<JobSagaMetaData?> GetSagaMetaDataOrNull(Guid correlationId, Guid organizationId)
    {
        return await _dbContext.Set<SplitMigrateSaga>()
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
    }

    public virtual async Task<JobSagaMetaData> GetSagaMetaData(Guid correlationId, Guid organizationId)
    {
        var metaData = await _dbContext.Set<SplitMigrateSaga>()
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

        if (metaData == null)
            throw new EntityNotFoundException(
                $"Could not find a SplitMigrate job with correlation id {correlationId} in Organization {organizationId}.");

        return metaData;
    }

    public void Dispose() => _dbContext?.Dispose();
}
