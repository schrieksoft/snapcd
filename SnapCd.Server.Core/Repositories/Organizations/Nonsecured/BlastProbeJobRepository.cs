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
using SnapCd.Server.Core.Misc.Exceptions;
using SnapCd.Server.Core.Misc.Helpers;

namespace SnapCd.Server.Core.Repositories.Organizations.Nonsecured;

public class BlastProbeJobRepositoryFactory(IDbContextFactory<SnapCdDbContext> dbFactory)
{
    public BlastProbeJobRepository Create()
    {
        return new BlastProbeJobRepository(dbFactory.CreateDbContext());
    }
}

/// <summary>
/// Writes to BlastProbeJobs. Each write has an Execute form that saves without owning a
/// transaction, for callers already inside one, and an outer form that supplies its own, following
/// StateMigrationJobRepository.
/// </summary>
public class BlastProbeJobRepository : IDisposable
{
    private readonly SnapCdDbContext _dbContext;

    public BlastProbeJobRepository(SnapCdDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    /// <summary>
    /// Wraps a write in its own transaction, unless one is already open. A saga consume runs inside
    /// the transaction MassTransit's saga repository opens on this same context, so the Execute form
    /// is what an activity calls; a caller outside a consume gets the transaction from here.
    /// </summary>
    private async Task InTransaction(Func<Task> write)
    {
        if (_dbContext.Database.CurrentTransaction is not null)
        {
            await write();
            return;
        }

        await using var transaction = await _dbContext.Database.BeginTransactionAsync();
        try
        {
            await write();
            await transaction.CommitAsync();
        }
        catch
        {
            await transaction.RollbackAsync();
            throw;
        }
    }

    public async Task<BlastProbeJob> Get(Guid id, Guid organizationId)
    {
        var job = await _dbContext.BlastProbeJobs
            .FirstOrDefaultAsync(j => j.Id == id && j.OrganizationId == organizationId);

        if (job == null)
            throw new EntityNotFoundException(
                $"{nameof(BlastProbeJob)} with Id {id} in Organization {organizationId} not found.");

        return job;
    }

    public async Task<List<BlastProbeJob>> ListByModule(Guid moduleId, Guid organizationId, int take = 50)
    {
        return await _dbContext.BlastProbeJobs
            .AsNoTracking()
            .Where(j => j.ModuleId == moduleId && j.OrganizationId == organizationId)
            .OrderByDescending(j => j.TimestampStart)
            .Take(take)
            .ToListAsync();
    }

    /// <summary>Every blast probe prompted by one upstream job.</summary>
    public async Task<List<BlastProbeJob>> ListByUpstreamJob(Guid upstreamModuleJobId, Guid organizationId)
    {
        return await _dbContext.BlastProbeJobs
            .AsNoTracking()
            .Where(j => j.UpstreamModuleJobId == upstreamModuleJobId && j.OrganizationId == organizationId)
            .OrderBy(j => j.TimestampStart)
            .ToListAsync();
    }

    public Task Finalize(
        Guid id,
        Guid organizationId,
        ExecutionStatus status,
        DateTimeOffset endTime,
        string? signalsJson = null) =>
        InTransaction(() => ExecuteFinalize(id, organizationId, status, endTime, signalsJson));

    public async Task ExecuteFinalize(
        Guid id,
        Guid organizationId,
        ExecutionStatus status,
        DateTimeOffset endTime,
        string? signalsJson = null)
    {
        var job = await Get(id, organizationId);

        job.Status = status;
        job.TimestampEnd = endTime;
        job.WaitingForRunner = false;

        if (signalsJson is not null)
            job.SignalsJson = signalsJson;

        await _dbContext.SaveChangesAsync();
    }

    public Task FinalizeWithServerError(
        Guid id,
        Guid organizationId,
        DateTimeOffset endTime,
        ServerSideStep? failedStep,
        string? errorHeader,
        string? errorMessage) =>
        InTransaction(() => ExecuteFinalizeWithServerError(
            id, organizationId, endTime, failedStep, errorHeader, errorMessage));

    public async Task ExecuteFinalizeWithServerError(
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
        job.WaitingForRunner = false;
        job.FailedOnServerSideStep = failedStep;
        job.ServerSideErrorHeader = ErrorText.FitHeader(errorHeader);
        job.ServerSideError = ErrorText.Body(errorMessage);

        await _dbContext.SaveChangesAsync();
    }

    public Task WaitingForRunner(Guid id, Guid organizationId, bool waitingForRunner) =>
        InTransaction(() => ExecuteWaitingForRunner(id, organizationId, waitingForRunner));

    public async Task ExecuteWaitingForRunner(Guid id, Guid organizationId, bool waitingForRunner)
    {
        var job = await Get(id, organizationId);
        job.WaitingForRunner = waitingForRunner;

        await _dbContext.SaveChangesAsync();
    }

    public void Dispose()
    {
        _dbContext.Dispose();
    }
}
