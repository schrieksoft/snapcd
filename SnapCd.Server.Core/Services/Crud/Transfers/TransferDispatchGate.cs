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

namespace SnapCd.Server.Core.Services.Crud.Transfers;

/// <summary>
/// Decides whether a Module's transfer step may still be sent to a runner. A receiver that has
/// refused or withdrawn is no longer party to the transfer, and the check happens at dispatch so a
/// step already in flight when consent changed stops at the server rather than touching its state.
/// </summary>
public class TransferDispatchGate
{
    private readonly IDbContextFactory<SnapCdDbContext> _dbContextFactory;

    public TransferDispatchGate(IDbContextFactory<SnapCdDbContext> dbContextFactory)
    {
        _dbContextFactory = dbContextFactory;
    }

    /// <summary>Throws when the Module may no longer be dispatched to.</summary>
    public async Task EnsureDispatchable(Guid moduleId, Guid organizationId)
    {
        await using var dbContext = await _dbContextFactory.CreateDbContextAsync();

        var transfer = await dbContext.Set<Transfer>().AsNoTracking()
            .FirstOrDefaultAsync(t =>
                t.OrganizationId == organizationId &&
                (t.SourceModuleId == moduleId || t.ReceiverModuleId == moduleId));

        if (transfer == null)
            throw new InvalidOperationException($"Module {moduleId} is not part of a transfer.");

        if (transfer.ReceiverModuleId == moduleId &&
            transfer.ReceiverConsentStatus is ConsentStatus.Refused or ConsentStatus.Revoked)
            throw new InvalidOperationException(
                $"The receiving Module has {transfer.ReceiverConsentStatus.ToString().ToLowerInvariant()} its consent.");
    }
}
