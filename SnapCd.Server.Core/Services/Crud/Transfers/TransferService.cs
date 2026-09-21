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
    /// The transfer's identity: the sha256 of the map's bytes, lowercase hex, matching what
    /// demonolith computes over the same file. Hashed as given rather than re-serialised, because a
    /// round-trip through a serialiser would not reproduce the bytes the runners see.
    /// </summary>
    public static string HashMap(string mapYaml) =>
        Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(mapYaml))).ToLowerInvariant();

    /// <summary>
    /// Opens a Transfer between two Modules. The source consents by initiating; the receiver is
    /// asked, or auto-consented when the initiator could have answered for it.
    /// </summary>
    public async Task<Transfer> Create(
        Guid sourceModuleId,
        Guid organizationId,
        string mapYaml,
        Guid receiverModuleId,
        string? proveRef)
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
            MapJson = mapYaml,
            MapHash = HashMap(mapYaml),
            Status = TransferStatus.Open,
            SourceProveRef = proveRef,
            ReceiverProveRef = proveRef
        };

        AskTheReceiver(transfer);
        dbContext.Transfers.Add(transfer);
        await dbContext.SaveChangesAsync();

        if (transfer.ReceiverConsentStatus == ConsentStatus.Pending)
            await _bus.Publish(new ConsentRequested
            {
                TransferId = transfer.Id,
                ModuleId = receiverModuleId,
                OrganizationId = organizationId
            });

        return transfer;
    }

    /// <summary>
    /// Records the receiver's answer. Only a principal who could pause the receiving Module may
    /// answer for it, and only while it is still Pending: a second answer is refused rather than
    /// silently replacing the first.
    /// </summary>
    public async Task Decide(Guid transferId, Guid moduleId, Guid organizationId, bool granted, string? proveRef, string? reason)
    {
        await using var dbContext = await _dbContextFactory.CreateDbContextAsync();
        var transfer = await LoadTransfer(dbContext, transferId, organizationId);

        RequireReceiver(transfer, moduleId);

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
    /// Changes the ref a side wants proved. Allowed until it locks, because after that the ref is
    /// what the merge was declared against.
    /// </summary>
    public async Task SetProveRef(Guid transferId, Guid moduleId, Guid organizationId, string proveRef)
    {
        if (!_moduleSecuredRepository.CanConsent(moduleId, organizationId))
            throw new PrincipalNotAuthorizedException(
                $"Principal is not allowed to set the prove ref for Module with Id {moduleId}");

        await using var dbContext = await _dbContextFactory.CreateDbContextAsync();
        var transfer = await LoadTransfer(dbContext, transferId, organizationId);
        var receiver = IsReceiver(transfer, moduleId);

        if (LockedAt(transfer, receiver) != null)
            throw new ManualJobNotAllowedException("This Module has locked; its ref can no longer change.");

        if (receiver) transfer.ReceiverProveRef = proveRef;
        else transfer.SourceProveRef = proveRef;

        await dbContext.SaveChangesAsync();
    }

    /// <summary>
    /// Withdraws a consent already given. Free before the receiver locked, because nothing was
    /// held; afterwards its hold becomes a pause.
    /// </summary>
    public async Task Revoke(Guid transferId, Guid moduleId, Guid organizationId, string? reason)
    {
        if (!_moduleSecuredRepository.CanConsent(moduleId, organizationId))
            throw new PrincipalNotAuthorizedException(
                $"Principal is not allowed to revoke consent for Module with Id {moduleId}");

        await using var dbContext = await _dbContextFactory.CreateDbContextAsync();
        var transfer = await LoadTransfer(dbContext, transferId, organizationId);

        RequireReceiver(transfer, moduleId);

        if (transfer.ReceiverConsentStatus != ConsentStatus.Granted)
            throw new ManualJobNotAllowedException(
                $"This receiver's consent is {transfer.ReceiverConsentStatus}; there is nothing to revoke.");

        if (transfer.Status == TransferStatus.Migrating)
            throw new ManualJobNotAllowedException("A migrate is running; consent cannot be revoked until it ends.");

        var wasHeld = transfer.ReceiverLockedAt != null;

        transfer.ReceiverConsentStatus = ConsentStatus.Revoked;
        RecordConsentPrincipal(transfer, organizationId);
        transfer.ReceiverConsentDecidedAt = DateTimeOffset.UtcNow;
        transfer.ReceiverConsentReason = reason;

        if (wasHeld)
        {
            transfer.ReceiverLockedAt = null;
            transfer.ReceiverLockedBy = null;
            transfer.ReceiverReleasedAt = DateTimeOffset.UtcNow;
        }

        await dbContext.SaveChangesAsync();

        await _bus.Publish(new ConsentDecided
        {
            TransferId = transferId,
            ModuleId = moduleId,
            OrganizationId = organizationId,
            Granted = false,
            PrincipalId = transfer.ReceiverConsentPrincipalId!.Value
        });

        if (wasHeld)
            await _bus.Publish(new ModuleWithdrawnFromTransfer
            {
                ModuleId = moduleId,
                OrganizationId = organizationId,
                TransferId = transferId,
                Reason = reason ?? "consent withdrawn after locking; state not migrated"
            });
    }

    /// <summary>
    /// Replaces the map. Consent is keyed to the hash it was given against, so a changed map asks
    /// the receiver again; if it had locked, its hold becomes a pause rather than lifting.
    /// </summary>
    public async Task<IReadOnlyList<Guid>> ReplaceMap(Guid transferId, Guid organizationId, string mapYaml, string mapHash)
    {
        await using var dbContext = await _dbContextFactory.CreateDbContextAsync();
        var transfer = await LoadTransfer(dbContext, transferId, organizationId);

        if (!_moduleSecuredRepository.CanConsent(transfer.SourceModuleId, organizationId))
            throw new PrincipalNotAuthorizedException(
                $"Principal is not allowed to edit transfer '{transferId}'");

        if (transfer.Status == TransferStatus.Migrating)
            throw new ManualJobNotAllowedException("A migrate is running; the map cannot change until it ends.");

        if (transfer.MapHash == mapHash) return [];

        transfer.MapJson = mapYaml;
        transfer.MapHash = mapHash;
        transfer.Status = TransferStatus.Open;

        var released = new List<Guid>();
        if (transfer.ReceiverLockedAt != null)
        {
            transfer.ReceiverLockedAt = null;
            transfer.ReceiverLockedBy = null;
            transfer.ReceiverMergedDeclaredAt = null;
            transfer.ReceiverMergedDeclaredBy = null;
            transfer.ReceiverMergedCommit = null;
            released.Add(transfer.ReceiverModuleId);
        }

        var asked = AskTheReceiver(transfer);

        await dbContext.SaveChangesAsync();

        if (asked)
            await _bus.Publish(new ConsentRequested
            {
                TransferId = transferId,
                ModuleId = transfer.ReceiverModuleId,
                OrganizationId = organizationId
            });

        // A receiver that had merged against the old map is in the same position as a withdrawal:
        // its code moved but its state did not, so its hold becomes a pause rather than lifting.
        foreach (var moduleId in released)
            await _bus.Publish(new ModuleWithdrawnFromTransfer
            {
                ModuleId = moduleId,
                OrganizationId = organizationId,
                TransferId = transferId,
                Reason = "the transfer's map changed after this Module locked; state not migrated"
            });

        return released;
    }

    /// <summary>
    /// Takes a side's hold, before its merge lands. From here its ordinary triggers park, including
    /// the merge's own: that is the point, since merged code is unsafe to apply until the state
    /// moves with it.
    /// </summary>
    public async Task Lock(Guid transferId, Guid moduleId, Guid organizationId)
    {
        if (!_moduleSecuredRepository.CanConsent(moduleId, organizationId))
            throw new PrincipalNotAuthorizedException(
                $"Principal is not allowed to lock Module with Id {moduleId}");

        await using var dbContext = await _dbContextFactory.CreateDbContextAsync();
        var transfer = await LoadTransfer(dbContext, transferId, organizationId);
        var receiver = IsReceiver(transfer, moduleId);

        if (receiver && transfer.ReceiverConsentStatus != ConsentStatus.Granted)
            throw new ManualJobNotAllowedException(
                $"This receiver's consent is {transfer.ReceiverConsentStatus}; it cannot lock.");

        if (LockedAt(transfer, receiver) != null) return;

        var now = DateTimeOffset.UtcNow;
        var by = _principalProvider.GetSubject(organizationId);
        var discriminator = _principalProvider.GetPrincipalDiscriminator();
        if (receiver)
        {
            transfer.ReceiverLockedAt = now;
            transfer.ReceiverLockedBy = by;
            transfer.ReceiverLockedByPrincipalDiscriminator = discriminator;
            transfer.ReceiverReleasedAt = null;
        }
        else
        {
            transfer.SourceLockedAt = now;
            transfer.SourceLockedBy = by;
            transfer.SourceLockedByPrincipalDiscriminator = discriminator;
            transfer.SourceReleasedAt = null;
        }

        await dbContext.SaveChangesAsync();

        await _bus.Publish(new ModuleHoldRequested
        {
            ModuleId = moduleId,
            OrganizationId = organizationId,
            TransferId = transferId
        });
    }

    /// <summary>
    /// Declares the proved code is on the configured branch, naming the merge commit. Migrate
    /// verifies that ancestry after GetModule, so a wrong commit here fails the job rather than
    /// pushing against code nobody proved.
    /// </summary>
    public async Task DeclareMerged(Guid transferId, Guid moduleId, Guid organizationId, string mergedCommit)
    {
        if (!_moduleSecuredRepository.CanConsent(moduleId, organizationId))
            throw new PrincipalNotAuthorizedException(
                $"Principal is not allowed to declare Module with Id {moduleId} merged");

        if (string.IsNullOrWhiteSpace(mergedCommit))
            throw new ArgumentException("A merge declaration needs the commit it landed as.", nameof(mergedCommit));

        await using var dbContext = await _dbContextFactory.CreateDbContextAsync();
        var transfer = await LoadTransfer(dbContext, transferId, organizationId);
        var receiver = IsReceiver(transfer, moduleId);

        if (LockedAt(transfer, receiver) == null)
            throw new ManualJobNotAllowedException("Lock this Module before declaring it merged.");

        var now = DateTimeOffset.UtcNow;
        var by = _principalProvider.GetSubject(organizationId);
        var discriminator = _principalProvider.GetPrincipalDiscriminator();
        if (receiver)
        {
            transfer.ReceiverMergedDeclaredAt = now;
            transfer.ReceiverMergedDeclaredBy = by;
            transfer.ReceiverMergedDeclaredByPrincipalDiscriminator = discriminator;
            transfer.ReceiverMergedCommit = mergedCommit;
        }
        else
        {
            transfer.SourceMergedDeclaredAt = now;
            transfer.SourceMergedDeclaredBy = by;
            transfer.SourceMergedDeclaredByPrincipalDiscriminator = discriminator;
            transfer.SourceMergedCommit = mergedCommit;
        }

        await dbContext.SaveChangesAsync();
    }

    /// <summary>
    /// Releases a side whose slice has landed. Unlike a withdrawal this is a true release: its code
    /// and state agree again, so ordinary work may resume.
    /// </summary>
    public async Task ReleaseLanded(Guid transferId, Guid moduleId, Guid organizationId)
    {
        await using var dbContext = await _dbContextFactory.CreateDbContextAsync();
        var transfer = await LoadTransfer(dbContext, transferId, organizationId);
        var receiver = IsReceiver(transfer, moduleId);

        if (receiver)
        {
            transfer.ReceiverLockedAt = null;
            transfer.ReceiverLockedBy = null;
            transfer.ReceiverReleasedAt = DateTimeOffset.UtcNow;
        }
        else
        {
            transfer.SourceLockedAt = null;
            transfer.SourceLockedBy = null;
            transfer.SourceReleasedAt = DateTimeOffset.UtcNow;
        }

        await dbContext.SaveChangesAsync();

        await _bus.Publish(new ModuleReleaseRequested
        {
            ModuleId = moduleId,
            OrganizationId = organizationId,
            TransferId = transferId
        });
    }

    /// <summary>
    /// Recomputes a Transfer's status from its own gates and the jobs run under it. Status is
    /// derived rather than declared, so a column that changes cannot leave it stale.
    /// </summary>
    public async Task<TransferStatus> DeriveStatus(Guid transferId, Guid organizationId)
    {
        await using var dbContext = await _dbContextFactory.CreateDbContextAsync();
        var transfer = await LoadTransfer(dbContext, transferId, organizationId);

        if (transfer.Status is TransferStatus.Migrated or TransferStatus.Abandoned)
            return transfer.Status;

        var derived = Derive(transfer, await MigrateIsRunning(dbContext, transfer));

        if (derived != transfer.Status)
        {
            transfer.Status = derived;
            await dbContext.SaveChangesAsync();
        }

        return derived;
    }

    private static TransferStatus Derive(Transfer transfer, bool migrateRunning)
    {
        if (migrateRunning) return TransferStatus.Migrating;

        // A revoked or refused consent cannot launch a Migrate, but does not close the Transfer:
        // the initiator may re-map or get the receiver back.
        if (transfer.ReceiverConsentStatus != ConsentStatus.Granted) return TransferStatus.Open;

        if (transfer.SourceMergedDeclaredAt != null && transfer.ReceiverMergedDeclaredAt != null)
            return TransferStatus.ReadyToMigrate;

        if (transfer.SourceLockedAt != null || transfer.ReceiverLockedAt != null)
            return TransferStatus.Merging;

        return TransferStatus.Proving;
    }

    private static async Task<bool> MigrateIsRunning(SnapCdDbContext dbContext, Transfer transfer)
    {
        return await dbContext.ManualModuleJobs.AnyAsync(j =>
            j.OrganizationId == transfer.OrganizationId
            && (j.ModuleId == transfer.SourceModuleId || j.ModuleId == transfer.ReceiverModuleId)
            && j.JobType == ManualJobTypes.TransferMigrate
            && j.Status == ExecutionStatus.Running);
    }

    /// <summary>
    /// Converts a side's hold into an ordinary pause. After a merge the Module's code and its state
    /// disagree until the migration lands, so releasing the hold is never a return to normal: the
    /// next ordinary plan would destroy or duplicate the moved resources.
    /// </summary>
    public async Task WithdrawParticipant(Guid transferId, Guid moduleId, Guid organizationId, string reason)
    {
        await using var dbContext = await _dbContextFactory.CreateDbContextAsync();
        var transfer = await LoadTransfer(dbContext, transferId, organizationId);
        var receiver = IsReceiver(transfer, moduleId);

        if (receiver)
        {
            transfer.ReceiverLockedAt = null;
            transfer.ReceiverLockedBy = null;
            transfer.ReceiverReleasedAt = DateTimeOffset.UtcNow;
        }
        else
        {
            transfer.SourceLockedAt = null;
            transfer.SourceLockedBy = null;
            transfer.SourceReleasedAt = DateTimeOffset.UtcNow;
        }

        await dbContext.SaveChangesAsync();

        await _bus.Publish(new ModuleWithdrawnFromTransfer
        {
            ModuleId = moduleId,
            OrganizationId = organizationId,
            TransferId = transferId,
            Reason = reason
        });
    }

    /// <summary>
    /// Closes a Transfer the initiator no longer wants. Allowed only while no Migrate is running;
    /// remaining holds become pauses, and nothing already pushed is revoked.
    /// </summary>
    public async Task<IReadOnlyList<Guid>> Abandon(Guid transferId, Guid organizationId, string reason)
    {
        await using var dbContext = await _dbContextFactory.CreateDbContextAsync();
        var transfer = await LoadTransfer(dbContext, transferId, organizationId);

        if (!_moduleSecuredRepository.CanConsent(transfer.SourceModuleId, organizationId))
            throw new PrincipalNotAuthorizedException(
                $"Principal is not allowed to abandon transfer '{transferId}'");

        if (await MigrateIsRunning(dbContext, transfer))
            throw new ManualJobNotAllowedException("A migrate is running; abort it before abandoning the transfer.");

        var held = new List<Guid>();
        var now = DateTimeOffset.UtcNow;

        if (transfer.SourceLockedAt != null)
        {
            transfer.SourceLockedAt = null;
            transfer.SourceLockedBy = null;
            transfer.SourceReleasedAt = now;
            held.Add(transfer.SourceModuleId);
        }

        if (transfer.ReceiverLockedAt != null)
        {
            transfer.ReceiverLockedAt = null;
            transfer.ReceiverLockedBy = null;
            transfer.ReceiverReleasedAt = now;
            held.Add(transfer.ReceiverModuleId);
        }

        // Holds are converted before the Transfer closes. A crash between the two leaves an open
        // Transfer whose Modules are released, which the next status derivation corrects; the other
        // order would leave a closed Transfer holding Modules that nothing owns.
        await dbContext.SaveChangesAsync();

        foreach (var moduleId in held)
            await _bus.Publish(new ModuleWithdrawnFromTransfer
            {
                ModuleId = moduleId,
                OrganizationId = organizationId,
                TransferId = transferId,
                Reason = reason
            });

        transfer.Status = TransferStatus.Abandoned;
        transfer.ClosedAt = now;
        transfer.CloseReason = reason;
        await dbContext.SaveChangesAsync();

        return held;
    }

    /// <summary>
    /// Sets the receiver's consent, granting it outright when the asker could have answered it
    /// themselves. Returns whether the receiver still has to be asked.
    /// </summary>
    private bool AskTheReceiver(Transfer transfer)
    {
        if (_moduleSecuredRepository.CanConsent(transfer.ReceiverModuleId, transfer.OrganizationId))
        {
            // The person could have clicked the button, so the click is implied.
            transfer.ReceiverConsentStatus = ConsentStatus.Granted;
            RecordConsentPrincipal(transfer, transfer.OrganizationId);
            transfer.ReceiverConsentDecidedAt = DateTimeOffset.UtcNow;
            return false;
        }

        transfer.ReceiverConsentStatus = ConsentStatus.Pending;
        transfer.ReceiverConsentPrincipalId = null;
        transfer.ReceiverConsentPrincipalDiscriminator = null;
        transfer.ReceiverConsentAgentId = null;
        transfer.ReceiverConsentDecidedAt = null;
        transfer.ReceiverConsentReason = null;
        return true;
    }

    /// <summary>Who decided, in the same shape an approval records it: principal, kind, and agent.</summary>
    private void RecordConsentPrincipal(Transfer transfer, Guid organizationId)
    {
        transfer.ReceiverConsentPrincipalId = _principalProvider.GetSubject(organizationId);
        transfer.ReceiverConsentPrincipalDiscriminator = _principalProvider.GetPrincipalDiscriminator();
        transfer.ReceiverConsentAgentId = _principalProvider.GetAgentId();
    }

    private static bool IsReceiver(Transfer transfer, Guid moduleId)
    {
        if (moduleId == transfer.ReceiverModuleId) return true;
        if (moduleId == transfer.SourceModuleId) return false;
        throw new EntityNotFoundException(
            $"Module '{moduleId}' does not take part in transfer '{transfer.Id}'");
    }

    private static void RequireReceiver(Transfer transfer, Guid moduleId)
    {
        if (!IsReceiver(transfer, moduleId))
            throw new ManualJobNotAllowedException(
                "The source consents by initiating the transfer; only the receiver is asked.");
    }

    private static DateTimeOffset? LockedAt(Transfer transfer, bool receiver) =>
        receiver ? transfer.ReceiverLockedAt : transfer.SourceLockedAt;

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
