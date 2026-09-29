// SPDX-License-Identifier: LicenseRef-Snap-CD-Source-Available-1.1
// Copyright (c) 2026 Karl Schriek / Schrieksoft.
// No license is granted to use this file, in whole or in part, (a) as training, fine-tuning, retrieval, or
// embedding data for any machine-learning model, or (b) as input to any machine-learning model, agent, or automated
// system for the purpose of producing a derivative work or reimplementation that is not otherwise permitted by the
// Snap CD Source-Available License (including any Competing Product as defined therein). Contact info@snapcd.io
// for terms covering either use.


using Microsoft.EntityFrameworkCore;
using SnapCd.Contracts;
using SnapCd.Server.Core.Database;
using SnapCd.Server.Core.Entities.Definition;
using SnapCd.Server.Core.Entities.Sagas;
using SnapCd.Server.Core.Enums;
using SnapCd.Server.Core.Misc.Exceptions;
using SnapCd.Server.Core.Misc.Utils;
using SnapCd.Server.Core.Repositories.Organizations.Secured;
using SnapCd.Server.Core.Events.Jobs.Module;
using SnapCd.Server.Core.Events.Steps.StateMigrations;
using SnapCd.Server.Core.Events.System;
using SnapCd.Server.Core.Services.PrincipalProvider;
using SnapCd.Server.Core.Services.ResolvedConfiguration;
using MassTransit;

namespace SnapCd.Server.Core.Services.Crud.Jobs;

public class StateMigrationServiceFactory(
    IDbContextFactory<SnapCdDbContext> dbFactory,
    ModuleSecuredRepositoryFactory moduleSecuredRepositoryFactory,
    ResolvedConfigurationServiceFactory resolvedConfigurationServiceFactory,
    IBus bus)
{
    public StateMigrationService Create(IPrincipalProvider? principalProvider = null)
    {
        return new StateMigrationService(
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
public class StateMigrationService : IDisposable
{
    private readonly IDbContextFactory<SnapCdDbContext> _dbContextFactory;
    private readonly ModuleSecuredRepository _moduleSecuredRepository;
    private readonly ResolvedConfigurationService? _resolvedConfigurationService;
    private readonly IBus? _bus;

    public StateMigrationService(
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

        var hasRunningStateMigration = await dbContext.StateMigrationJobs.AsNoTracking()
            .AnyAsync(j => j.ModuleId == moduleId && j.OrganizationId == organizationId
                           && j.Status == ExecutionStatus.Running);

        if (hasRunningStateMigration)
            return "A manual job is already running on this module.";

        // A manual job is pinned to one runner for its whole life, so one dispatched with nothing
        // connected is not late, it is lost. Deployment jobs queue here; a manual job is started
        // by someone standing on the page, so it is better refused than parked out of sight.
        if (!await HasConnectedRunner(dbContext, moduleId, organizationId))
            return "No runner is connected for this module.";

        return null;
    }

    /// <summary>
    /// Whether the runner this Module is configured for has a connection, honouring a Module that
    /// pins itself to one named instance.
    /// </summary>
    private static async Task<bool> HasConnectedRunner(
        SnapCdDbContext dbContext, Guid moduleId, Guid organizationId)
    {
        var module = await dbContext.Modules.AsNoTracking()
            .Where(m => m.Id == moduleId && m.OrganizationId == organizationId)
            .Select(m => new { m.RunnerId, m.RunnerInstanceName })
            .FirstOrDefaultAsync();

        if (module is null) return false;

        return await dbContext.RunnerConnections.AsNoTracking()
            .AnyAsync(rc => rc.RunnerId == module.RunnerId
                            && rc.OrganizationId == organizationId
                            && (module.RunnerInstanceName == null
                                || rc.InstanceName == module.RunnerInstanceName));
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
    /// <summary>
    /// Says a job has appeared, so an open page re-reads what is now blocked. Only the four
    /// finalization activities announced a change before, which left every other viewer's
    /// launchers live until something else refreshed them.
    /// </summary>
    private async Task AnnounceStarted(StateMigrationJob job)
    {
        if (_bus is null) return;

        await _bus.Publish(new StateMigrationUpdatedEvent
        {
            JobId = job.Id,
            ModuleId = job.ModuleId,
            OrganizationId = job.OrganizationId
        });
    }

    public async Task<StateMigrationJob> Start(
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
            throw new StateMigrationNotAllowedException(blocked);

        var correlationId = optionalCorrelationId ?? Guid.NewGuid();

        await using var dbContext = await _dbContextFactory.CreateDbContextAsync();

        var job = new StateMigrationJob
        {
            Id = correlationId,
            ModuleId = moduleId,
            OrganizationId = organizationId,
            TimestampStart = DateTimeOffset.UtcNow,
            JobType = jobType,
            Status = ExecutionStatus.Running
        };

        dbContext.StateMigrationJobs.Add(job);

        try
        {
            await dbContext.SaveChangesAsync();
        }
        catch (DbUpdateException)
        {
            // The filtered unique index is the real guarantee: the check above can be raced.
            throw new StateMigrationNotAllowedException("A manual job is already running on this module.");
        }

        await AnnounceStarted(job);

        return job;
    }
    /// <summary>
    /// Starts a transfer attempt: a job on each Module the run covers, at once. demonolith's own
    /// ordering interlock is waived, so neither waits for the other; what moved is recorded instead.
    /// </summary>
    /// <summary>
    /// Starts this Module's half of a transfer. The counterparty is named only so a plan that reads
    /// a value the other side produces knows whose outputs to wait for; the two are started
    /// separately and nothing here coordinates them.
    /// </summary>
    public async Task<StateMigrationJob> StartTransferMigrate(
        Guid moduleId,
        Guid counterpartyModuleId,
        Guid organizationId,
        string? proveRef,
        Guid? transferId = null,
        bool awaitConsent = false)
    {
        if (_resolvedConfigurationService is null || _bus is null)
            throw new InvalidOperationException(
                $"{nameof(StateMigrationService)} was constructed without the dependencies needed to start a job.");

        if (!_moduleSecuredRepository.CanConsent(moduleId, organizationId))
            throw new PrincipalNotAuthorizedException(
                $"Principal is not allowed to write state on Module with Id {moduleId}");

        if (counterpartyModuleId == moduleId)
            throw new StateMigrationNotAllowedException("A Module cannot transfer to itself.");

        await using var dbContext = await _dbContextFactory.CreateDbContextAsync();

        var blocked = await GetBlockedReason(moduleId, organizationId);
        if (blocked is not null)
            throw new StateMigrationNotAllowedException($"{await ModuleName(dbContext, moduleId)}: {blocked}");

        var job = new StateMigrationJob
        {
            Id = Guid.NewGuid(),
            ModuleId = moduleId,
            TransferId = transferId,
            ProveRef = proveRef,
            OrganizationId = organizationId,
            TimestampStart = DateTimeOffset.UtcNow,
            JobType = StateMigrationTypes.TransferMigrate,
            Status = ExecutionStatus.Running
        };

        dbContext.StateMigrationJobs.Add(job);
        await dbContext.SaveChangesAsync();
        await AnnounceStarted(job);

        try
        {
            await _bus.Publish(new TransferMigrateRequested
            {
                CorrelationId = job.Id,
                Declared = await _resolvedConfigurationService.GetDeclared(moduleId, organizationId),
                CounterpartyModuleId = counterpartyModuleId,
                TransferId = transferId,
                AwaitConsent = awaitConsent,
                ProveRef = proveRef
            });
        }
        catch (Exception ex)
        {
            // The row is already Running and would block every later manual job on this Module.
            await FailJob(job.Id, organizationId, ex.Message);
            throw;
        }

        return job;
    }

    private static async Task<string> ModuleName(SnapCdDbContext dbContext, Guid moduleId) =>
        await dbContext.Modules.AsNoTracking()
            .Where(m => m.Id == moduleId)
            .Select(m => m.Name)
            .FirstOrDefaultAsync() ?? moduleId.ToString()[..8];

    /// <summary>One Module's state move, under a run.</summary>
    /// <summary>
    /// Asks which of these addresses are in a Module's state. Writes nothing, so it needs only what
    /// any manual job needs: the Module free to run.
    /// </summary>
    public async Task<StateMigrationJob> StartStateListFiltered(
        Guid moduleId, Guid organizationId, IReadOnlyCollection<string> addresses)
    {
        if (_resolvedConfigurationService is null || _bus is null)
            throw new InvalidOperationException(
                $"{nameof(StateMigrationService)} was constructed without the dependencies needed to start a job.");

        if (!_moduleSecuredRepository.CanPause(moduleId, organizationId))
            throw new PrincipalNotAuthorizedException(
                $"Principal is not allowed to run manual jobs on Module with Id {moduleId}");

        if (addresses.Count == 0)
            throw new StateMigrationNotAllowedException("Name at least one address to check.");

        var blocked = await GetBlockedReason(moduleId, organizationId);
        if (blocked is not null)
            throw new StateMigrationNotAllowedException(blocked);

        await using var dbContext = await _dbContextFactory.CreateDbContextAsync();

        var job = new StateMigrationJob
        {
            Id = Guid.NewGuid(),
            ModuleId = moduleId,
            OrganizationId = organizationId,
            TimestampStart = DateTimeOffset.UtcNow,
            JobType = StateMigrationTypes.StateListFiltered,
            Status = ExecutionStatus.Running
        };

        dbContext.StateMigrationJobs.Add(job);
        await dbContext.SaveChangesAsync();
        await AnnounceStarted(job);

        try
        {
            await _bus.Publish(new StateListFilteredJobRequested
            {
                CorrelationId = job.Id,
                Declared = await _resolvedConfigurationService.GetDeclared(moduleId, organizationId),
                Addresses = addresses.Distinct().ToList()
            });
        }
        catch (Exception ex)
        {
            await FailJob(job.Id, organizationId, ex.Message);
            throw;
        }

        return job;
    }

    /// <summary>
    /// Moves, imports or removes addresses in a Module's state, then checks what is there. Each
    /// address is run on its own, so the job can end partly done.
    /// </summary>
    public async Task<StateMigrationJob> StartMove(
        Guid moduleId,
        Guid organizationId,
        IReadOnlyCollection<AddressInstruction> instructions)
    {
        if (_resolvedConfigurationService is null || _bus is null)
            throw new InvalidOperationException(
                $"{nameof(StateMigrationService)} was constructed without the dependencies needed to start a job.");

        if (!_moduleSecuredRepository.CanConsent(moduleId, organizationId))
            throw new PrincipalNotAuthorizedException(
                $"Principal is not allowed to write state on Module with Id {moduleId}");

        if (instructions.Count == 0)
            throw new StateMigrationNotAllowedException("Name at least one address.");

        if (instructions.Any(i => string.IsNullOrWhiteSpace(i.Target)))
            throw new StateMigrationNotAllowedException("Every address in a move needs a target.");

        var blocked = await GetBlockedReason(moduleId, organizationId);
        if (blocked is not null)
            throw new StateMigrationNotAllowedException(blocked);

        await using var dbContext = await _dbContextFactory.CreateDbContextAsync();

        var job = new StateMigrationJob
        {
            Id = Guid.NewGuid(),
            ModuleId = moduleId,
            OrganizationId = organizationId,
            TimestampStart = DateTimeOffset.UtcNow,
            JobType = StateMigrationTypes.StateMove,
            Status = ExecutionStatus.Running
        };

        dbContext.StateMigrationJobs.Add(job);
        await dbContext.SaveChangesAsync();
        await AnnounceStarted(job);

        try
        {
            await _bus.Publish(new MoveJobRequested
            {
                CorrelationId = job.Id,
                Declared = await _resolvedConfigurationService.GetDeclared(moduleId, organizationId),
                Instructions = instructions.ToList()
            });
        }
        catch (Exception ex)
        {
            await FailJob(job.Id, organizationId, ex.Message);
            throw;
        }

        return job;
    }

    public async Task<StateMigrationJob> StartImport(
        Guid moduleId,
        Guid organizationId,
        IReadOnlyCollection<AddressInstruction> instructions)
    {
        if (_resolvedConfigurationService is null || _bus is null)
            throw new InvalidOperationException(
                $"{nameof(StateMigrationService)} was constructed without the dependencies needed to start a job.");

        if (!_moduleSecuredRepository.CanConsent(moduleId, organizationId))
            throw new PrincipalNotAuthorizedException(
                $"Principal is not allowed to write state on Module with Id {moduleId}");

        if (instructions.Count == 0)
            throw new StateMigrationNotAllowedException("Name at least one address.");

        if (instructions.Any(i => string.IsNullOrWhiteSpace(i.Target)))
            throw new StateMigrationNotAllowedException("Every address in an import needs a target.");

        var blocked = await GetBlockedReason(moduleId, organizationId);
        if (blocked is not null)
            throw new StateMigrationNotAllowedException(blocked);

        await using var dbContext = await _dbContextFactory.CreateDbContextAsync();

        var job = new StateMigrationJob
        {
            Id = Guid.NewGuid(),
            ModuleId = moduleId,
            OrganizationId = organizationId,
            TimestampStart = DateTimeOffset.UtcNow,
            JobType = StateMigrationTypes.StateImport,
            Status = ExecutionStatus.Running
        };

        dbContext.StateMigrationJobs.Add(job);
        await dbContext.SaveChangesAsync();
        await AnnounceStarted(job);

        try
        {
            await _bus.Publish(new ImportJobRequested
            {
                CorrelationId = job.Id,
                Declared = await _resolvedConfigurationService.GetDeclared(moduleId, organizationId),
                Instructions = instructions.ToList()
            });
        }
        catch (Exception ex)
        {
            await FailJob(job.Id, organizationId, ex.Message);
            throw;
        }

        return job;
    }

    public async Task<StateMigrationJob> StartRemove(
        Guid moduleId,
        Guid organizationId,
        IReadOnlyCollection<AddressInstruction> instructions)
    {
        if (_resolvedConfigurationService is null || _bus is null)
            throw new InvalidOperationException(
                $"{nameof(StateMigrationService)} was constructed without the dependencies needed to start a job.");

        if (!_moduleSecuredRepository.CanConsent(moduleId, organizationId))
            throw new PrincipalNotAuthorizedException(
                $"Principal is not allowed to write state on Module with Id {moduleId}");

        if (instructions.Count == 0)
            throw new StateMigrationNotAllowedException("Name at least one address.");

        var blocked = await GetBlockedReason(moduleId, organizationId);
        if (blocked is not null)
            throw new StateMigrationNotAllowedException(blocked);

        await using var dbContext = await _dbContextFactory.CreateDbContextAsync();

        var job = new StateMigrationJob
        {
            Id = Guid.NewGuid(),
            ModuleId = moduleId,
            OrganizationId = organizationId,
            TimestampStart = DateTimeOffset.UtcNow,
            JobType = StateMigrationTypes.StateRemove,
            Status = ExecutionStatus.Running
        };

        dbContext.StateMigrationJobs.Add(job);
        await dbContext.SaveChangesAsync();
        await AnnounceStarted(job);

        try
        {
            await _bus.Publish(new RemoveJobRequested
            {
                CorrelationId = job.Id,
                Declared = await _resolvedConfigurationService.GetDeclared(moduleId, organizationId),
                Instructions = instructions.ToList()
            });
        }
        catch (Exception ex)
        {
            await FailJob(job.Id, organizationId, ex.Message);
            throw;
        }

        return job;
    }


    public async Task<StateMigrationJob> StartSplitMigrate(
        Guid moduleId,
        Guid organizationId,
        string? rootDirectory,
        bool force,
        string? sourceRevision = null,
        bool rederiveBackend = false)
    {
        // A state migration only ever runs the configured branch; a ref belongs to a prove job.
        if (sourceRevision is not null)
            throw new StateMigrationNotAllowedException("A state migration runs the module's configured branch. Only a prove job can be given a ref.");

        if (_resolvedConfigurationService is null || _bus is null)
            throw new InvalidOperationException(
                $"{nameof(StateMigrationService)} was constructed without the dependencies needed to start a job.");

        var job = await Start(moduleId, organizationId, StateMigrationTypes.SplitMigrate);

        try
        {
            var declared = await _resolvedConfigurationService.GetDeclared(moduleId, organizationId);

            await _bus.Publish(new SplitMigrateRequested
            {
                CorrelationId = job.Id,
                Declared = declared,
                RootDirectory = rootDirectory,
                Force = force,
                RederiveBackend = rederiveBackend
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
    /// Starts a SplitProve job: the split's chain up to and including the proof, at an explicit
    /// ref, with nothing written. Paused like every manual job: the proof plans the module's real
    /// state, so an apply running alongside it would share the runner's working directory.
    /// </summary>
    public async Task<StateMigrationJob> StartSplitProve(
        Guid moduleId,
        Guid organizationId,
        string? rootDirectory,
        string sourceRevision)
    {
        if (!SourceRevisionOverride.IsValidRef(sourceRevision))
            throw new StateMigrationNotAllowedException($"'{sourceRevision}' is not a usable git ref.");

        if (_resolvedConfigurationService is null || _bus is null)
            throw new InvalidOperationException(
                $"{nameof(StateMigrationService)} was constructed without the dependencies needed to start a job.");

        var job = await Start(moduleId, organizationId, StateMigrationTypes.SplitProve);

        try
        {
            var declared = await _resolvedConfigurationService.GetDeclared(moduleId, organizationId, sourceRevision);

            await _bus.Publish(new SplitMigrateRequested
            {
                CorrelationId = job.Id,
                Declared = declared,
                RootDirectory = rootDirectory,
                Force = false,
                StopAfterProve = true
            });
        }
        catch (Exception ex)
        {
            await FailJob(job.Id, organizationId, ex.Message);
            throw;
        }

        return job;
    }

    /// <summary>
    /// Cancels a manual job. Guarded by the Pause verb rather than the deployment jobs' RunJob,
    /// matching how manual jobs are started and decided.
    /// </summary>
    public async Task Cancel(Guid jobId, Guid moduleId, Guid organizationId, CancellationType cancellationType)
    {
        if (_bus is null)
            throw new InvalidOperationException(
                $"{nameof(StateMigrationService)} was constructed without the dependencies needed to cancel a job.");

        if (!_moduleSecuredRepository.CanPause(moduleId, organizationId))
            throw new PrincipalNotAuthorizedException(
                $"Principal is not allowed to cancel manual jobs on Module with Id {moduleId}");

        await _bus.Publish(new CancelStateMigrationJobRequested
        {
            CorrelationId = jobId,
            OrganizationId = organizationId,
            CancellationType = cancellationType
        });
    }

    /// <summary>
    /// Records an approve or decline decision on a manual job and asks the saga to re-evaluate.
    /// The unique index on (job, principal) means one decision per principal; a second attempt
    /// surfaces as a refusal rather than silently replacing the first.
    /// </summary>
    public async Task Decide(Guid jobId, Guid moduleId, Guid organizationId, bool declined)
    {
        if (_bus is null)
            throw new InvalidOperationException(
                $"{nameof(StateMigrationService)} was constructed without the dependencies needed to record a decision.");

        if (!_moduleSecuredRepository.CanPause(moduleId, organizationId))
            throw new PrincipalNotAuthorizedException(
                $"Principal is not allowed to decide manual jobs on Module with Id {moduleId}");

        await using var dbContext = await _dbContextFactory.CreateDbContextAsync();

        var job = await dbContext.StateMigrationJobs
            .FirstOrDefaultAsync(j => j.Id == jobId && j.OrganizationId == organizationId);

        if (job is null)
            throw new EntityNotFoundException($"Manual job '{jobId}' not found");

        if (job.WaitingForApproval != true)
            throw new StateMigrationNotAllowedException("This job is not waiting for approval.");

        dbContext.StateMigrationJobApprovals.Add(new StateMigrationJobApproval
        {
            Id = Guid.NewGuid(),
            OrganizationId = organizationId,
            StateMigrationJobId = jobId,
            DecisionDateTime = DateTime.UtcNow,
            Declined = declined,
            PrincipalId = _moduleSecuredRepository.PrincipalProvider.GetSubject(organizationId),
            PrincipalDiscriminator = _moduleSecuredRepository.PrincipalProvider.GetPrincipalDiscriminator(),
            AgentId = _moduleSecuredRepository.PrincipalProvider.GetAgentId()
        });

        try
        {
            await dbContext.SaveChangesAsync();
        }
        catch (DbUpdateException)
        {
            // One decision per principal per job, enforced by the index rather than by this check.
            throw new StateMigrationNotAllowedException("You have already decided on this job.");
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

        var job = await dbContext.StateMigrationJobs
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

        dbContext.StateMigrationJobs.Add(new StateMigrationJob
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
