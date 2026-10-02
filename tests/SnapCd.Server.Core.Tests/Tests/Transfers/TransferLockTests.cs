// SPDX-License-Identifier: LicenseRef-Snap-CD-Source-Available-1.1
// Copyright (c) 2026 Karl Schriek / Schrieksoft.
// No license is granted to use this file, in whole or in part, (a) as training, fine-tuning, retrieval, or
// embedding data for any machine-learning model, or (b) as input to any machine-learning model, agent, or automated
// system for the purpose of producing a derivative work or reimplementation that is not otherwise permitted by the
// Snap CD Source-Available License (including any Competing Product as defined therein). Contact info@snapcd.io
// for terms covering either use.

using Microsoft.EntityFrameworkCore;
using SnapCd.Server.Core.Entities.Definition;
using SnapCd.Server.Core.Enums;
using SnapCd.Server.Core.Tests.Infrastructure;
using Xunit;

namespace SnapCd.Server.Core.Tests.Tests.Transfers;

/// <summary>
/// A Module is in at most one open transfer, in either role. No index on Transfers can say that,
/// because the pair lives in two columns of one row, so a trigger keeps TransferLocks true to
/// Transfers and its primary key is what refuses the second one.
/// </summary>
[Collection("NewRoleBasedSharedFixture")]
public class TransferLockTests : IAsyncLifetime
{
    private readonly Fixture _fixture;
    private Guid _a;
    private Guid _b;
    private Guid _c;
    private Guid _organizationId;
    private readonly List<Guid> _seeded = [];

    public TransferLockTests(Fixture fixture) => _fixture = fixture;

    public Task InitializeAsync()
    {
        _a = _fixture.Modules["0000"].Id;
        _b = _fixture.Modules["0001"].Id;
        _c = _fixture.Modules["0010"].Id;
        _organizationId = _fixture.Organizations["0"].Id;
        return Task.CompletedTask;
    }

    public async Task DisposeAsync()
    {
        await using var db = _fixture.CreateDbContext();
        await db.Transfers.Where(t => _seeded.Contains(t.Id)).ExecuteDeleteAsync();
    }

    [Fact]
    public async Task Opening_A_Transfer_Claims_Both_Modules()
    {
        await Open(_a, _b);

        await using var db = _fixture.CreateDbContext();
        var locked = await db.TransferLocks.AsNoTracking()
            .Where(l => l.OrganizationId == _organizationId)
            .Select(l => l.ModuleId)
            .ToListAsync();

        Assert.Contains(_a, locked);
        Assert.Contains(_b, locked);
    }

    /// <summary>The same pair again, which is the case two filtered indexes would also catch.</summary>
    [Fact]
    public async Task A_Module_Cannot_Start_A_Second_Transfer()
    {
        await Open(_a, _b);

        await Assert.ThrowsAnyAsync<DbUpdateException>(() => Open(_a, _c));
    }

    /// <summary>
    /// The case the indexes miss: A initiates one transfer and is the counterparty of another.
    /// The lock table has no notion of role, so it refuses this too.
    /// </summary>
    [Fact]
    public async Task A_Module_Cannot_Be_On_Both_Sides_Of_Two_Transfers()
    {
        await Open(_a, _b);

        await Assert.ThrowsAnyAsync<DbUpdateException>(() => Open(_c, _a));
    }

    /// <summary>Closing releases both, so a later transfer over the same Modules is allowed.</summary>
    [Fact]
    public async Task Closing_Releases_Both_Modules()
    {
        var transferId = await Open(_a, _b);

        await using (var db = _fixture.CreateDbContext())
        {
            var transfer = await db.Transfers.SingleAsync(t => t.Id == transferId);
            transfer.ClosedAt = DateTimeOffset.UtcNow;
            await db.SaveChangesAsync();
        }

        await using var check = _fixture.CreateDbContext();
        Assert.Empty(await check.TransferLocks.AsNoTracking()
            .Where(l => l.TransferId == transferId).ToListAsync());

        // Both are free again.
        await Open(_a, _b);
    }

    /// <summary>An unrelated pair is unaffected by another transfer being open.</summary>
    [Fact]
    public async Task Two_Transfers_Over_Different_Modules_Both_Open()
    {
        await Open(_a, _b);
        await Open(_c, _fixture.Modules["0011"].Id);

        await using var db = _fixture.CreateDbContext();
        Assert.Equal(2, await db.Transfers.AsNoTracking()
            .CountAsync(t => _seeded.Contains(t.Id) && t.ClosedAt == null));
    }

    private async Task<Guid> Open(Guid moduleId, Guid counterpartyModuleId)
    {
        var transferId = Guid.NewGuid();
        _seeded.Add(transferId);

        await using var db = _fixture.CreateDbContext();
        db.Transfers.Add(new Transfer
        {
            Id = transferId,
            OrganizationId = _organizationId,
            ModuleId = moduleId,
            CounterpartyModuleId = counterpartyModuleId,
            ConsentStatus = ConsentStatus.Pending
        });
        await db.SaveChangesAsync();

        return transferId;
    }
}
