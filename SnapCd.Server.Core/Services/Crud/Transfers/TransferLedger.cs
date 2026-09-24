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

namespace SnapCd.Server.Core.Services.Crud.Transfers;

/// <summary>
/// The record of which resources a transfer has actually moved. Rows are opened by the run that
/// moves them and closed by whatever later observes the address where it should be, which need not
/// be the transfer: a state move run by hand closes the row without knowing one exists.
/// </summary>
public class TransferLedger(IDbContextFactory<SnapCdDbContext> dbContextFactory)
{
    /// <summary>
    /// Opens a row per address the transfer expects to move. Both Modules run at once and each
    /// reports the same addresses, so the second to report finds the rows already there.
    /// </summary>
    public async Task Open(
        Guid transferId,
        Guid organizationId,
        Guid leftModuleId,
        Guid arrivedModuleId,
        IReadOnlyCollection<string> addresses)
    {
        if (addresses.Count == 0) return;

        await using var dbContext = await dbContextFactory.CreateDbContextAsync();

        var existing = await dbContext.TransferObjects
            .Where(o => o.TransferId == transferId
                        && o.OrganizationId == organizationId
                        && addresses.Contains(o.Address))
            .ToDictionaryAsync(o => o.Address);

        foreach (var address in addresses.Distinct())
        {
            if (existing.ContainsKey(address)) continue;

            dbContext.TransferObjects.Add(new TransferObject
            {
                Id = Guid.NewGuid(),
                OrganizationId = organizationId,
                TransferId = transferId,
                Address = address,
                LeftModuleId = leftModuleId,
                ArrivedModuleId = arrivedModuleId
            });
        }

        await dbContext.SaveChangesAsync();
    }

    /// <summary>
    /// Accounts for what a job observed. An address seen in the Module expecting it has arrived;
    /// one absent from the Module giving it up has left. A row closes when both hold.
    /// </summary>
    /// <returns>The transfers whose rows this touched, for the caller to close if they are done.</returns>
    public async Task<IReadOnlyList<Guid>> Account(
        Guid moduleId,
        Guid organizationId,
        Guid jobId,
        IReadOnlyCollection<string> present,
        IReadOnlyCollection<string> absent)
    {
        if (present.Count == 0 && absent.Count == 0) return [];

        await using var dbContext = await dbContextFactory.CreateDbContextAsync();

        var seen = present.Concat(absent).Distinct().ToList();

        var rows = await dbContext.TransferObjects
            .Where(o => o.OrganizationId == organizationId
                        && seen.Contains(o.Address)
                        && (o.ArrivedModuleId == moduleId || o.LeftModuleId == moduleId)
                        && o.Transfer.ClosedAt == null)
            .ToListAsync();

        if (rows.Count == 0) return [];

        var now = DateTimeOffset.UtcNow;

        foreach (var row in rows)
        {
            if (row.ArrivedAt == null && row.ArrivedModuleId == moduleId && present.Contains(row.Address))
                row.ArrivedAt = now;

            if (row.LeftAt == null && row.LeftModuleId == moduleId && absent.Contains(row.Address))
                row.LeftAt = now;

            if (row.ArrivedAt != null && row.LeftAt != null && row.ResolvedByJobId == null)
                row.ResolvedByJobId = jobId;
        }

        await dbContext.SaveChangesAsync();

        return rows.Select(r => r.TransferId).Distinct().ToList();
    }

    /// <summary>
    /// Closes a transfer once nothing is left unaccounted for. Nobody is asked: a transfer whose
    /// resources all arrived has nothing left to decide.
    /// </summary>
    public async Task CloseIfAccountedFor(Guid transferId, Guid organizationId)
    {
        await using var dbContext = await dbContextFactory.CreateDbContextAsync();

        var transfer = await dbContext.Transfers
            .FirstOrDefaultAsync(t => t.Id == transferId && t.OrganizationId == organizationId);

        if (transfer is null || transfer.ClosedAt != null) return;

        var open = await dbContext.TransferObjects.AnyAsync(o =>
            o.TransferId == transferId
            && o.OrganizationId == organizationId
            && (o.ArrivedAt == null || o.LeftAt == null));

        if (open) return;

        transfer.ClosedAt = DateTimeOffset.UtcNow;
        transfer.CloseReason = "every resource accounted for";

        await dbContext.SaveChangesAsync();
    }
}
