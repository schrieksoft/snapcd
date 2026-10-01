// SPDX-License-Identifier: LicenseRef-Snap-CD-Source-Available-1.1
// Copyright (c) 2026 Karl Schriek / Schrieksoft.
// No license is granted to use this file, in whole or in part, (a) as training, fine-tuning, retrieval, or
// embedding data for any machine-learning model, or (b) as input to any machine-learning model, agent, or automated
// system for the purpose of producing a derivative work or reimplementation that is not otherwise permitted by the
// Snap CD Source-Available License (including any Competing Product as defined therein). Contact info@snapcd.io
// for terms covering either use.
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using SnapCd.Contracts;
using SnapCd.Server.Core.Database;
using SnapCd.Server.Core.Entities.Definition;
using SnapCd.Server.Core.Enums;
using SnapCd.Server.Core.Services;
using SnapCd.Server.Core.Services.Crud.Transfers;
using SnapCd.Server.Core.Settings;
using SnapCd.Server.Core.Tests.Infrastructure;
using Xunit;

namespace SnapCd.Server.Core.Tests.Tests.Transfers;

/// <summary>
/// A fragment is raw state, so it is encrypted at rest like state and deleted when the job that
/// needed it ends. Steps are the only progress record, so attempts accumulate rather than overwrite.
/// </summary>
[Collection("NewRoleBasedSharedFixture")]
public class TransferArtefactAndStepTests : IAsyncLifetime
{
    private readonly Fixture _fixture;
    private Guid _moduleId;
    private Guid _counterpartyModuleId;
    private Guid _organizationId;
    private readonly List<Guid> _seededJobs = [];
    private readonly List<Guid> _seededTransfers = [];

    public TransferArtefactAndStepTests(Fixture fixture) => _fixture = fixture;

    public Task InitializeAsync()
    {
        _moduleId = _fixture.Modules["0000"].Id;
        _counterpartyModuleId = _fixture.Modules["0001"].Id;
        _organizationId = _fixture.Organizations["0"].Id;
        return Task.CompletedTask;
    }

    public async Task DisposeAsync()
    {
        await using var db = _fixture.CreateDbContext();
        await db.TransferArtefacts.Where(a => _seededTransfers.Contains(a.TransferId)).ExecuteDeleteAsync();
        await db.Transfers.Where(t => _seededTransfers.Contains(t.Id)).ExecuteDeleteAsync();
        await db.StateMigrationJobSteps.Where(s => _seededJobs.Contains(s.JobId)).ExecuteDeleteAsync();
        await db.StateMigrationJobs.Where(j => _seededJobs.Contains(j.Id)).ExecuteDeleteAsync();
    }

    [Fact]
    public async Task The_Fragment_Round_Trips()
    {
        var transferId = await SeedTransfer();
        var service = ArtefactService();

        await service.StoreSourceFragment(transferId, _organizationId, "{\"serial\":7}", "map_hash: abc");

        var (fragment, meta) = await service.ReadSourceFragment(transferId, _organizationId);

        Assert.Equal("{\"serial\":7}", fragment);
        Assert.Equal("map_hash: abc", meta);
    }

    [Fact]
    public async Task The_Outputs_Round_Trip()
    {
        var transferId = await SeedTransfer();
        var service = ArtefactService();

        await service.StoreReceiverOutputs(transferId, _organizationId, "outputs:\n  dns: zone");

        Assert.Equal("outputs:\n  dns: zone", await service.ReadReceiverOutputs(transferId, _organizationId));
    }

    /// <summary>A fragment carries whatever state carries, so it never sits in the table in clear.</summary>
    [Fact]
    public async Task An_Artefact_Is_Encrypted_At_Rest()
    {
        var transferId = await SeedTransfer();
        await ArtefactService()
            .StoreSourceFragment(transferId, _organizationId, "super-secret-password", "meta");

        await using var db = _fixture.CreateDbContext();
        var row = await db.TransferArtefacts.AsNoTracking().SingleAsync(a => a.TransferId == transferId);

        Assert.DoesNotContain("super-secret-password", row.SourceFragmentCiphertext);
    }

    /// <summary>A half that runs again overwrites what it produced before.</summary>
    [Fact]
    public async Task Storing_Twice_Replaces_Rather_Than_Accumulates()
    {
        var transferId = await SeedTransfer();
        var service = ArtefactService();

        await service.StoreSourceFragment(transferId, _organizationId, "first", "m1");
        await service.StoreSourceFragment(transferId, _organizationId, "second", "m2");

        var (fragment, _) = await service.ReadSourceFragment(transferId, _organizationId);

        Assert.Equal("second", fragment);
        Assert.Equal(1, await CountRows(transferId));
    }

    [Fact]
    public async Task Reading_What_The_Source_Never_Produced_Is_Null()
    {
        var transferId = await SeedTransfer();
        var (fragment, meta) = await ArtefactService().ReadSourceFragment(transferId, _organizationId);

        Assert.Null(fragment);
        Assert.Null(meta);
        Assert.Null(await ArtefactService().ReadReceiverOutputs(transferId, _organizationId));
    }

    /// <summary>The gate the receiver waits on.</summary>
    [Fact]
    public async Task HasSourceFragment_Is_False_Until_The_Source_Writes_One()
    {
        var transferId = await SeedTransfer();
        var service = ArtefactService();

        Assert.False(await service.HasSourceFragment(transferId, _organizationId));

        await service.StoreReceiverOutputs(transferId, _organizationId, "outputs only");
        Assert.False(await service.HasSourceFragment(transferId, _organizationId));

        await service.StoreSourceFragment(transferId, _organizationId, "state", "meta");
        Assert.True(await service.HasSourceFragment(transferId, _organizationId));
    }

    /// <summary>Nothing a transfer carries outlives the transfer.</summary>
    [Fact]
    public async Task Closing_Deletes_What_Was_Carried()
    {
        var transferId = await SeedTransfer();
        var service = ArtefactService();

        await service.StoreSourceFragment(transferId, _organizationId, "a", "m");
        await service.StoreReceiverOutputs(transferId, _organizationId, "b");

        Assert.Equal(1, await service.DeleteForTransfer(transferId, _organizationId));

        var (fragment, _) = await service.ReadSourceFragment(transferId, _organizationId);
        Assert.Null(fragment);
    }

    private async Task<int> CountRows(Guid transferId)
    {
        await using var db = _fixture.CreateDbContext();
        return await db.TransferArtefacts.CountAsync(a => a.TransferId == transferId);
    }

    private async Task<Guid> SeedTransfer()
    {
        var transferId = Guid.NewGuid();
        _seededTransfers.Add(transferId);

        await using var db = _fixture.CreateDbContext();
        db.Transfers.Add(new Transfer
        {
            Id = transferId,
            OrganizationId = _organizationId,
            ModuleId = _moduleId,
            CounterpartyModuleId = _counterpartyModuleId,
            ConsentStatus = ConsentStatus.Granted,
            // Closed on creation: the locks are what a transfer claims, and these tests only need
            // the row to hang artefacts from.
            ClosedAt = DateTimeOffset.UtcNow,
            CloseReason = "seeded for a test"
        });
        await db.SaveChangesAsync();
        return transferId;
    }

    private async Task<Guid> SeedJob()
    {
        var jobId = Guid.NewGuid();
        _seededJobs.Add(jobId);

        await using var db = _fixture.CreateDbContext();
        db.StateMigrationJobs.Add(new StateMigrationJob
        {
            Id = jobId,
            ModuleId = _moduleId,
            OrganizationId = _organizationId,
            TimestampStart = DateTimeOffset.UtcNow,
            JobType = StateMigrationTypes.TransferProve,
            Status = ExecutionStatus.Running
        });
        await db.SaveChangesAsync();
        return jobId;
    }

    private TransferArtefactService ArtefactService() =>
        new(DbContextFactory(), new StateEncryptionService(Options.Create(new StateStoreSettings
        {
            EncryptionKey = Convert.ToBase64String(new byte[32])
        })));

    private StateMigrationStepService StepService() => new(DbContextFactory());

    private IDbContextFactory<SnapCdDbContext> DbContextFactory()
    {
        var services = new ServiceCollection();
        services.AddDbContextFactory<SnapCdDbContext>(o => o.UseSqlServer(_fixture.ConnectionString));
        return services.BuildServiceProvider().GetRequiredService<IDbContextFactory<SnapCdDbContext>>();
    }
}
