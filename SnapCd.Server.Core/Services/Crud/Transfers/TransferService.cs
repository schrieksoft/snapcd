// SPDX-License-Identifier: LicenseRef-Snap-CD-Source-Available-1.1
// Copyright (c) 2026 Karl Schriek / Schrieksoft.
// No license is granted to use this file, in whole or in part, (a) as training, fine-tuning, retrieval, or
// embedding data for any machine-learning model, or (b) as input to any machine-learning model, agent, or automated
// system for the purpose of producing a derivative work or reimplementation that is not otherwise permitted by the
// Snap CD Source-Available License (including any Competing Product as defined therein). Contact info@snapcd.io
// for terms covering either use.


using SnapCd.Contracts.Enums;
using System.Security.Cryptography;
using System.Text;
using MassTransit;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using SnapCd.Server.Core.Database;
using SnapCd.Server.Core.Entities.Definition;
using SnapCd.Server.Core.Enums;
using SnapCd.Server.Core.Events.System;
using SnapCd.Server.Core.Misc.Exceptions;
using SnapCd.Server.Core.Repositories.Organizations.Secured;
using SnapCd.Server.Core.Services.PrincipalProvider;

namespace SnapCd.Server.Core.Services.Crud.Transfers;

public class TransferServiceFactory(
    IDbContextFactory<SnapCdDbContext> dbFactory,
    ModuleSecuredRepositoryFactory moduleSecuredRepositoryFactory,
    IBus bus)
{
    public TransferService Create(IPrincipalProvider? principalProvider = null)
    {
        var provider = principalProvider ?? new HttpContextPrincipalProvider(new HttpContextAccessor());
        return new TransferService(dbFactory, moduleSecuredRepositoryFactory.Create(provider), provider, bus);
    }
}

/// <summary>
/// Consent and the gates around it. A Transfer covers two Modules whose owners may be different
/// people, so every decision here is recorded against the principal who made it.
/// </summary>
public class TransferService : IDisposable
{
    private readonly IDbContextFactory<SnapCdDbContext> _dbContextFactory;
    private readonly ModuleSecuredRepository _moduleSecuredRepository;
    private readonly IPrincipalProvider _principalProvider;
    private readonly IBus _bus;

    public TransferService(
        IDbContextFactory<SnapCdDbContext> dbContextFactory,
        ModuleSecuredRepository moduleSecuredRepository,
        IPrincipalProvider principalProvider,
        IBus bus)
    {
        _dbContextFactory = dbContextFactory;
        _moduleSecuredRepository = moduleSecuredRepository;
        _principalProvider = principalProvider;
        _bus = bus;
    }

    /// <summary>
    /// Records the receiver's answer. Only a principal who could pause the receiving Module may
    /// answer for it, and only while it is still Pending: a second answer is refused rather than
    /// silently replacing the first.
    /// </summary>
    /// <summary>
    /// Opens a transfer between two Modules. What moves is in the code, so nothing about the map is
    /// recorded: this is the two Modules and the counterparty's decision.
    /// </summary>
    public async Task<Transfer> Create(Guid moduleId, Guid organizationId, Guid counterpartyModuleId)
    {
        if (!_moduleSecuredRepository.CanConsent(moduleId, organizationId))
            throw new PrincipalNotAuthorizedException(
                $"Principal is not allowed to start a transfer from Module with Id {moduleId}");

        if (counterpartyModuleId == moduleId)
            throw new ManualJobNotAllowedException("A Module cannot transfer to itself.");

        await using var dbContext = await _dbContextFactory.CreateDbContextAsync();

        var transfer = new Transfer
        {
            Id = Guid.NewGuid(),
            OrganizationId = organizationId,
            ModuleId = moduleId,
            CounterpartyModuleId = counterpartyModuleId,
            ConsentStatus = ConsentStatus.Pending
        };

        dbContext.Transfers.Add(transfer);
        await dbContext.SaveChangesAsync();

        await _bus.Publish(new ConsentRequested
        {
            TransferId = transfer.Id,
            ModuleId = counterpartyModuleId,
            OrganizationId = organizationId
        });

        return transfer;
    }

    /// <summary>
    /// Records the counterparty's answer. Its state is written too, so the decision is its owners'.
    /// Answered once: consent holds until a run starts and means nothing after.
    /// </summary>
    public async Task Decide(
        Guid transferId, Guid moduleId, Guid organizationId, bool granted, string? reason)
    {
        await using var dbContext = await _dbContextFactory.CreateDbContextAsync();
        var transfer = await LoadTransfer(dbContext, transferId, organizationId);

        if (transfer.CounterpartyModuleId != moduleId)
            throw new ManualJobNotAllowedException("Only the counterparty answers a transfer's consent.");

        if (!_moduleSecuredRepository.CanConsent(moduleId, organizationId))
            throw new PrincipalNotAuthorizedException(
                $"Principal is not allowed to consent on behalf of Module with Id {moduleId}");

        if (transfer.ConsentStatus != ConsentStatus.Pending)
            throw new ManualJobNotAllowedException(
                $"This transfer's consent is {transfer.ConsentStatus}, not Pending.");

        transfer.ConsentStatus = granted ? ConsentStatus.Granted : ConsentStatus.Refused;
        transfer.ConsentPrincipalId = _principalProvider.GetSubject(organizationId);
        transfer.ConsentPrincipalDiscriminator = _principalProvider.GetPrincipalDiscriminator();
        transfer.ConsentAgentId = _principalProvider.GetAgentId();
        transfer.ConsentDecidedAt = DateTimeOffset.UtcNow;
        transfer.ConsentReason = reason;

        await dbContext.SaveChangesAsync();

        await _bus.Publish(new ConsentDecided
        {
            TransferId = transferId,
            ModuleId = moduleId,
            OrganizationId = organizationId,
            Granted = granted,
            PrincipalId = transfer.ConsentPrincipalId!.Value
        });
    }

    /// <summary>
    /// Ends the transfer. From here its resources are no longer watched, so one of them being
    /// deleted later is not mistaken for a move that never landed.
    /// </summary>
    public async Task Close(Guid transferId, Guid organizationId, string? reason)
    {
        await using var dbContext = await _dbContextFactory.CreateDbContextAsync();
        var transfer = await LoadTransfer(dbContext, transferId, organizationId);

        if (!_moduleSecuredRepository.CanConsent(transfer.ModuleId, organizationId))
            throw new PrincipalNotAuthorizedException(
                $"Principal is not allowed to close transfer '{transferId}'");

        if (transfer.ClosedAt != null) return;

        var running = await dbContext.ManualModuleJobs.AnyAsync(j =>
            j.OrganizationId == organizationId
            && j.Status == ExecutionStatus.Running
            && dbContext.TransferRuns.Any(r => r.Id == j.TransferRunId && r.TransferId == transferId));

        if (running)
            throw new ManualJobNotAllowedException("A job is running for this transfer; cancel it first.");

        var open = await dbContext.TransferObjects.CountAsync(o =>
            o.TransferId == transferId
            && o.OrganizationId == organizationId
            && (o.LeftAt == null || o.ArrivedAt == null));

        // Closing over unaccounted resources is allowed, because a person may have resolved them
        // by hand - but they have to say so, since nothing will watch them afterwards.
        if (open > 0 && string.IsNullOrWhiteSpace(reason))
            throw new ManualJobNotAllowedException(
                $"{open} of this transfer's resources are still unaccounted for. Closing needs a reason.");

        transfer.ClosedAt = DateTimeOffset.UtcNow;
        transfer.ClosedBy = _principalProvider.GetSubject(organizationId);
        transfer.ClosedByPrincipalDiscriminator = _principalProvider.GetPrincipalDiscriminator();
        transfer.CloseReason = reason;

        await dbContext.SaveChangesAsync();
    }

    /// <summary>Starts an attempt. Its scope and refs are its own; the consent is the transfer's.</summary>
    public async Task<TransferRun> StartRun(
        Guid transferId,
        Guid organizationId,
        TransferScope scope,
        string? moduleRef,
        string? counterpartyRef)
    {
        await using var dbContext = await _dbContextFactory.CreateDbContextAsync();
        var transfer = await LoadTransfer(dbContext, transferId, organizationId);

        if (!_moduleSecuredRepository.CanConsent(transfer.ModuleId, organizationId))
            throw new PrincipalNotAuthorizedException(
                $"Principal is not allowed to run transfer '{transferId}'");

        if (transfer.ClosedAt != null)
            throw new ManualJobNotAllowedException("This transfer is closed.");

        // Consent covers the counterparty's state, so a run that leaves it alone does not need it.
        if (scope is TransferScope.Both or TransferScope.CounterpartyOnly
            && transfer.ConsentStatus is not (ConsentStatus.Granted or ConsentStatus.NotRequired))
            throw new ManualJobNotAllowedException(
                $"The counterparty's consent is {transfer.ConsentStatus}; it must be granted before its state is written.");

        var inFlight = await dbContext.ManualModuleJobs.AnyAsync(j =>
            j.OrganizationId == organizationId
            && j.Status == ExecutionStatus.Running
            && dbContext.TransferRuns.Any(r => r.Id == j.TransferRunId && r.TransferId == transferId));

        if (inFlight)
            throw new ManualJobNotAllowedException("This transfer already has a run in progress.");

        var run = new TransferRun
        {
            Id = Guid.NewGuid(),
            OrganizationId = organizationId,
            TransferId = transferId,
            Scope = scope,
            Ref = moduleRef,
            CounterpartyRef = counterpartyRef,
            StartedAt = DateTimeOffset.UtcNow
        };


        dbContext.TransferRuns.Add(run);
        await dbContext.SaveChangesAsync();

        return run;
    }

    /// <summary>
    /// Where the transfer stands, read from its runs' jobs rather than stored.
    /// </summary>
    public async Task<TransferStatus> DeriveStatus(Guid transferId, Guid organizationId)
    {
        await using var dbContext = await _dbContextFactory.CreateDbContextAsync();
        var transfer = await LoadTransfer(dbContext, transferId, organizationId);

        if (transfer.ClosedAt != null) return TransferStatus.Migrated;

        var jobs = await dbContext.ManualModuleJobs.AsNoTracking()
            .Where(j => j.OrganizationId == organizationId
                        && dbContext.TransferRuns.Any(r => r.Id == j.TransferRunId && r.TransferId == transferId))
            .Select(j => j.Status)
            .ToListAsync();

        if (jobs.Count == 0) return TransferStatus.Open;
        if (jobs.Any(x => x == ExecutionStatus.Running)) return TransferStatus.Migrating;

        var landed = jobs.Count(x => x == ExecutionStatus.Completed);

        return landed == jobs.Count ? TransferStatus.Migrated
            : landed > 0 ? TransferStatus.PartiallyCompleted
            : TransferStatus.Failed;
    }

    /// <summary>
    /// Whether a run still has work in flight. Read from its jobs rather than stored, so a run that
    /// fell over is finished rather than pending forever.
    /// </summary>
    public async Task<bool> IsRunning(Guid runId, Guid organizationId)
    {
        await using var dbContext = await _dbContextFactory.CreateDbContextAsync();

        return await dbContext.ManualModuleJobs.AnyAsync(j =>
            j.TransferRunId == runId
            && j.OrganizationId == organizationId
            && j.Status == ExecutionStatus.Running);
    }

    /// <summary>
    /// Resources this transfer still has not accounted for: gone from one Module and not yet seen
    /// in the other.
    /// </summary>
    public async Task<int> CountOpenObjects(Guid transferId, Guid organizationId)
    {
        await using var dbContext = await _dbContextFactory.CreateDbContextAsync();

        return await dbContext.TransferObjects.CountAsync(o =>
            o.TransferId == transferId
            && o.OrganizationId == organizationId
            && (o.LeftAt == null || o.ArrivedAt == null));
    }

    /// <summary>
    /// The addresses this Module is still expected to be holding: ones the transfer moved into it
    /// that have not been seen there. What a check asks about.
    /// </summary>
    public async Task<IReadOnlyList<string>> OpenAddressesFor(
        Guid transferId, Guid moduleId, Guid organizationId)
    {
        await using var dbContext = await _dbContextFactory.CreateDbContextAsync();

        return await dbContext.TransferObjects.AsNoTracking()
            .Where(o => o.TransferId == transferId
                        && o.OrganizationId == organizationId
                        && (o.ArrivedModuleId == moduleId && o.ArrivedAt == null
                            || o.LeftModuleId == moduleId && o.LeftAt == null))
            .Select(o => o.Address)
            .Distinct()
            .ToListAsync();
    }

    /// <summary>The transfer this Module is in, if any: one that has not been closed.</summary>
    public async Task<Transfer?> FindOpenForModule(Guid moduleId, Guid organizationId)
    {
        await using var dbContext = await _dbContextFactory.CreateDbContextAsync();

        return await dbContext.Transfers.AsNoTracking()
            .Include(t => t.Runs)
            .FirstOrDefaultAsync(t =>
                t.OrganizationId == organizationId &&
                t.ClosedAt == null &&
                (t.ModuleId == moduleId || t.CounterpartyModuleId == moduleId));
    }

    /// <summary>Every transfer this Module has been part of, newest first.</summary>
    public async Task<IReadOnlyList<Transfer>> ListForModule(Guid moduleId, Guid organizationId)
    {
        await using var dbContext = await _dbContextFactory.CreateDbContextAsync();

        return await dbContext.Transfers.AsNoTracking()
            .Include(t => t.Runs)
            .Where(t =>
                t.OrganizationId == organizationId &&
                (t.ModuleId == moduleId || t.CounterpartyModuleId == moduleId))
            .OrderByDescending(t => t.CreatedDateTime)
            .ToListAsync();
    }

    private static async Task<Transfer> LoadTransfer(SnapCdDbContext dbContext, Guid transferId, Guid organizationId)
    {
        var transfer = await dbContext.Transfers
            .FirstOrDefaultAsync(t => t.Id == transferId && t.OrganizationId == organizationId);

        if (transfer == null)
            throw new EntityNotFoundException($"Transfer '{transferId}' not found");

        return transfer;
    }

    public void Dispose()
    {
        _moduleSecuredRepository.Dispose();
    }
}
