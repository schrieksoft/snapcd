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

namespace SnapCd.Server.Core.Tests.Tests.RippleProbes;

/// <summary>
/// A probe left Running with no saga blocks every later probe on its Module through the filtered
/// unique index, so the cleanup sweep closes it directly and the launch path opens up again.
/// </summary>
[Collection("NewRoleBasedSharedFixture")]
public class OrphanedRippleProbeCleanupTests : IAsyncLifetime
{
    private readonly Fixture _fixture;
    private IDbContextFactory<SnapCdDbContext> _dbContextFactory = null!;
    private Guid _moduleId;
    private Guid _upstreamModuleId;
    private Guid _organizationId;
    private readonly List<Guid> _seeded = [];

    public OrphanedRippleProbeCleanupTests(Fixture fixture) => _fixture = fixture;

    public Task InitializeAsync()
    {
        _moduleId = _fixture.Modules["0010"].Id;
        _upstreamModuleId = _fixture.Modules["0011"].Id;
        _organizationId = _fixture.Organizations["0"].Id;
        var services = new ServiceCollection();
        services.AddDbContextFactory<SnapCdDbContext>(o => o.UseSqlServer(_fixture.ConnectionString));
        _dbContextFactory = services.BuildServiceProvider().GetRequiredService<IDbContextFactory<SnapCdDbContext>>();
        return Task.CompletedTask;
    }

    public async Task DisposeAsync()
    {
        await using var db = _fixture.CreateDbContext();
        db.Set<RippleProbeSaga>().RemoveRange(db.Set<RippleProbeSaga>().Where(s => _seeded.Contains(s.CorrelationId)));
        db.RippleProbeJobs.RemoveRange(db.RippleProbeJobs.Where(j => _seeded.Contains(j.Id)));
        await db.SaveChangesAsync();
    }

    [Fact]
    public async Task A_Running_Probe_Without_A_Saga_Is_Orphaned()
    {
        var jobId = await SeedProbe(withSaga: false, ended: false);

        var orphaned = await new OrphanedJobCleanupService(_dbContextFactory).ListOrphanedRippleProbes();

        Assert.Contains(orphaned, j => j.Id == jobId && j.JobType == nameof(RippleProbeSaga) && j.OrganizationId == _organizationId);
    }

    [Fact]
    public async Task A_Running_Probe_With_A_Saga_Is_Not_Orphaned()
    {
        var jobId = await SeedProbe(withSaga: true, ended: false);

        var orphaned = await new OrphanedJobCleanupService(_dbContextFactory).ListOrphanedRippleProbes();

        Assert.DoesNotContain(orphaned, j => j.Id == jobId);
    }

    [Fact]
    public async Task A_Finished_Probe_Without_A_Saga_Is_Not_Orphaned()
    {
        var jobId = await SeedProbe(withSaga: false, ended: true);

        var orphaned = await new OrphanedJobCleanupService(_dbContextFactory).ListOrphanedRippleProbes();

        Assert.DoesNotContain(orphaned, j => j.Id == jobId);
    }

    [Fact]
    public async Task The_Sweep_Closes_The_Orphan_And_The_Launch_Path_Reopens()
    {
        var jobId = await SeedProbe(withSaga: false, ended: false);
        Assert.Equal("A ripple probe is already running on this module.", await BlockedReason());

        var job = new OrphanedJobCleanupJob(
            new OrphanedJobCleanupService(_dbContextFactory),
            new StateMigrationJobRepositoryFactory(_dbContextFactory),
            new RippleProbeJobRepositoryFactory(_dbContextFactory),
            _fixture.CreateMockBus(),
            NullLogger<OrphanedJobCleanupJob>.Instance);
        await job.ExecuteJob();

        await using (var db = _fixture.CreateDbContext())
        {
            var closed = await db.RippleProbeJobs.AsNoTracking().SingleAsync(j => j.Id == jobId);
            Assert.Equal(ExecutionStatus.Failed, closed.Status);
            Assert.NotNull(closed.TimestampEnd);
            Assert.Equal("This probe was abandoned.", closed.ServerSideErrorHeader);
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
        using var service = new RippleProbeService(_dbContextFactory, secured);
        return await service.GetBlockedReason(_moduleId, _organizationId);
    }

    private async Task<Guid> SeedProbe(bool withSaga, bool ended)
    {
        var jobId = Guid.NewGuid();
        _seeded.Add(jobId);
        await using var db = _fixture.CreateDbContext();
        db.RippleProbeJobs.Add(new RippleProbeJob
        {
            Id = jobId,
            ModuleId = _moduleId,
            UpstreamModuleId = _upstreamModuleId,
            OrganizationId = _organizationId,
            TimestampStart = DateTimeOffset.UtcNow.AddMinutes(-5),
            TimestampEnd = ended ? DateTimeOffset.UtcNow : null,
            Status = ended ? ExecutionStatus.Completed : ExecutionStatus.Running,
            InputsJson = "{}"
        });
        if (withSaga)
            db.Set<RippleProbeSaga>().Add(new RippleProbeSaga
            {
                CorrelationId = jobId,
                ModuleId = _moduleId,
                UpstreamModuleId = _upstreamModuleId,
                OrganizationId = _organizationId,
                CurrentState = "RippleProbeProbePending",
                RunnerId = _fixture.Runners["0"].Id,
                RunnerName = "runner",
                RunnerInstanceName = "orphan-probe-test",
                DeclaredJson = "{}",
                InputsJson = "{}",
                RowVersion = []
            });
        await db.SaveChangesAsync();
        return jobId;
    }
}
