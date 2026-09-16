// SPDX-License-Identifier: LicenseRef-Snap-CD-Source-Available-1.1
// Copyright (c) 2026 Karl Schriek / Schrieksoft.
// No license is granted to use this file, in whole or in part, (a) as training, fine-tuning, retrieval, or
// embedding data for any machine-learning model, or (b) as input to any machine-learning model, agent, or automated
// system for the purpose of producing a derivative work or reimplementation that is not otherwise permitted by the
// Snap CD Source-Available License (including any Competing Product as defined therein). Contact info@snapcd.io
// for terms covering either use.
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using SnapCd.Contracts;
using SnapCd.Server.Core.Database;
using SnapCd.Server.Core.Entities.Definition;
using SnapCd.Server.Core.Entities.Sagas;
using SnapCd.Server.Core.Enums;
using SnapCd.Server.Core.Repositories.Organizations.Nonsecured;
using SnapCd.Server.Core.Repositories.Organizations.Secured;
using SnapCd.Server.Core.Services;
using SnapCd.Server.Core.Services.Crud.Jobs;
using SnapCd.Server.Core.Tests.Infrastructure;
using Xunit;

namespace SnapCd.Server.Core.Tests.Tests.ManualJobs;

/// <summary>
/// A manual job left Running with no saga blocks every later manual job on its Module through
/// the filtered unique index, so the cleanup sweep closes it directly and the launch path opens
/// up again.
/// </summary>
[Collection("NewRoleBasedSharedFixture")]
public class OrphanedManualJobCleanupTests : IAsyncLifetime
{
    private readonly Fixture _fixture;
    private IDbContextFactory<SnapCdDbContext> _dbContextFactory = null!;
    private Guid _moduleId;
    private Guid _organizationId;
    private readonly List<Guid> _seeded = [];

    public OrphanedManualJobCleanupTests(Fixture fixture) => _fixture = fixture;

    public Task InitializeAsync()
    {
        _moduleId = _fixture.Modules["0000"].Id;
        _organizationId = _fixture.Organizations["0"].Id;
        var services = new ServiceCollection();
        services.AddDbContextFactory<SnapCdDbContext>(o => o.UseSqlServer(_fixture.ConnectionString));
        _dbContextFactory = services.BuildServiceProvider().GetRequiredService<IDbContextFactory<SnapCdDbContext>>();
        return Task.CompletedTask;
    }

    public async Task DisposeAsync()
    {
        await using var db = _fixture.CreateDbContext();
        db.Set<SplitMonolithSaga>().RemoveRange(db.Set<SplitMonolithSaga>().Where(s => _seeded.Contains(s.CorrelationId)));
        db.ManualModuleJobs.RemoveRange(db.ManualModuleJobs.Where(j => _seeded.Contains(j.Id)));
        var saga = await db.Set<ModuleSaga>().FirstAsync(s => s.CorrelationId == _moduleId);
        saga.Paused = false;
        await db.SaveChangesAsync();
    }

    [Fact]
    public async Task A_Running_Job_Without_A_Saga_Is_Orphaned()
    {
        var jobId = await SeedJob(withSaga: false, ended: false);

        var orphaned = await new OrphanedJobCleanupService(_dbContextFactory).ListOrphanedManualJobs();

        Assert.Contains(orphaned, j => j.Id == jobId && j.JobType == ManualJobTypes.SplitMonolith && j.OrganizationId == _organizationId);
    }

    [Fact]
    public async Task A_Running_Job_With_A_Saga_Is_Not_Orphaned()
    {
        var jobId = await SeedJob(withSaga: true, ended: false);

        var orphaned = await new OrphanedJobCleanupService(_dbContextFactory).ListOrphanedManualJobs();

        Assert.DoesNotContain(orphaned, j => j.Id == jobId);
    }

    [Fact]
    public async Task A_Finished_Job_Without_A_Saga_Is_Not_Orphaned()
    {
        var jobId = await SeedJob(withSaga: false, ended: true);

        var orphaned = await new OrphanedJobCleanupService(_dbContextFactory).ListOrphanedManualJobs();

        Assert.DoesNotContain(orphaned, j => j.Id == jobId);
    }

    [Fact]
    public async Task The_Sweep_Closes_The_Orphan_And_The_Launch_Path_Reopens()
    {
        await SetPaused(true);
        var jobId = await SeedJob(withSaga: false, ended: false);
        Assert.Equal("A manual job is already running on this module.", await BlockedReason());

        var job = new OrphanedJobCleanupJob(
            new OrphanedJobCleanupService(_dbContextFactory),
            new ManualModuleJobRepositoryFactory(_dbContextFactory),
            _fixture.CreateMockBus(),
            NullLogger<OrphanedJobCleanupJob>.Instance);
        await job.ExecuteJob();

        await using (var db = _fixture.CreateDbContext())
        {
            var closed = await db.ManualModuleJobs.AsNoTracking().SingleAsync(j => j.Id == jobId);
            Assert.Equal(ExecutionStatus.Failed, closed.Status);
            Assert.NotNull(closed.TimestampEnd);
            Assert.Equal("This job was abandoned.", closed.ServerSideErrorHeader);
            Assert.False(closed.WaitingForApproval);
        }

        Assert.Null(await BlockedReason());
    }

    private async Task<string?> BlockedReason()
    {
        var principalProvider = _fixture.CreatePrincipalProvider(
            _fixture.OrganizationPrincipals["0"][OrganizationRole.Owner].DirectUser.Id, PrincipalDiscriminator.User, _organizationId);
        var secured = new ModuleSecuredRepository(
            new ModuleRepository(_fixture.CreateDbContext(), principalProvider, _fixture.CreateMockBus(), _fixture.CreateModuleSettings()),
            principalProvider);
        using var service = new ManualJobService(_dbContextFactory, secured);
        return await service.GetBlockedReason(_moduleId, _organizationId);
    }

    private async Task<Guid> SeedJob(bool withSaga, bool ended)
    {
        var jobId = Guid.NewGuid();
        _seeded.Add(jobId);
        await using var db = _fixture.CreateDbContext();
        db.ManualModuleJobs.Add(new ManualModuleJob
        {
            Id = jobId,
            ModuleId = _moduleId,
            OrganizationId = _organizationId,
            TimestampStart = DateTimeOffset.UtcNow.AddMinutes(-5),
            TimestampEnd = ended ? DateTimeOffset.UtcNow : null,
            JobType = ManualJobTypes.SplitMonolith,
            Status = ended ? ExecutionStatus.Completed : ExecutionStatus.Running
        });
        if (withSaga)
            db.Set<SplitMonolithSaga>().Add(new SplitMonolithSaga
            {
                CorrelationId = jobId,
                ModuleId = _moduleId,
                OrganizationId = _organizationId,
                CurrentState = "PlanPending",
                RunnerId = _fixture.Runners["0"].Id,
                RunnerName = "runner",
                RunnerInstanceName = "orphan-test",
                DeclaredJson = "{}",
                RowVersion = []
            });
        await db.SaveChangesAsync();
        return jobId;
    }

    private async Task SetPaused(bool paused)
    {
        await using var db = _fixture.CreateDbContext();
        var saga = await db.Set<ModuleSaga>().FirstAsync(s => s.CorrelationId == _moduleId);
        saga.Paused = paused;
        await db.SaveChangesAsync();
    }
}
