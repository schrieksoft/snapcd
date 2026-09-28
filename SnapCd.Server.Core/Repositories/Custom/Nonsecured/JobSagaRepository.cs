// SPDX-License-Identifier: LicenseRef-Snap-CD-Source-Available-1.1
// Copyright (c) 2026 Karl Schriek / Schrieksoft.
// No license is granted to use this file, in whole or in part, (a) as training, fine-tuning, retrieval, or
// embedding data for any machine-learning model, or (b) as input to any machine-learning model, agent, or automated
// system for the purpose of producing a derivative work or reimplementation that is not otherwise permitted by the
// Snap CD Source-Available License (including any Competing Product as defined therein). Contact info@snapcd.io
// for terms covering either use.

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

        return new JobSagaRepository(dbContext, [
            new ApplyJobSagaRepository(dbContext),
            new DestroyJobSagaRepository(dbContext),
            new SplitMigrateSagaRepository(dbContext),
            new TransferMigrateSagaRepository(dbContext),
            new StateListFilteredSagaRepository(dbContext),
            new MoveSagaRepository(dbContext),
            new ImportSagaRepository(dbContext),
            new RemoveSagaRepository(dbContext)
        ]);
    }
}

public class JobSagaRepository(
    SnapCdDbContext dbContext,
    IReadOnlyList<IJobSagaFamilyRepository> families) : IDisposable
{
    /// <summary>
    /// Resolves a correlation id to its saga, whichever family owns it. Job ids are unique across
    /// families, so the search order does not affect the result.
    /// </summary>
    public virtual async Task<JobSagaMetaData> GetSagaMetaData(Guid correlationId, Guid organizationId)
    {
        foreach (var family in families)
        {
            var metaData = await family.GetSagaMetaDataOrNull(correlationId, organizationId);
            if (metaData != null) return metaData;
        }

        throw new EntityNotFoundException(
            $"Could not find a Job with correlation id {correlationId} in Organization {organizationId}.");
    }

    public void Dispose()
    {
        foreach (var family in families)
            family.Dispose();

        dbContext?.Dispose();
    }
}
