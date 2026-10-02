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

    // ----- trg_StateMigrationJobs_DeleteTransferArtefacts -----
    //
    // The artefacts go when neither of the transfer's jobs is still running. That is later than the
    // close, which fires on the first job ending, so the negative case below is the one that
    // matters: the surviving half still needs what the first produced.

    /// <summary>Both halves ended, so nothing is left to read the files.</summary>
    [Fact]
    public async Task Both_Halves_Ended_Deletes_The_Artefacts()
    {
        var transferId = await SeedTransfer();
        var source = await SeedTransferJob(transferId, _moduleId, ExecutionStatus.Running);
        var receiver = await SeedTransferJob(transferId, _counterpartyModuleId, ExecutionStatus.Running);

        await ArtefactService().StoreSourceFragment(transferId, _organizationId, "state", "meta");
        Assert.Equal(1, await CountRows(transferId));

        await SetStatus(source, ExecutionStatus.Completed);
        Assert.Equal(1, await CountRows(transferId));

        await SetStatus(receiver, ExecutionStatus.Completed);
        Assert.Equal(0, await CountRows(transferId));
    }

    /// <summary>
    /// The first half ending must not take the files with it: the other half is still running and
    /// the fragment is what it is waiting for.
    /// </summary>
    [Fact]
    public async Task One_Half_Still_Running_Keeps_The_Artefacts()
    {
        var transferId = await SeedTransfer();
        var source = await SeedTransferJob(transferId, _moduleId, ExecutionStatus.Running);
        await SeedTransferJob(transferId, _counterpartyModuleId, ExecutionStatus.Running);

        await ArtefactService().StoreSourceFragment(transferId, _organizationId, "state", "meta");

        await SetStatus(source, ExecutionStatus.Completed);

        Assert.Equal(1, await CountRows(transferId));
    }

    /// <summary>Ended is ended: a failed half counts, and a retry is a new transfer either way.</summary>
    [Fact]
    public async Task A_Failed_Half_Still_Counts_As_Ended()
    {
        var transferId = await SeedTransfer();
        var source = await SeedTransferJob(transferId, _moduleId, ExecutionStatus.Running);
        var receiver = await SeedTransferJob(transferId, _counterpartyModuleId, ExecutionStatus.Running);

        await ArtefactService().StoreSourceFragment(transferId, _organizationId, "state", "meta");

        await SetStatus(source, ExecutionStatus.Completed);
        await SetStatus(receiver, ExecutionStatus.Failed);

        Assert.Equal(0, await CountRows(transferId));
    }

    /// <summary>
    /// A job row can disappear rather than end: deleting a Module cascades to its jobs. Without the
    /// trigger's DELETE arm those artefacts would never be removed.
    /// </summary>
    [Fact]
    public async Task A_Deleted_Job_Row_Also_Releases_The_Artefacts()
    {
        var transferId = await SeedTransfer();
        var source = await SeedTransferJob(transferId, _moduleId, ExecutionStatus.Completed);
        var receiver = await SeedTransferJob(transferId, _counterpartyModuleId, ExecutionStatus.Running);

        await ArtefactService().StoreSourceFragment(transferId, _organizationId, "state", "meta");
        Assert.Equal(1, await CountRows(transferId));

        await using (var db = _fixture.CreateDbContext())
        {
            await db.StateMigrationJobs
                .Where(j => j.Id == receiver && j.OrganizationId == _organizationId)
                .ExecuteDeleteAsync();
        }

        Assert.Equal(0, await CountRows(transferId));
        Assert.NotEqual(Guid.Empty, source);
    }

    /// <summary>A job on no transfer must not reach another transfer's artefacts.</summary>
    [Fact]
    public async Task A_Job_Without_A_Transfer_Deletes_Nothing()
    {
        var transferId = await SeedTransfer();
        await SeedTransferJob(transferId, _moduleId, ExecutionStatus.Running);
        await ArtefactService().StoreSourceFragment(transferId, _organizationId, "state", "meta");

        // A third Module: at most one Running job per Module, so it cannot share one with a half.
        var unrelated = await SeedTransferJob(null, _fixture.Modules["0010"].Id, ExecutionStatus.Running);
        await SetStatus(unrelated, ExecutionStatus.Completed);

        Assert.Equal(1, await CountRows(transferId));
    }

    private async Task<Guid> SeedTransferJob(Guid? transferId, Guid moduleId, ExecutionStatus status)
    {
        var jobId = Guid.NewGuid();
        _seededJobs.Add(jobId);

        await using var db = _fixture.CreateDbContext();
        db.StateMigrationJobs.Add(new StateMigrationJob
        {
            Id = jobId,
            ModuleId = moduleId,
            OrganizationId = _organizationId,
            TransferId = transferId,
            TimestampStart = DateTimeOffset.UtcNow,
            JobType = StateMigrationTypes.TransferMigrate,
            Status = status
        });
        await db.SaveChangesAsync();
        return jobId;
    }

    private async Task SetStatus(Guid jobId, ExecutionStatus status)
    {
        await using var db = _fixture.CreateDbContext();
        var job = await db.StateMigrationJobs
            .FirstAsync(j => j.Id == jobId && j.OrganizationId == _organizationId);
        job.Status = status;
        job.TimestampEnd = DateTimeOffset.UtcNow;
        await db.SaveChangesAsync();
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
