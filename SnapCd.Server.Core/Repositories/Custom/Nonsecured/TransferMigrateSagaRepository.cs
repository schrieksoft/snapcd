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

public class TransferMigrateSagaRepositoryFactory(IDbContextFactory<SnapCdDbContext> dbFactory)
{
    public TransferMigrateSagaRepository Create() => new(dbFactory.CreateDbContext());
}

public class TransferMigrateSagaRepository : IDisposable
{
    private readonly SnapCdDbContext _dbContext;

    public TransferMigrateSagaRepository(SnapCdDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    /// <summary>
    /// A transfer job is per Module, so a reply names both: the job, and which Module answered.
    /// </summary>
    public virtual async Task<JobSagaMetaData> GetSagaMetaData(Guid jobId, Guid moduleId, Guid organizationId)
    {
        var metaData = await _dbContext.Set<TransferMigrateSaga>()
            .Where(x =>
                x.CorrelationId == jobId &&
                x.ModuleId == moduleId &&
                x.OrganizationId == organizationId)
            .Select(x => new JobSagaMetaData
            {
                CurrentState = x.CurrentState,
                RunnerId = x.RunnerId,
                RunnerInstanceName = x.RunnerInstanceName,
                OrganizationId = x.OrganizationId
            })
            .FirstOrDefaultAsync();

        if (metaData == null)
            throw new EntityNotFoundException(
                $"Could not find Module {moduleId} on transfer job {jobId} in Organization {organizationId}.");

        return metaData;
    }

    public void Dispose() => _dbContext?.Dispose();
}
