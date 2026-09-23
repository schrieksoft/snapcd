// SPDX-License-Identifier: LicenseRef-Snap-CD-Source-Available-1.1
// Copyright (c) 2026 Karl Schriek / Schrieksoft.
// No license is granted to use this file, in whole or in part, (a) as training, fine-tuning, retrieval, or
// embedding data for any machine-learning model, or (b) as input to any machine-learning model, agent, or automated
// system for the purpose of producing a derivative work or reimplementation that is not otherwise permitted by the
// Snap CD Source-Available License (including any Competing Product as defined therein). Contact info@snapcd.io
// for terms covering either use.


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
/// Consent and the gates around it. A Transfer coordinates two Modules whose owners may be
/// different people, so every decision here is recorded against the principal who made it and the
/// map hash it was made against.
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
    /// recorded: this is the two Modules, the ref they run against, and who consented.
    /// </summary>
    public async Task<Transfer> Create(
        Guid sourceModuleId,
        Guid organizationId,
        Guid receiverModuleId,
        string? proveRef,
        TransferScope scope = TransferScope.Both)
    {
        if (!_moduleSecuredRepository.CanConsent(sourceModuleId, organizationId))
            throw new PrincipalNotAuthorizedException(
                $"Principal is not allowed to start a transfer from Module with Id {sourceModuleId}");

        if (receiverModuleId == sourceModuleId)
            throw new ManualJobNotAllowedException("A Module cannot transfer to itself.");

        await using var dbContext = await _dbContextFactory.CreateDbContextAsync();

        var transfer = new Transfer
        {
            Id = Guid.NewGuid(),
            OrganizationId = organizationId,
            SourceModuleId = sourceModuleId,
            ReceiverModuleId = receiverModuleId,
            Scope = scope,
            SourceProveRef = proveRef,
            ReceiverProveRef = proveRef,
            ReceiverConsentStatus = scope == TransferScope.SourceOnly
                ? ConsentStatus.NotRequired
                : ConsentStatus.Pending
        };

        dbContext.Transfers.Add(transfer);
        await dbContext.SaveChangesAsync();

        return transfer;
    }

    public async Task Decide(Guid transferId, Guid moduleId, Guid organizationId, bool granted, string? proveRef, string? reason)
    {
        await using var dbContext = await _dbContextFactory.CreateDbContextAsync();
        var transfer = await LoadTransfer(dbContext, transferId, organizationId);

        if (transfer.ReceiverModuleId != moduleId)
            throw new ManualJobNotAllowedException("Only the receiving Module answers a transfer's consent.");

        if (!_moduleSecuredRepository.CanConsent(moduleId, organizationId))
            throw new PrincipalNotAuthorizedException(
                $"Principal is not allowed to consent on behalf of Module with Id {moduleId}");

        if (transfer.ReceiverConsentStatus != ConsentStatus.Pending)
            throw new ManualJobNotAllowedException(
                $"This receiver's consent is {transfer.ReceiverConsentStatus}, not Pending.");

        transfer.ReceiverConsentStatus = granted ? ConsentStatus.Granted : ConsentStatus.Refused;
        RecordConsentPrincipal(transfer, organizationId);
        transfer.ReceiverConsentDecidedAt = DateTimeOffset.UtcNow;
        transfer.ReceiverConsentReason = reason;
        if (granted && !string.IsNullOrWhiteSpace(proveRef)) transfer.ReceiverProveRef = proveRef;

        await dbContext.SaveChangesAsync();

        await _bus.Publish(new ConsentDecided
        {
            TransferId = transferId,
            ModuleId = moduleId,
            OrganizationId = organizationId,
            Granted = granted,
            PrincipalId = transfer.ReceiverConsentPrincipalId!.Value
        });
    }

    /// <summary>
    /// Where the transfer stands, read from its jobs rather than stored. A transfer has at most one
    /// job per Module, so there is nothing to pick between and nothing to keep in step.
    /// </summary>
    public async Task<TransferStatus> DeriveStatus(Guid transferId, Guid organizationId)
    {
        await using var dbContext = await _dbContextFactory.CreateDbContextAsync();
        var transfer = await LoadTransfer(dbContext, transferId, organizationId);

        var jobs = await dbContext.ManualModuleJobs.AsNoTracking()
            .Where(j => j.TransferId == transferId && j.OrganizationId == organizationId)
            .Select(j => new { j.ModuleId, j.Status })
            .ToListAsync();

        if (jobs.Count == 0) return TransferStatus.Open;

        if (jobs.Any(j => j.Status == ExecutionStatus.Running)) return TransferStatus.Migrating;

        var landed = jobs.Count(j => j.Status == ExecutionStatus.Completed);

        return landed == jobs.Count ? TransferStatus.Migrated
            : landed > 0 ? TransferStatus.PartiallyCompleted
            : TransferStatus.Failed;
    }

    /// <summary>
    /// Puts the receiver's consent back to Pending, clearing any earlier answer. Receiving state
    /// is always the receiving side's own decision, whoever started the transfer.
    /// </summary>
    private static void AskTheReceiver(Transfer transfer)
    {
        transfer.ReceiverConsentStatus = ConsentStatus.Pending;
        transfer.ReceiverConsentPrincipalId = null;
        transfer.ReceiverConsentPrincipalDiscriminator = null;
        transfer.ReceiverConsentAgentId = null;
        transfer.ReceiverConsentDecidedAt = null;
        transfer.ReceiverConsentReason = null;
    }

    /// <summary>Who decided, in the same shape an approval records it: principal, kind, and agent.</summary>
    private void RecordConsentPrincipal(Transfer transfer, Guid organizationId)
    {
        transfer.ReceiverConsentPrincipalId = _principalProvider.GetSubject(organizationId);
        transfer.ReceiverConsentPrincipalDiscriminator = _principalProvider.GetPrincipalDiscriminator();
        transfer.ReceiverConsentAgentId = _principalProvider.GetAgentId();
    }

    /// <summary>
    /// Asks the receiver for its consent, once the map is known. The receiver is always asked:
    /// receiving state into a Module is that Module's own decision.
    /// </summary>
    public async Task AskReceiverFor(Guid transferId, Guid organizationId)
    {
        await using var dbContext = await _dbContextFactory.CreateDbContextAsync();
        var transfer = await LoadTransfer(dbContext, transferId, organizationId);

        AskTheReceiver(transfer);
        await dbContext.SaveChangesAsync();

        await _bus.Publish(new ConsentRequested
        {
            TransferId = transferId,
            ModuleId = transfer.ReceiverModuleId,
            OrganizationId = organizationId
        });
    }

    /// <summary>Every transfer this Module has been part of, newest first.</summary>
    public async Task<IReadOnlyList<Transfer>> ListForModule(Guid moduleId, Guid organizationId)
    {
        await using var dbContext = await _dbContextFactory.CreateDbContextAsync();

        return await dbContext.Transfers.AsNoTracking()
            .Where(t =>
                t.OrganizationId == organizationId &&
                (t.SourceModuleId == moduleId || t.ReceiverModuleId == moduleId))
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
