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
using SnapCd.Server.Core.Misc.Helpers;

namespace SnapCd.Server.Core.Repositories.Organizations.Nonsecured;

public class StateMigrationJobRepositoryFactory(IDbContextFactory<SnapCdDbContext> dbFactory)
{
    public StateMigrationJobRepository Create()
    {
        return new StateMigrationJobRepository(dbFactory.CreateDbContext());
    }
}

/// <summary>
/// Writes to StateMigrationJobs. Each write has an Execute form that saves without owning a
/// transaction, for callers already inside one, and an outer form that supplies its own - the split
/// GenericRepository makes, which this cannot inherit for want of a DTO and CRUD events it has no
/// use for.
///
/// Deliberately not ModuleJobRepository: that one carries deployment
/// vocabulary a manual job has no use for — ActualStateHeadline, IsCurrent, DefinitiveRevision —
/// and sharing it would let a deployment-motivated change silently alter how manual jobs close.
/// </summary>
public class StateMigrationJobRepository : IDisposable
{
    private readonly SnapCdDbContext _dbContext;

    public StateMigrationJobRepository(SnapCdDbContext dbContext)
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

    public async Task<StateMigrationJob> Get(Guid id, Guid organizationId)
    {
        var job = await _dbContext.StateMigrationJobs
            .FirstOrDefaultAsync(j => j.Id == id && j.OrganizationId == organizationId);

        if (job == null)
            throw new EntityNotFoundException(
                $"{nameof(StateMigrationJob)} with Id {id} in Organization {organizationId} not found.");

        return job;
    }

    public async Task<List<StateMigrationJob>> ListByModule(Guid moduleId, Guid organizationId, int take = 50)
    {
        return await _dbContext.StateMigrationJobs
            .AsNoTracking()
            .Include(j => j.Transfer)
            .Where(j => j.ModuleId == moduleId && j.OrganizationId == organizationId)
            .OrderByDescending(j => j.TimestampStart)
            .Take(take)
            .ToListAsync();
    }

    public async Task<List<StateMigrationJobApproval>> ListApprovals(Guid jobId, Guid organizationId)
    {
        return await _dbContext.StateMigrationJobApprovals
            .AsNoTracking()
            .Where(a => a.StateMigrationJobId == jobId && a.OrganizationId == organizationId)
            .OrderByDescending(a => a.DecisionDateTime)
            .ToListAsync();
    }

    /// <summary>
    /// Closes the job. Only the saga may call this: the filtered unique index keys on Running, so
    /// a job left open blocks every future manual job on the module.
    /// </summary>
    public Task Finalize(
        Guid id,
        Guid organizationId,
        ExecutionStatus status,
        DateTimeOffset endTime) =>
        InTransaction(() => ExecuteFinalize(id, organizationId, status, endTime));

    public async Task ExecuteFinalize(
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
        job.WaitingForApproval = false;
        job.FailedOnServerSideStep = failedStep;
        job.ServerSideErrorHeader = ErrorText.FitHeader(errorHeader);
        job.ServerSideError = ErrorText.Body(errorMessage);

        await _dbContext.SaveChangesAsync();
    }

    public Task WaitingForApproval(Guid id, Guid organizationId, bool waitingForApproval) =>
        InTransaction(() => ExecuteWaitingForApproval(id, organizationId, waitingForApproval));

    public async Task ExecuteWaitingForApproval(Guid id, Guid organizationId, bool waitingForApproval)
    {
        var job = await Get(id, organizationId);
        job.WaitingForApproval = waitingForApproval;

        await _dbContext.SaveChangesAsync();
    }

    public Task WaitingForRunner(Guid id, Guid organizationId, bool waitingForRunner) =>
        InTransaction(() => ExecuteWaitingForRunner(id, organizationId, waitingForRunner));

    public async Task ExecuteWaitingForRunner(Guid id, Guid organizationId, bool waitingForRunner)
    {
        var job = await Get(id, organizationId);

        if (job.WaitingForRunner == waitingForRunner) return;

        job.WaitingForRunner = waitingForRunner;

        await _dbContext.SaveChangesAsync();
    }

    public Task WaitingForConsent(Guid id, Guid organizationId, bool waitingForConsent) =>
        InTransaction(() => ExecuteWaitingForConsent(id, organizationId, waitingForConsent));

    public async Task ExecuteWaitingForConsent(Guid id, Guid organizationId, bool waitingForConsent)
    {
        var job = await Get(id, organizationId);

        if (job.WaitingForConsent == waitingForConsent) return;

        job.WaitingForConsent = waitingForConsent;

        await _dbContext.SaveChangesAsync();
    }

    public void Dispose()
    {
        _dbContext?.Dispose();
    }
}
