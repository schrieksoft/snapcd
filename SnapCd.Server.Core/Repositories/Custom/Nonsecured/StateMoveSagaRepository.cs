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
using SnapCd.Server.Core.Views;

namespace SnapCd.Server.Core.Repositories.Custom.Nonsecured;

public class StateMoveSagaRepositoryFactory(IDbContextFactory<SnapCdDbContext> dbFactory)
{
    public StateMoveSagaRepository Create() => new(dbFactory.CreateDbContext());
}

public class StateMoveSagaRepository(SnapCdDbContext dbContext) : IDisposable
{
    /// <summary>The job's saga, or null when this job belongs to another family.</summary>
    public virtual async Task<JobSagaMetaData?> GetSagaMetaDataOrNull(Guid jobId, Guid organizationId) =>
        await dbContext.Set<StateMoveSaga>()
            .Where(x => x.CorrelationId == jobId && x.OrganizationId == organizationId)
            .Select(x => new JobSagaMetaData
            {
                Family = JobSagaFamily.StateMove,
                CurrentState = x.CurrentState,
                RunnerId = x.RunnerId,
                RunnerInstanceName = x.RunnerInstanceName,
                OrganizationId = x.OrganizationId,
                PreviousStateBeforeCancelling = x.PreviousStateBeforeCancelling
            })
            .FirstOrDefaultAsync();

    public void Dispose() => dbContext?.Dispose();
}
