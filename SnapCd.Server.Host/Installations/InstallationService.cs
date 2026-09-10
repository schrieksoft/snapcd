// SPDX-License-Identifier: LicenseRef-Snap-CD-Source-Available-1.1
// Copyright (c) 2026 Karl Schriek / Schrieksoft.
// No license is granted to use this file, in whole or in part, (a) as training, fine-tuning, retrieval, or
// embedding data for any machine-learning model, or (b) as input to any machine-learning model, agent, or automated
// system for the purpose of producing a derivative work or reimplementation that is not otherwise permitted by the
// Snap CD Source-Available License (including any Competing Product as defined therein). Contact info@snapcd.io
// for terms covering either use.


using Microsoft.EntityFrameworkCore;
using SnapCd.Server.Core.Database;

namespace SnapCd.Server.Host.Installations;

/// <summary>The singleton installation row. It is inserted by the migration that creates the table, so absence is an error.</summary>
public class InstallationService(IDbContextFactory<SnapCdDbContext> dbContextFactory)
{
    public async Task<Installation> GetAsync(CancellationToken ct = default)
    {
        await using var db = await dbContextFactory.CreateDbContextAsync(ct);
        return await db.Set<Installation>().SingleOrDefaultAsync(i => i.Id == Installation.SingletonId, ct)
               ?? throw new InvalidOperationException("The installation row is missing; the database was not migrated.");
    }

    public async Task UpdateAsync(Action<Installation> apply, CancellationToken ct = default)
    {
        await using var db = await dbContextFactory.CreateDbContextAsync(ct);
        var row = await db.Set<Installation>().SingleAsync(i => i.Id == Installation.SingletonId, ct);
        apply(row);
        await db.SaveChangesAsync(ct);
    }
}
