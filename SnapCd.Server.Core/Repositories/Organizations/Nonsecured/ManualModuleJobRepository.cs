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
using SnapCd.Server.Core.Enums;
using SnapCd.Server.Core.Entities.Definition;
using SnapCd.Server.Core.Misc.Exceptions;

namespace SnapCd.Server.Core.Repositories.Organizations.Nonsecured;

public class ManualModuleJobRepositoryFactory(IDbContextFactory<SnapCdDbContext> dbFactory)
{
    public ManualModuleJobRepository Create()
    {
        return new ManualModuleJobRepository(dbFactory.CreateDbContext());
    }
}

/// <summary>
/// Writes to ManualModuleJobs. Deliberately not ModuleJobRepository: that one carries deployment
/// vocabulary a manual job has no use for — ActualStateHeadline, IsCurrent, DefinitiveRevision —
/// and sharing it would let a deployment-motivated change silently alter how manual jobs close.
/// </summary>
public class ManualModuleJobRepository : IDisposable
{
    private readonly SnapCdDbContext _dbContext;

    public ManualModuleJobRepository(SnapCdDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<ManualModuleJob> Get(Guid id, Guid organizationId)
    {
        var job = await _dbContext.ManualModuleJobs
            .FirstOrDefaultAsync(j => j.Id == id && j.OrganizationId == organizationId);

        if (job == null)
            throw new EntityNotFoundException(
                $"{nameof(ManualModuleJob)} with Id {id} in Organization {organizationId} not found.");

        return job;
    }

    /// <summary>
    /// Closes the job. Only the saga may call this: the filtered unique index keys on Running, so
    /// a job left open blocks every future manual job on the module.
    /// </summary>
    public async Task Finalize(
        Guid id,
        Guid organizationId,
        ExecutionStatus status,
        DateTimeOffset endTime)
    {
        var job = await Get(id, organizationId);

        job.Status = status;
        job.TimestampEnd = endTime;
        job.WaitingForApproval = false;

        await _dbContext.SaveChangesAsync();
    }

    public async Task FinalizeWithServerError(
        Guid id,
        Guid organizationId,
        DateTimeOffset endTime,
        ServerSideStep? failedStep,
        string? errorHeader,
        string? errorMessage)
    {
        var job = await Get(id, organizationId);

        job.Status = ExecutionStatus.Failed;
        job.TimestampEnd = endTime;
        job.WaitingForApproval = false;
        job.FailedOnServerSideStep = failedStep;
        job.ServerSideErrorHeader = errorHeader;
        job.ServerSideError = errorMessage;

        await _dbContext.SaveChangesAsync();
    }

    public async Task WaitingForApproval(Guid id, Guid organizationId, bool waitingForApproval)
    {
        var job = await Get(id, organizationId);
        job.WaitingForApproval = waitingForApproval;

        await _dbContext.SaveChangesAsync();
    }

    public void Dispose()
    {
        _dbContext?.Dispose();
    }
}
