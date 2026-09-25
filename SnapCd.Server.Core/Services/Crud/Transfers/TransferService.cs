// SPDX-License-Identifier: LicenseRef-Snap-CD-Source-Available-1.1
// Copyright (c) 2026 Karl Schriek / Schrieksoft.
// No license is granted to use this file, in whole or in part, (a) as training, fine-tuning, retrieval, or
// embedding data for any machine-learning model, or (b) as input to any machine-learning model, agent, or automated
// system for the purpose of producing a derivative work or reimplementation that is not otherwise permitted by the
// Snap CD Source-Available License (including any Competing Product as defined therein). Contact info@snapcd.io
// for terms covering either use.


using MassTransit;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using SnapCd.Contracts;
using SnapCd.Server.Core.Database;
using SnapCd.Server.Core.Entities.Definition;
using SnapCd.Server.Core.Enums;
using SnapCd.Server.Core.Events.System;
using SnapCd.Server.Core.Misc.Exceptions;
using SnapCd.Server.Core.Repositories.Organizations.Secured;
using SnapCd.Server.Core.Services.Crud.Jobs;
using SnapCd.Server.Core.Services.PrincipalProvider;

namespace SnapCd.Server.Core.Services.Crud.Transfers;

public class TransferServiceFactory(
    IDbContextFactory<SnapCdDbContext> dbFactory,
    ModuleSecuredRepositoryFactory moduleSecuredRepositoryFactory,
    ManualJobServiceFactory manualJobServiceFactory,
    IBus bus)
{
    public TransferService Create(IPrincipalProvider? principalProvider = null)
    {
        var provider = principalProvider ?? new HttpContextPrincipalProvider(new HttpContextAccessor());

        return new TransferService(
            dbFactory,
            moduleSecuredRepositoryFactory.Create(provider),
            manualJobServiceFactory.Create(provider),
            provider,
            bus);
    }
}

/// <summary>
/// The agreement between two Modules to move resources between their states. It starts each side's
/// job and records who agreed; it does not coordinate the two, which run independently.
/// </summary>
public class TransferService(
    IDbContextFactory<SnapCdDbContext> dbContextFactory,
    ModuleSecuredRepository moduleSecuredRepository,
    ManualJobService manualJobService,
    IPrincipalProvider principalProvider,
    IBus bus) : IDisposable
{
    /// <summary>
    /// Opens a transfer and asks the counterparty to agree. This Module's job starts straight
    /// away and waits for that answer as its first step, so the wait is visible on the page
    /// rather than being a state nothing shows. The counterparty is checked when it answers.
    /// </summary>
    public async Task<Transfer> Open(
        Guid moduleId,
        Guid counterpartyModuleId,
        Guid organizationId,
        string? moduleRef,
        bool startImmediately)
    {
        if (!moduleSecuredRepository.CanConsent(moduleId, organizationId))
            throw new PrincipalNotAuthorizedException(
                $"Principal is not allowed to start a transfer from Module with Id {moduleId}");

        if (counterpartyModuleId == moduleId)
            throw new ManualJobNotAllowedException("A Module cannot transfer to itself.");

        await using var dbContext = await dbContextFactory.CreateDbContextAsync();

        await EnsureModuleExists(dbContext, counterpartyModuleId, organizationId);

        var blocked = await manualJobService.GetBlockedReason(moduleId, organizationId);
        if (blocked is not null)
            throw new ManualJobNotAllowedException(blocked);

        var transfer = new Transfer
        {
            Id = Guid.NewGuid(),
            OrganizationId = organizationId,
            ModuleId = moduleId,
            CounterpartyModuleId = counterpartyModuleId,
            ModuleRef = moduleRef,
            ConsentStatus = ConsentStatus.Pending
        };

        dbContext.Transfers.Add(transfer);

        // The trigger on Transfers claims both Modules here; a Module already in an open transfer
        // fails on TransferLocks' primary key rather than on a check that could race.
        await SaveOrRefuse(dbContext, "One of these modules is already in a transfer.");

        await bus.Publish(new ConsentRequested
        {
            TransferId = transfer.Id,
            ModuleId = counterpartyModuleId,
            OrganizationId = organizationId
        });

        // The job starts now either way; unless told not to, its first step is the wait.
        await manualJobService.StartTransferMigrate(
            moduleId, counterpartyModuleId, organizationId, moduleRef, transfer.Id,
            awaitConsent: !startImmediately);

        return transfer;
    }

    /// <summary>
    /// Records the counterparty's answer and, on a yes, starts both sides. Refusing closes the
    /// transfer, which releases both Modules.
    /// </summary>
    public async Task Decide(
        Guid transferId,
        Guid moduleId,
        Guid organizationId,
        bool granted,
        string? moduleRef)
    {
        await using var dbContext = await dbContextFactory.CreateDbContextAsync();
        var transfer = await LoadTransfer(dbContext, transferId, organizationId);

        if (transfer.CounterpartyModuleId != moduleId)
            throw new ManualJobNotAllowedException("Only the counterparty answers a transfer.");

        if (!moduleSecuredRepository.CanConsent(moduleId, organizationId))
            throw new PrincipalNotAuthorizedException(
                $"Principal is not allowed to answer for Module with Id {moduleId}");

        if (transfer.ConsentStatus != ConsentStatus.Pending)
            throw new ManualJobNotAllowedException(
                $"This transfer has already been answered: {transfer.ConsentStatus}.");

        if (granted)
        {
            var blocked = await manualJobService.GetBlockedReason(moduleId, organizationId);
            if (blocked is not null)
                throw new ManualJobNotAllowedException(blocked);
        }

        transfer.ConsentStatus = granted ? ConsentStatus.Granted : ConsentStatus.Refused;
        transfer.ConsentPrincipalId = principalProvider.GetSubject(organizationId);
        transfer.ConsentPrincipalDiscriminator = principalProvider.GetPrincipalDiscriminator();
        transfer.ConsentDecidedAt = DateTimeOffset.UtcNow;
        transfer.CounterpartyRef = moduleRef;

        if (!granted)
        {
            transfer.ClosedAt = DateTimeOffset.UtcNow;
            transfer.ClosedBy = transfer.ConsentPrincipalId;
            transfer.CloseReason = "refused";
        }

        await dbContext.SaveChangesAsync();

        // This side has nothing running yet; the initiator's job is already waiting on the answer.
        if (granted)
            await manualJobService.StartTransferMigrate(
                transfer.CounterpartyModuleId, transfer.ModuleId, organizationId,
                transfer.CounterpartyRef, transfer.Id);

        // Releases the initiator, either to go ahead or to end.
        await bus.Publish(new ConsentDecided
        {
            TransferId = transfer.Id,
            ModuleId = moduleId,
            OrganizationId = organizationId,
            Granted = granted
        });
    }

    /// <summary>
    /// Closes a transfer once both sides have finished, releasing both Modules. A further attempt
    /// is a new transfer rather than a re-run of this one.
    /// </summary>
    public async Task CloseIfBothSidesEnded(Guid transferId, Guid organizationId)
    {
        await using var dbContext = await dbContextFactory.CreateDbContextAsync();

        var transfer = await dbContext.Transfers
            .FirstOrDefaultAsync(t => t.Id == transferId && t.OrganizationId == organizationId);

        if (transfer is null || transfer.ClosedAt != null) return;

        var jobs = await dbContext.ManualModuleJobs.AsNoTracking()
            .Where(j => j.TransferId == transferId && j.OrganizationId == organizationId)
            .Select(j => j.Status)
            .ToListAsync();

        if (jobs.Count < 2 || jobs.Any(s => s == ExecutionStatus.Running)) return;

        transfer.ClosedAt = DateTimeOffset.UtcNow;
        transfer.CloseReason = "both modules finished";

        await dbContext.SaveChangesAsync();
    }

    /// <summary>The open transfer this Module is in, in either role.</summary>
    public async Task<Transfer?> FindOpenForModule(Guid moduleId, Guid organizationId)
    {
        await using var dbContext = await dbContextFactory.CreateDbContextAsync();

        return await dbContext.Transfers.AsNoTracking()
            .FirstOrDefaultAsync(t =>
                t.OrganizationId == organizationId &&
                t.ClosedAt == null &&
                (t.ModuleId == moduleId || t.CounterpartyModuleId == moduleId));
    }

    /// <summary>
    /// The lock table's primary key is what refuses a Module already in a transfer, so the
    /// violation it raises is a refusal rather than a fault.
    /// </summary>
    private static async Task SaveOrRefuse(SnapCdDbContext dbContext, string message)
    {
        try
        {
            await dbContext.SaveChangesAsync();
        }
        catch (DbUpdateException)
        {
            throw new ManualJobNotAllowedException(message);
        }
    }

    private static async Task EnsureModuleExists(
        SnapCdDbContext dbContext, Guid moduleId, Guid organizationId)
    {
        var exists = await dbContext.Modules.AsNoTracking()
            .AnyAsync(m => m.Id == moduleId && m.OrganizationId == organizationId);

        if (!exists)
            throw new EntityNotFoundException($"Module '{moduleId}' not found");
    }

    private static async Task<Transfer> LoadTransfer(
        SnapCdDbContext dbContext, Guid transferId, Guid organizationId)
    {
        var transfer = await dbContext.Transfers
            .FirstOrDefaultAsync(t => t.Id == transferId && t.OrganizationId == organizationId);

        if (transfer is null)
            throw new EntityNotFoundException($"Transfer '{transferId}' not found");

        return transfer;
    }

    private static async Task<string> ModuleName(SnapCdDbContext dbContext, Guid moduleId) =>
        await dbContext.Modules.AsNoTracking()
            .Where(m => m.Id == moduleId)
            .Select(m => m.Name)
            .FirstOrDefaultAsync() ?? moduleId.ToString()[..8];

    public void Dispose() => moduleSecuredRepository.Dispose();
}
