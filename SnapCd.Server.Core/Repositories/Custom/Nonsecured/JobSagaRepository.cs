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
        var applyJobSagaRepository = new ApplyJobSagaRepository(dbContext);
        var destroyJobSagaRepository = new DestroyJobSagaRepository(dbContext);
        var splitMonolithSagaRepository = new SplitMigrateSagaRepository(dbContext);
        var transferMigrateSagaRepository = new TransferMigrateSagaRepository(dbContext);
        var stateListFilteredSagaRepository = new StateListFilteredSagaRepository(dbContext);
        return new JobSagaRepository(
            dbContext, applyJobSagaRepository, destroyJobSagaRepository,
            splitMonolithSagaRepository, transferMigrateSagaRepository,
            stateListFilteredSagaRepository);
    }
}

public class JobSagaRepository : IDisposable
{
    private readonly SnapCdDbContext _dbContext;
    private readonly ApplyJobSagaRepository _applyJobSagaRepository;
    private readonly DestroyJobSagaRepository _destroyJobSagaRepository;
    private readonly SplitMigrateSagaRepository _splitMonolithSagaRepository;
    private readonly TransferMigrateSagaRepository _transferMigrateSagaRepository;
    private readonly StateListFilteredSagaRepository _stateListFilteredSagaRepository;

    public JobSagaRepository(
        SnapCdDbContext dbContext,
        ApplyJobSagaRepository applyJobSagaRepository,
        DestroyJobSagaRepository destroyJobSagaRepository,
        SplitMigrateSagaRepository splitMonolithSagaRepository,
        TransferMigrateSagaRepository transferMigrateSagaRepository,
        StateListFilteredSagaRepository stateListFilteredSagaRepository)
    {
        _dbContext = dbContext;
        _applyJobSagaRepository = applyJobSagaRepository;
        _destroyJobSagaRepository = destroyJobSagaRepository;
        _splitMonolithSagaRepository = splitMonolithSagaRepository;
        _transferMigrateSagaRepository = transferMigrateSagaRepository;
        _stateListFilteredSagaRepository = stateListFilteredSagaRepository;
    }

    /// <summary>
    /// Resolves a correlation id to its saga, whichever family owns it. Job ids are unique across
    /// families, so the search order does not affect the result.
    /// </summary>
    public virtual async Task<JobSagaMetaData> GetSagaMetaData(Guid correlationId, Guid organizationId)
    {
        var metaData = await _applyJobSagaRepository.GetSagaMetaDataOrNull(correlationId, organizationId);
        if (metaData == null)
            metaData = await _destroyJobSagaRepository.GetSagaMetaDataOrNull(correlationId, organizationId);

        if (metaData == null)
        {
            var split = await _splitMonolithSagaRepository.GetSagaMetaDataOrNull(correlationId, organizationId);
            if (split != null)
                metaData = new JobSagaMetaData
                {
                    Family = JobSagaFamily.SplitMigrate,
                    CurrentState = split.CurrentState,
                    RunnerId = split.RunnerId,
                    RunnerInstanceName = split.RunnerInstanceName,
                    OrganizationId = split.OrganizationId,
                    PreviousStateBeforeCancelling = split.PreviousStateBeforeCancelling
                };
        }

        if (metaData == null)
            metaData = await _transferMigrateSagaRepository.GetSagaMetaDataOrNull(correlationId, organizationId);

        if (metaData == null)
            metaData = await _stateListFilteredSagaRepository.GetSagaMetaDataOrNull(correlationId, organizationId);

        if (metaData == null)
            throw new EntityNotFoundException($"Could not find a Job with correlation id {correlationId} in Organization {organizationId}.");

        return metaData;
    }


    public void Dispose()
    {
        _applyJobSagaRepository?.Dispose();
        _destroyJobSagaRepository?.Dispose();
        _splitMonolithSagaRepository?.Dispose();
        _stateListFilteredSagaRepository?.Dispose();
        _transferMigrateSagaRepository?.Dispose();
        _dbContext?.Dispose();
    }
}