// SPDX-License-Identifier: LicenseRef-Snap-CD-Source-Available-1.1
// Copyright (c) 2026 Karl Schriek / Schrieksoft.
// No license is granted to use this file, in whole or in part, (a) as training, fine-tuning, retrieval, or
// embedding data for any machine-learning model, or (b) as input to any machine-learning model, agent, or automated
// system for the purpose of producing a derivative work or reimplementation that is not otherwise permitted by the
// Snap CD Source-Available License (including any Competing Product as defined therein). Contact info@snapcd.io
// for terms covering either use.

using MassTransit;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.EntityFrameworkCore;
using Moq;
using SnapCd.Contracts;
using SnapCd.Server.Core.Database;
using SnapCd.Server.Core.Entities.Definition;
using SnapCd.Server.Core.Entities.Sagas;
using SnapCd.Server.Core.Enums;
using SnapCd.Server.Core.Misc.Exceptions;
using SnapCd.Server.Core.Repositories.Organizations.Nonsecured;
using SnapCd.Server.Core.Repositories.Organizations.Secured;
using SnapCd.Server.Core.Services.Crud.Jobs;
using SnapCd.Server.Core.Tests.Infrastructure;
using Xunit;

namespace SnapCd.Server.Core.Tests.Tests.SplitMonolith;

/// <summary>
/// A manual job is refused rather than queued, so the launch path's checks are the whole of its
/// admission control. Each refusal must name the condition that blocked it.
/// </summary>
[Collection("NewRoleBasedSharedFixture")]
public class ManualJobServiceTests : IAsyncLifetime
{
    private readonly Fixture _fixture;
    private SnapCdDbContext _dbContext = null!;
    private Guid _moduleId;
    private Guid _organizationId;

    public ManualJobServiceTests(Fixture fixture) => _fixture = fixture;

    public Task InitializeAsync()
    {
        _dbContext = _fixture.CreateDbContext();
        _moduleId = _fixture.Modules["0000"].Id;
        _organizationId = _fixture.Organizations["0"].Id;
        return Task.CompletedTask;
    }

    public async Task DisposeAsync()
    {
        await ResetPause();
        await RemoveManualJobs();
        await _dbContext.DisposeAsync();
    }

    [Fact]
    public async Task Refuses_When_The_Module_Is_Not_Paused()
    {
        await SetPaused(false);

        using var service = CreateService();
        var reason = await service.GetBlockedReason(_moduleId, _organizationId);

        Assert.Equal("The module must be paused before a manual job can run.", reason);
    }

    [Fact]
    public async Task Refuses_While_A_Deployment_Is_Still_Draining()
    {
        await SetPaused(true);
        await AddCurrentModuleJob();

        using var service = CreateService();
        var reason = await service.GetBlockedReason(_moduleId, _organizationId);

        Assert.Contains("still finishing", reason);
    }

    [Fact]
    public async Task Refuses_When_A_Manual_Job_Is_Already_Running()
    {
        await SetPaused(true);
        await AddRunningManualJob();

        using var service = CreateService();
        var reason = await service.GetBlockedReason(_moduleId, _organizationId);

        Assert.Equal("A manual job is already running on this module.", reason);
    }

    [Fact]
    public async Task Allows_A_Paused_And_Quiet_Module()
    {
        await SetPaused(true);

        using var service = CreateService();
        var reason = await service.GetBlockedReason(_moduleId, _organizationId);

        Assert.Null(reason);
    }

    [Fact]
    public async Task Start_Refuses_When_Blocked()
    {
        await SetPaused(false);

        using var service = CreateService();

        await Assert.ThrowsAsync<ManualJobNotAllowedException>(
            () => service.Start(_moduleId, _organizationId, "SplitMonolith"));
    }

    /// <summary>
    /// The returned Id must be the correlation id the caller publishes the saga request with:
    /// the job row and its saga share one id.
    /// </summary>
    [Fact]
    public async Task Start_Uses_The_Supplied_Correlation_Id()
    {
        await SetPaused(true);
        var correlationId = Guid.NewGuid();

        using var service = CreateService();
        var job = await service.Start(_moduleId, _organizationId, "SplitMonolith", correlationId);

        Assert.Equal(correlationId, job.Id);
        Assert.Equal(ExecutionStatus.Running, job.Status);
        Assert.Null(job.TimestampEnd);
    }

    /// <summary>
    /// There is no gatekeeping saga serialising these requests, so the pre-check can be raced and
    /// the filtered unique index is the real guarantee. The second insert must surface as a
    /// refusal rather than an unhandled DbUpdateException.
    /// </summary>
    [Fact]
    public async Task Two_Concurrent_Starts_Leave_Only_One_Running()
    {
        await SetPaused(true);

        using var first = CreateService();
        using var second = CreateService();

        var results = await Task.WhenAll(
            Attempt(first),
            Attempt(second));

        Assert.Equal(1, results.Count(r => r));

        await using var db = _fixture.CreateDbContext();
        var running = await db.ManualModuleJobs
            .CountAsync(j => j.ModuleId == _moduleId && j.Status == ExecutionStatus.Running);

        Assert.Equal(1, running);
        return;

        async Task<bool> Attempt(ManualJobService service)
        {
            try
            {
                await service.Start(_moduleId, _organizationId, "SplitMonolith");
                return true;
            }
            catch (ManualJobNotAllowedException)
            {
                return false;
            }
        }
    }

    private ManualJobService CreateService()
    {
        var principalProvider = _fixture.CreatePrincipalProvider(
            _fixture.OrganizationPrincipals["0"][OrganizationRole.Owner].DirectUser.Id,
            PrincipalDiscriminator.User,
            _organizationId);

        var securedRepository = new ModuleSecuredRepository(
            new ModuleRepository(_fixture.CreateDbContext(), principalProvider, _fixture.CreateMockBus(), _fixture.CreateModuleSettings()),
            principalProvider);

        return new ManualJobService(DbContextFactory(), securedRepository);
    }

    /// The fixture hands out contexts, not a factory; the service needs one of its own.
    private IDbContextFactory<SnapCdDbContext> DbContextFactory()
    {
        var services = new ServiceCollection();
        services.AddDbContextFactory<SnapCdDbContext>(o => o.UseSqlServer(_fixture.ConnectionString));
        return services.BuildServiceProvider().GetRequiredService<IDbContextFactory<SnapCdDbContext>>();
    }

    private async Task SetPaused(bool paused)
    {
        await using var db = _fixture.CreateDbContext();
        var saga = await db.Set<ModuleSaga>().FirstAsync(s => s.CorrelationId == _moduleId);
        saga.Paused = paused;
        await db.SaveChangesAsync();
    }

    private Task ResetPause() => SetPaused(false);

    private async Task AddCurrentModuleJob()
    {
        await using var db = _fixture.CreateDbContext();
        db.ModuleJobs.Add(new ModuleJob
        {
            Id = Guid.NewGuid(),
            ModuleId = _moduleId,
            OrganizationId = _organizationId,
            TimestampStart = DateTimeOffset.UtcNow,
            JobType = "ApplyJobSaga",
            Status = ExecutionStatus.Running,
            IsCurrent = true
        });
        await db.SaveChangesAsync();
    }

    private async Task AddRunningManualJob()
    {
        await using var db = _fixture.CreateDbContext();
        db.ManualModuleJobs.Add(new ManualModuleJob
        {
            Id = Guid.NewGuid(),
            ModuleId = _moduleId,
            OrganizationId = _organizationId,
            TimestampStart = DateTimeOffset.UtcNow,
            JobType = "SplitMonolith",
            Status = ExecutionStatus.Running
        });
        await db.SaveChangesAsync();
    }

    private async Task RemoveManualJobs()
    {
        await using var db = _fixture.CreateDbContext();
        var manual = await db.ManualModuleJobs.Where(j => j.ModuleId == _moduleId).ToListAsync();
        db.ManualModuleJobs.RemoveRange(manual);

        var jobs = await db.ModuleJobs.Where(j => j.ModuleId == _moduleId && j.IsCurrent == true).ToListAsync();
        db.ModuleJobs.RemoveRange(jobs);

        await db.SaveChangesAsync();
    }
}
