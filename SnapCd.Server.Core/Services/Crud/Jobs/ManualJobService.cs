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
using SnapCd.Server.Core.Entities.Sagas;
using SnapCd.Server.Core.Enums;
using SnapCd.Server.Core.Misc.Exceptions;
using SnapCd.Server.Core.Repositories.Organizations.Secured;
using SnapCd.Server.Core.Events.Jobs.Module;
using SnapCd.Server.Core.Events.System;
using SnapCd.Server.Core.Services.PrincipalProvider;
using SnapCd.Server.Core.Services.ResolvedConfiguration;
using MassTransit;

namespace SnapCd.Server.Core.Services.Crud.Jobs;

public class ManualJobServiceFactory(
    IDbContextFactory<SnapCdDbContext> dbFactory,
    ModuleSecuredRepositoryFactory moduleSecuredRepositoryFactory,
    ResolvedConfigurationServiceFactory resolvedConfigurationServiceFactory,
    IBus bus)
{
    public ManualJobService Create(IPrincipalProvider? principalProvider = null)
    {
        return new ManualJobService(
            dbFactory,
            moduleSecuredRepositoryFactory.Create(principalProvider),
            resolvedConfigurationServiceFactory.Create(),
            bus);
    }
}

/// <summary>
/// Starts operator-initiated jobs against a paused Module. There is no gatekeeping saga here:
/// a manual job that cannot start is refused rather than queued, so it never fires at an
/// unpredictable later moment.
/// </summary>
public class ManualJobService : IDisposable
{
    private readonly IDbContextFactory<SnapCdDbContext> _dbContextFactory;
    private readonly ModuleSecuredRepository _moduleSecuredRepository;
    private readonly ResolvedConfigurationService? _resolvedConfigurationService;
    private readonly IBus? _bus;

    public ManualJobService(
        IDbContextFactory<SnapCdDbContext> dbContextFactory,
        ModuleSecuredRepository moduleSecuredRepository,
        ResolvedConfigurationService? resolvedConfigurationService = null,
        IBus? bus = null)
    {
        _dbContextFactory = dbContextFactory;
        _moduleSecuredRepository = moduleSecuredRepository;
        _resolvedConfigurationService = resolvedConfigurationService;
        _bus = bus;
    }

    /// <summary>
    /// Why a manual job cannot start on this Module, or null when it can. Drives the UI's
    /// explanation of a disabled launcher; the launch itself re-checks rather than trusting this.
    /// </summary>
    public async Task<string?> GetBlockedReason(Guid moduleId, Guid organizationId)
    {
        await using var dbContext = await _dbContextFactory.CreateDbContextAsync();

        var saga = await dbContext.Set<ModuleSaga>().AsNoTracking()
            .FirstOrDefaultAsync(s => s.CorrelationId == moduleId && s.OrganizationId == organizationId);

        if (saga is null)
            return "The module has no saga and cannot run manual jobs.";

        if (!saga.Paused)
            return "The module must be paused before a manual job can run.";

        var hasRunningModuleJob = await dbContext.ModuleJobs.AsNoTracking()
            .AnyAsync(j => j.ModuleId == moduleId && j.OrganizationId == organizationId && j.IsCurrent == true);

        if (hasRunningModuleJob)
            return "A job is still finishing on this module. Manual jobs become available once it is quiet.";

        var hasRunningManualJob = await dbContext.ManualModuleJobs.AsNoTracking()
            .AnyAsync(j => j.ModuleId == moduleId && j.OrganizationId == organizationId
                           && j.Status == ExecutionStatus.Running);

        if (hasRunningManualJob)
            return "A manual job is already running on this module.";

        return null;
    }

    /// <summary>
    /// Creates the job row and returns it. The returned Id is the correlation id the caller must
    /// publish its saga request with: as with ModuleJob, the job row and its saga share one id, so
    /// `JobType` names the saga table and `Id` names the row in it.
    /// </summary>
    /// <remarks>
    /// The row is left Running. Only the saga may write a terminal status — the filtered unique
    /// index keys on Running, so an early terminal write here would let a second job start
    /// underneath the first.
    /// </remarks>
    public async Task<ManualModuleJob> Start(
        Guid moduleId,
        Guid organizationId,
        string jobType,
        Guid? optionalCorrelationId = null)
    {
        if (!_moduleSecuredRepository.CanPause(moduleId, organizationId))
            throw new PrincipalNotAuthorizedException(
                $"Principal is not allowed to run manual jobs on Module with Id {moduleId}");

        var blocked = await GetBlockedReason(moduleId, organizationId);
        if (blocked is not null)
            throw new ManualJobNotAllowedException(blocked);

        var correlationId = optionalCorrelationId ?? Guid.NewGuid();

        await using var dbContext = await _dbContextFactory.CreateDbContextAsync();

        var job = new ManualModuleJob
        {
            Id = correlationId,
            ModuleId = moduleId,
            OrganizationId = organizationId,
            TimestampStart = DateTimeOffset.UtcNow,
            JobType = jobType,
            Status = ExecutionStatus.Running
        };

        dbContext.ManualModuleJobs.Add(job);

        try
        {
            await dbContext.SaveChangesAsync();
        }
        catch (DbUpdateException)
        {
            // The filtered unique index is the real guarantee: the check above can be raced.
            throw new ManualJobNotAllowedException("A manual job is already running on this module.");
        }

        return job;
    }

    /// <summary>
    /// Starts a SplitMonolith job: creates the record, then publishes the saga request with the
    /// same id. The two share one correlation id, so publishing with a fresh one would leave the
    /// row and its saga unable to find each other.
    /// </summary>
    public async Task<ManualModuleJob> StartSplitMonolith(
        Guid moduleId,
        Guid organizationId,
        string? rootDirectory,
        bool force)
    {
        if (_resolvedConfigurationService is null || _bus is null)
            throw new InvalidOperationException(
                $"{nameof(ManualJobService)} was constructed without the dependencies needed to start a job.");

        var job = await Start(moduleId, organizationId, ManualJobTypes.SplitMonolith);

        try
        {
            var declared = await _resolvedConfigurationService.GetDeclared(moduleId, organizationId);

            await _bus.Publish(new SplitMonolithRequested
            {
                CorrelationId = job.Id,
                Declared = declared,
                RootDirectory = rootDirectory,
                Force = force
            });
        }
        catch (Exception ex)
        {
            // The row is already Running and would block every later manual job on this module.
            await FailJob(job.Id, organizationId, ex.Message);
            throw;
        }

        return job;
    }

    /// <summary>
    /// Records an approve or decline decision on a manual job and asks the saga to re-evaluate.
    /// The unique index on (job, principal) means one decision per principal; a second attempt
    /// surfaces as a refusal rather than silently replacing the first.
    /// </summary>
    public async Task Decide(Guid jobId, Guid moduleId, Guid organizationId, bool declined, string? reason)
    {
        if (_bus is null)
            throw new InvalidOperationException(
                $"{nameof(ManualJobService)} was constructed without the dependencies needed to record a decision.");

        if (!_moduleSecuredRepository.CanPause(moduleId, organizationId))
            throw new PrincipalNotAuthorizedException(
                $"Principal is not allowed to decide manual jobs on Module with Id {moduleId}");

        if (declined && string.IsNullOrWhiteSpace(reason))
            throw new ArgumentException("A decline needs a reason.", nameof(reason));

        await using var dbContext = await _dbContextFactory.CreateDbContextAsync();

        var job = await dbContext.ManualModuleJobs
            .FirstOrDefaultAsync(j => j.Id == jobId && j.OrganizationId == organizationId);

        if (job is null)
            throw new EntityNotFoundException($"Manual job '{jobId}' not found");

        if (job.WaitingForApproval != true)
            throw new ManualJobNotAllowedException("This job is not waiting for approval.");

        dbContext.ManualModuleJobApprovals.Add(new ManualModuleJobApproval
        {
            Id = Guid.NewGuid(),
            OrganizationId = organizationId,
            ManualModuleJobId = jobId,
            DecisionDateTime = DateTime.UtcNow,
            Declined = declined,
            PrincipalId = _moduleSecuredRepository.PrincipalProvider.GetSubject(organizationId),
            PrincipalDiscriminator = _moduleSecuredRepository.PrincipalProvider.GetPrincipalDiscriminator(),
            AgentId = _moduleSecuredRepository.PrincipalProvider.GetAgentId(),
            Reason = reason
        });

        try
        {
            await dbContext.SaveChangesAsync();
        }
        catch (DbUpdateException)
        {
            // One decision per principal per job, enforced by the index rather than by this check.
            throw new ManualJobNotAllowedException("You have already decided on this job.");
        }

        await _bus.Publish(new ApprovalReevaluationRequestedEvent
        {
            ModuleId = moduleId,
            ModuleJobId = jobId
        });
    }

    private async Task FailJob(Guid jobId, Guid organizationId, string errorMessage)
    {
        await using var dbContext = await _dbContextFactory.CreateDbContextAsync();

        var job = await dbContext.ManualModuleJobs
            .FirstOrDefaultAsync(j => j.Id == jobId && j.OrganizationId == organizationId);

        if (job is null) return;

        job.Status = ExecutionStatus.Failed;
        job.TimestampEnd = DateTimeOffset.UtcNow;
        job.FailedOnServerSideStep = ServerSideStep.Start;
        job.ServerSideErrorHeader = "This job failed due to an error occurring on the Server. The full error can be seen below.";
        job.ServerSideError = errorMessage;

        await dbContext.SaveChangesAsync();
    }

    /// <summary>
    /// Records a manual job that failed before its saga could take over, so a launch that throws
    /// leaves a visible job rather than nothing. Mirrors JobService.CreateFailedJob.
    /// </summary>
    public async Task CreateFailedJob(
        Guid correlationId,
        Guid moduleId,
        Guid organizationId,
        string jobType,
        string errorMessage)
    {
        await using var dbContext = await _dbContextFactory.CreateDbContextAsync();

        var timestamp = DateTimeOffset.UtcNow;

        dbContext.ManualModuleJobs.Add(new ManualModuleJob
        {
            Id = correlationId,
            ModuleId = moduleId,
            OrganizationId = organizationId,
            TimestampStart = timestamp,
            TimestampEnd = timestamp,
            JobType = jobType,
            Status = ExecutionStatus.Failed,
            ServerSideErrorHeader = "This job failed due to an error occurring on the Server. The full error can be seen below.",
            ServerSideError = errorMessage
        });

        await dbContext.SaveChangesAsync();
    }

    public void Dispose()
    {
        _moduleSecuredRepository?.Dispose();
    }
}
