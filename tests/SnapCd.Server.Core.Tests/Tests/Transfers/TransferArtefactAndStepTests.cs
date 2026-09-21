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
    private Guid _organizationId;
    private readonly List<Guid> _seededJobs = [];

    public TransferArtefactAndStepTests(Fixture fixture) => _fixture = fixture;

    public Task InitializeAsync()
    {
        _moduleId = _fixture.Modules["0000"].Id;
        _organizationId = _fixture.Organizations["0"].Id;
        return Task.CompletedTask;
    }

    public async Task DisposeAsync()
    {
        await using var db = _fixture.CreateDbContext();
        await db.ManualModuleJobArtefacts.Where(a => _seededJobs.Contains(a.JobId)).ExecuteDeleteAsync();
        await db.ManualModuleJobSteps.Where(s => _seededJobs.Contains(s.JobId)).ExecuteDeleteAsync();
        await db.ManualModuleJobs.Where(j => _seededJobs.Contains(j.Id)).ExecuteDeleteAsync();
    }

    [Fact]
    public async Task An_Artefact_Round_Trips()
    {
        var jobId = await SeedJob();
        var service = ArtefactService();

        await service.Store(jobId, _organizationId, "fragment-app.tfstate", "{\"serial\":7}");

        Assert.Equal("{\"serial\":7}", await service.Read(jobId, _organizationId, "fragment-app.tfstate"));
    }

    /// <summary>A fragment carries whatever state carries, so it never sits in the table in clear.</summary>
    [Fact]
    public async Task An_Artefact_Is_Encrypted_At_Rest()
    {
        var jobId = await SeedJob();
        await ArtefactService().Store(jobId, _organizationId, "fragment-app.tfstate", "super-secret-password");

        await using var db = _fixture.CreateDbContext();
        var row = await db.ManualModuleJobArtefacts.AsNoTracking().SingleAsync(a => a.JobId == jobId);

        Assert.DoesNotContain("super-secret-password", row.Ciphertext);
    }

    [Fact]
    public async Task Storing_The_Same_Name_Twice_Replaces_It()
    {
        var jobId = await SeedJob();
        var service = ArtefactService();

        await service.Store(jobId, _organizationId, "outputs-app.yaml", "first");
        await service.Store(jobId, _organizationId, "outputs-app.yaml", "second");

        Assert.Equal("second", await service.Read(jobId, _organizationId, "outputs-app.yaml"));

        await using var db = _fixture.CreateDbContext();
        Assert.Equal(1, await db.ManualModuleJobArtefacts.CountAsync(a => a.JobId == jobId));
    }

    [Fact]
    public async Task Reading_An_Artefact_The_Job_Never_Produced_Is_Null()
    {
        var jobId = await SeedJob();
        Assert.Null(await ArtefactService().Read(jobId, _organizationId, "fragment-missing.tfstate"));
    }

    /// <summary>Nothing a transfer carries outlives the job that carried it.</summary>
    [Fact]
    public async Task Finalizing_Deletes_Every_Artefact()
    {
        var jobId = await SeedJob();
        var service = ArtefactService();

        await service.Store(jobId, _organizationId, "fragment-app.tfstate", "a");
        await service.Store(jobId, _organizationId, "outputs-app.yaml", "b");

        Assert.Equal(2, await service.DeleteForJob(jobId, _organizationId));
        Assert.Null(await service.Read(jobId, _organizationId, "fragment-app.tfstate"));
    }

    [Fact]
    public async Task A_Retry_Writes_A_New_Attempt_Rather_Than_Overwriting()
    {
        var jobId = await SeedJob();
        var steps = StepService();

        await steps.Dispatched(jobId, _organizationId, _moduleId, "TransferMigrateProve");
        await steps.Completed(jobId, _organizationId, _moduleId, "TransferMigrateProve", ManualJobStepStatus.Refused, exitCode: 2);

        var attempt = await steps.Dispatched(jobId, _organizationId, _moduleId, "TransferMigrateProve");
        await steps.Completed(jobId, _organizationId, _moduleId, "TransferMigrateProve", ManualJobStepStatus.Succeeded, exitCode: 0);

        Assert.Equal(2, attempt);

        await using var db = _fixture.CreateDbContext();
        var all = await db.ManualModuleJobSteps.AsNoTracking()
            .Where(s => s.JobId == jobId).OrderBy(s => s.Attempt).ToListAsync();

        Assert.Equal(2, all.Count);
        Assert.Equal(ManualJobStepStatus.Refused, all[0].Status);
        Assert.Equal(ManualJobStepStatus.Succeeded, all[1].Status);
    }

    /// <summary>The verdict reads the latest attempt, so an earlier refusal does not outvote a retry.</summary>
    [Fact]
    public async Task Latest_Returns_One_Row_Per_Module_And_Task()
    {
        var jobId = await SeedJob();
        var steps = StepService();

        await steps.Dispatched(jobId, _organizationId, _moduleId, "TransferMigrateProve");
        await steps.Completed(jobId, _organizationId, _moduleId, "TransferMigrateProve", ManualJobStepStatus.Refused, exitCode: 2);
        await steps.Dispatched(jobId, _organizationId, _moduleId, "TransferMigrateProve");
        await steps.Completed(jobId, _organizationId, _moduleId, "TransferMigrateProve", ManualJobStepStatus.Succeeded, exitCode: 0);

        var latest = await steps.Latest(jobId, _organizationId);

        var step = Assert.Single(latest);
        Assert.Equal(ManualJobStepStatus.Succeeded, step.Status);
        Assert.Equal(2, step.Attempt);
    }

    [Fact]
    public async Task A_Reused_Step_Cites_The_Key_That_Made_It_Reusable()
    {
        var jobId = await SeedJob();
        var steps = StepService();

        await steps.Reused(jobId, _organizationId, _moduleId, "TransferMigrateProve", "key-abc");

        var step = Assert.Single(await steps.Latest(jobId, _organizationId));
        Assert.Equal(ManualJobStepStatus.Succeeded, step.Status);
        Assert.Equal("key-abc", step.InputKey);
    }

    [Fact]
    public async Task A_Skipped_Step_Is_Recorded_Rather_Than_Omitted()
    {
        var jobId = await SeedJob();
        var steps = StepService();

        await steps.Skipped(jobId, _organizationId, _moduleId, "TransferMigrateProve");

        var step = Assert.Single(await steps.Latest(jobId, _organizationId));
        Assert.Equal(ManualJobStepStatus.Skipped, step.Status);
    }

    /// <summary>A producer retried after this succeeded means the result no longer counts.</summary>
    [Fact]
    public async Task Marking_Stale_Demotes_An_Earlier_Green_Result()
    {
        var jobId = await SeedJob();
        var steps = StepService();

        await steps.Dispatched(jobId, _organizationId, _moduleId, "TransferMigrateProve");
        await steps.Completed(jobId, _organizationId, _moduleId, "TransferMigrateProve", ManualJobStepStatus.Succeeded, exitCode: 0);

        await steps.MarkStale(jobId, _organizationId, _moduleId);

        var step = Assert.Single(await steps.Latest(jobId, _organizationId));
        Assert.Equal(ManualJobStepStatus.Stale, step.Status);
    }

    private async Task<Guid> SeedJob()
    {
        var jobId = Guid.NewGuid();
        _seededJobs.Add(jobId);

        await using var db = _fixture.CreateDbContext();
        db.ManualModuleJobs.Add(new ManualModuleJob
        {
            Id = jobId,
            ModuleId = _moduleId,
            OrganizationId = _organizationId,
            TimestampStart = DateTimeOffset.UtcNow,
            JobType = ManualJobTypes.TransferProve,
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

    private ManualJobStepService StepService() => new(DbContextFactory());

    private IDbContextFactory<SnapCdDbContext> DbContextFactory()
    {
        var services = new ServiceCollection();
        services.AddDbContextFactory<SnapCdDbContext>(o => o.UseSqlServer(_fixture.ConnectionString));
        return services.BuildServiceProvider().GetRequiredService<IDbContextFactory<SnapCdDbContext>>();
    }
}
