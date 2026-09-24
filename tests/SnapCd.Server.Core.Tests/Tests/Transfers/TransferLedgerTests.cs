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
using SnapCd.Server.Core.Services.Crud.Transfers;
using SnapCd.Server.Core.Tests.Infrastructure;
using Xunit;

namespace SnapCd.Server.Core.Tests.Tests.Transfers;

/// <summary>
/// The ledger: one row per address the transfer moves, open until the address has left one Module
/// and been seen in the other. Anything that observes state closes rows, transfer or not.
/// </summary>
[Collection("NewRoleBasedSharedFixture")]
public class TransferLedgerTests : IAsyncLifetime
{
    private readonly Fixture _fixture;
    private Guid _moduleId;
    private Guid _counterpartyId;
    private Guid _organizationId;
    private readonly List<Guid> _seeded = [];

    public TransferLedgerTests(Fixture fixture) => _fixture = fixture;

    public Task InitializeAsync()
    {
        _moduleId = _fixture.Modules["0000"].Id;
        _counterpartyId = _fixture.Modules["0001"].Id;
        _organizationId = _fixture.Organizations["0"].Id;
        return Task.CompletedTask;
    }

    public async Task DisposeAsync()
    {
        await using var db = _fixture.CreateDbContext();
        await db.TransferObjects.Where(o => _seeded.Contains(o.TransferId)).ExecuteDeleteAsync();
        await db.Transfers.Where(t => _seeded.Contains(t.Id)).ExecuteDeleteAsync();
    }

    [Fact]
    public async Task A_Run_Opens_One_Row_Per_Address()
    {
        var transferId = await SeedTransfer();
        var ledger = Ledger();

        await ledger.Open(transferId, _organizationId, _moduleId, _counterpartyId,
            ["random_pet.a", "random_pet.b"]);

        var rows = await Rows(transferId);
        Assert.Equal(2, rows.Count);
        Assert.All(rows, r => Assert.Equal(_moduleId, r.LeftModuleId));
        Assert.All(rows, r => Assert.Equal(_counterpartyId, r.ArrivedModuleId));
        Assert.All(rows, r => Assert.Null(r.LeftAt));
        Assert.All(rows, r => Assert.Null(r.ArrivedAt));
    }

    /// <summary>Both Modules report the same addresses, so the second to report adds nothing.</summary>
    [Fact]
    public async Task The_Counterparty_Reporting_The_Same_Addresses_Adds_No_Rows()
    {
        var transferId = await SeedTransfer();
        var ledger = Ledger();

        await ledger.Open(transferId, _organizationId, _moduleId, _counterpartyId, ["random_pet.a"]);
        await ledger.Open(transferId, _organizationId, _moduleId, _counterpartyId, ["random_pet.a"]);

        Assert.Single(await Rows(transferId));
    }

    [Fact]
    public async Task Both_Modules_Accounting_Closes_The_Rows_And_The_Transfer()
    {
        var transferId = await SeedTransfer();
        var ledger = Ledger();
        var jobId = Guid.NewGuid();

        await ledger.Open(transferId, _organizationId, _moduleId, _counterpartyId, ["random_pet.a"]);

        await ledger.Account(_moduleId, _organizationId, jobId, [], ["random_pet.a"]);
        await ledger.Account(_counterpartyId, _organizationId, jobId, ["random_pet.a"], []);
        await ledger.CloseIfAccountedFor(transferId, _organizationId);

        var row = Assert.Single(await Rows(transferId));
        Assert.NotNull(row.LeftAt);
        Assert.NotNull(row.ArrivedAt);
        Assert.Equal(jobId, row.ResolvedByJobId);

        await using var db = _fixture.CreateDbContext();
        var transfer = await db.Transfers.AsNoTracking().SingleAsync(t => t.Id == transferId);
        Assert.NotNull(transfer.ClosedAt);
    }

    /// <summary>The half-landed case: the source gave the address up, the receiver never saw it.</summary>
    [Fact]
    public async Task One_Module_Landing_Leaves_The_Row_And_The_Transfer_Open()
    {
        var transferId = await SeedTransfer();
        var ledger = Ledger();

        await ledger.Open(transferId, _organizationId, _moduleId, _counterpartyId, ["random_pet.a"]);

        await ledger.Account(_moduleId, _organizationId, Guid.NewGuid(), [], ["random_pet.a"]);
        await ledger.CloseIfAccountedFor(transferId, _organizationId);

        var row = Assert.Single(await Rows(transferId));
        Assert.NotNull(row.LeftAt);
        Assert.Null(row.ArrivedAt);
        Assert.Null(row.ResolvedByJobId);

        await using var db = _fixture.CreateDbContext();
        var transfer = await db.Transfers.AsNoTracking().SingleAsync(t => t.Id == transferId);
        Assert.Null(transfer.ClosedAt);
    }

    /// <summary>
    /// A later job accounting for the address closes it, which is what lets the ledger outlive the
    /// run that opened it.
    /// </summary>
    [Fact]
    public async Task A_Later_Job_Closes_What_The_Run_Left_Open()
    {
        var transferId = await SeedTransfer();
        var ledger = Ledger();

        await ledger.Open(transferId, _organizationId, _moduleId, _counterpartyId, ["random_pet.a"]);
        await ledger.Account(_moduleId, _organizationId, Guid.NewGuid(), [], ["random_pet.a"]);

        var laterJobId = Guid.NewGuid();
        await ledger.Account(_counterpartyId, _organizationId, laterJobId, ["random_pet.a"], []);
        await ledger.CloseIfAccountedFor(transferId, _organizationId);

        var row = Assert.Single(await Rows(transferId));
        Assert.Equal(laterJobId, row.ResolvedByJobId);

        await using var db = _fixture.CreateDbContext();
        Assert.NotNull((await db.Transfers.AsNoTracking().SingleAsync(t => t.Id == transferId)).ClosedAt);
    }

    /// <summary>
    /// The remedy for a half-landed transfer: a second run over the module that missed it. The rows
    /// outlive the run that opened them, so the second one closes what the first left.
    /// </summary>
    [Fact]
    public async Task A_Second_Run_Closes_What_The_First_Left_Open()
    {
        var transferId = await SeedTransfer();
        var ledger = Ledger();

        // First run: the source gave the address up, the counterparty never took it on.
        await ledger.Open(transferId, _organizationId, _moduleId, _counterpartyId, ["random_pet.a"]);
        await ledger.Account(_moduleId, _organizationId, Guid.NewGuid(), [], ["random_pet.a"]);
        await ledger.CloseIfAccountedFor(transferId, _organizationId);

        await using (var db = _fixture.CreateDbContext())
            Assert.Null((await db.Transfers.AsNoTracking().SingleAsync(t => t.Id == transferId)).ClosedAt);

        // Second run over the counterparty alone, under the same transfer.
        await ledger.Open(transferId, _organizationId, _moduleId, _counterpartyId, ["random_pet.a"]);
        await ledger.Account(_counterpartyId, _organizationId, Guid.NewGuid(), ["random_pet.a"], []);
        await ledger.CloseIfAccountedFor(transferId, _organizationId);

        var row = Assert.Single(await Rows(transferId));
        Assert.NotNull(row.LeftAt);
        Assert.NotNull(row.ArrivedAt);

        await using var final = _fixture.CreateDbContext();
        Assert.NotNull((await final.Transfers.AsNoTracking().SingleAsync(t => t.Id == transferId)).ClosedAt);
    }

    /// <summary>
    /// What a state list observes is what closes the ledger. The consumer path is the same one a
    /// state mv run by hand takes, which is what lets the ledger outlive the transfer's own run.
    /// </summary>
    [Fact]
    public async Task What_A_State_Check_Found_Closes_The_Ledger()
    {
        var transferId = await SeedTransfer();
        var ledger = Ledger();
        var jobId = Guid.NewGuid();

        await ledger.Open(transferId, _organizationId, _moduleId, _counterpartyId, ["random_pet.a"]);
        await ledger.Account(_moduleId, _organizationId, Guid.NewGuid(), [], ["random_pet.a"]);

        // What a StateListFiltered job on the counterparty would report having found.
        var touched = await ledger.Account(
            _counterpartyId, _organizationId, jobId, ["random_pet.a"], []);

        foreach (var id in touched)
            await ledger.CloseIfAccountedFor(id, _organizationId);

        var row = Assert.Single(await Rows(transferId));
        Assert.NotNull(row.ArrivedAt);
        Assert.Equal(jobId, row.ResolvedByJobId);

        await using var db = _fixture.CreateDbContext();
        Assert.NotNull((await db.Transfers.AsNoTracking().SingleAsync(t => t.Id == transferId)).ClosedAt);
    }

    /// <summary>An address nobody is waiting for is not the ledger's business.</summary>
    [Fact]
    public async Task An_Address_No_Transfer_Expects_Closes_Nothing()
    {
        var transferId = await SeedTransfer();
        var ledger = Ledger();

        await ledger.Open(transferId, _organizationId, _moduleId, _counterpartyId, ["random_pet.a"]);

        var touched = await ledger.Account(
            _moduleId, _organizationId, Guid.NewGuid(), [], ["random_pet.unrelated"]);

        Assert.Empty(touched);
        Assert.Null(Assert.Single(await Rows(transferId)).LeftAt);
    }

    private TransferLedger Ledger() => new(new FixtureDbContextFactory(_fixture));

    private async Task<Guid> SeedTransfer()
    {
        var transferId = Guid.NewGuid();
        _seeded.Add(transferId);

        await using var db = _fixture.CreateDbContext();
        db.Transfers.Add(new Transfer
        {
            Id = transferId,
            OrganizationId = _organizationId,
            ModuleId = _moduleId,
            CounterpartyModuleId = _counterpartyId,
            ConsentStatus = ConsentStatus.Granted
        });
        await db.SaveChangesAsync();
        return transferId;
    }

    private async Task<List<TransferObject>> Rows(Guid transferId)
    {
        await using var db = _fixture.CreateDbContext();
        return await db.TransferObjects.AsNoTracking()
            .Where(o => o.TransferId == transferId)
            .OrderBy(o => o.Address)
            .ToListAsync();
    }

    private sealed class FixtureDbContextFactory(Fixture fixture) : IDbContextFactory<SnapCdDbContext>
    {
        public SnapCdDbContext CreateDbContext() => fixture.CreateDbContext();
    }
}
