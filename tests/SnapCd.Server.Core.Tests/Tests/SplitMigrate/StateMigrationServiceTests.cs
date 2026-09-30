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

namespace SnapCd.Server.Core.Tests.Tests.SplitMigrate;

/// <summary>
/// A manual job is refused rather than queued, so the launch path's checks are the whole of its
/// admission control. Each refusal must name the condition that blocked it.
/// </summary>
[Collection("NewRoleBasedSharedFixture")]
public class StateMigrationServiceTests : IAsyncLifetime
{
    private readonly Fixture _fixture;
    private SnapCdDbContext _dbContext = null!;
    private Guid _moduleId;
    private Guid _organizationId;

    public StateMigrationServiceTests(Fixture fixture) => _fixture = fixture;

    private const string ConnectedInstance = "state-migration-service-tests";

    private StateManagementEngine? _originalModuleEngine;
    private StateManagementEngine? _originalNamespaceEngine;

    public async Task InitializeAsync()
    {
        _dbContext = _fixture.CreateDbContext();
        _moduleId = _fixture.Modules["0000"].Id;
        _organizationId = _fixture.Organizations["0"].Id;

        var module = _dbContext.Modules.AsNoTracking().First(m => m.Id == _moduleId);
        _originalModuleEngine = module.Engine;
        _originalNamespaceEngine = _dbContext.Namespaces.AsNoTracking()
            .First(n => n.Id == module.NamespaceId).DefaultEngine;

        // A state migration is refused unless the Module's runner is connected.
        var runnerId = _dbContext.Modules.AsNoTracking().First(m => m.Id == _moduleId).RunnerId;
        if (!_dbContext.RunnerConnections.Any(rc => rc.RunnerId == runnerId && rc.InstanceName == ConnectedInstance))
        {
            _dbContext.RunnerConnections.Add(new RunnerConnection
            {
                Id = Guid.NewGuid(),
                OrganizationId = _organizationId,
                RunnerId = runnerId,
                InstanceName = ConnectedInstance,
                SignalRConnectionId = $"{ConnectedInstance}-connection",
                ServerInstanceId = Guid.NewGuid()
            });
            await _dbContext.SaveChangesAsync();
        }
    }

    public async Task DisposeAsync()
    {
        await ResetPause();
        await RemoveStateMigrations();
        await SetModuleEngine(_originalModuleEngine);
        await SetNamespaceDefaultEngine(_originalNamespaceEngine);
        _dbContext.RunnerConnections.RemoveRange(
            _dbContext.RunnerConnections.Where(rc => rc.InstanceName == ConnectedInstance));
        await _dbContext.SaveChangesAsync();
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
        await AddRunningStateMigration();

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
    public async Task Refuses_A_Pulumi_Module()
    {
        await SetPaused(true);
        await SetModuleEngine(StateManagementEngine.Pulumi);

        using var service = CreateService();
        var reason = await service.GetBlockedReason(_moduleId, _organizationId);

        Assert.Contains("Pulumi", reason);
    }

    [Fact]
    public async Task Refuses_A_Module_Inheriting_Pulumi_From_Its_Namespace()
    {
        await SetPaused(true);
        await SetModuleEngine(null);
        await SetNamespaceDefaultEngine(StateManagementEngine.Pulumi);

        using var service = CreateService();
        var reason = await service.GetBlockedReason(_moduleId, _organizationId);

        Assert.Contains("Pulumi", reason);
    }

    [Fact]
    public async Task Allows_A_Module_With_No_Engine_Anywhere()
    {
        // Configuration resolves an unset engine to OpenTofu, which has the state commands.
        await SetPaused(true);
        await SetModuleEngine(null);
        await SetNamespaceDefaultEngine(null);

        using var service = CreateService();

        Assert.Null(await service.GetBlockedReason(_moduleId, _organizationId));
    }

    [Theory]
    [InlineData(StateManagementEngine.Terraform)]
    [InlineData(StateManagementEngine.OpenTofu)]
    public async Task Allows_The_Terraform_Lineage(StateManagementEngine engine)
    {
        await SetPaused(true);
        await SetModuleEngine(engine);

        using var service = CreateService();

        Assert.Null(await service.GetBlockedReason(_moduleId, _organizationId));
    }

    [Fact]
    public async Task Start_Refuses_A_Pulumi_Module()
    {
        await SetPaused(true);
        await SetModuleEngine(StateManagementEngine.Pulumi);

        using var service = CreateService();

        await Assert.ThrowsAsync<StateMigrationNotAllowedException>(
            () => service.Start(_moduleId, _organizationId, StateMigrationTypes.SplitMigrate));
    }

    [Fact]
    public async Task Start_Refuses_When_Blocked()
    {
        await SetPaused(false);

        using var service = CreateService();

        await Assert.ThrowsAsync<StateMigrationNotAllowedException>(
            () => service.Start(_moduleId, _organizationId, StateMigrationTypes.SplitMigrate));
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
        var job = await service.Start(_moduleId, _organizationId, StateMigrationTypes.SplitMigrate, correlationId);

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
    public async Task A_Prove_Refuses_On_An_Unpaused_Module()
    {
        await SetPaused(false);

        using var service = CreateService();
        Assert.Equal("The module must be paused before a manual job can run.", await service.GetBlockedReason(_moduleId, _organizationId));
    }

    [Fact]
    public async Task A_Prove_Refuses_While_A_Deployment_Is_Draining()
    {
        await SetPaused(true);
        await AddCurrentModuleJob();

        using var service = CreateService();
        Assert.Contains("still finishing", await service.GetBlockedReason(_moduleId, _organizationId));
    }

    [Fact]
    public async Task A_Prove_Refuses_A_Bad_Ref_Before_Anything_Is_Written()
    {
        await SetPaused(true);

        using var service = CreateService();
        await Assert.ThrowsAsync<StateMigrationNotAllowedException>(
            () => service.StartSplitProve(_moduleId, _organizationId, null, "-b"));

        await using var db = _fixture.CreateDbContext();
        Assert.Equal(0, await db.StateMigrationJobs.CountAsync(j => j.ModuleId == _moduleId));
    }

    [Fact]
    public async Task A_State_Migration_Refuses_A_Ref()
    {
        await SetPaused(true);

        using var service = CreateService();
        var ex = await Assert.ThrowsAsync<StateMigrationNotAllowedException>(
            () => service.StartSplitMigrate(_moduleId, _organizationId, null, false, "feature/split"));

        Assert.Contains("configured branch", ex.Message);
        await using var db = _fixture.CreateDbContext();
        Assert.Equal(0, await db.StateMigrationJobs.CountAsync(j => j.ModuleId == _moduleId && j.Status == ExecutionStatus.Running));
    }

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
        var running = await db.StateMigrationJobs
            .CountAsync(j => j.ModuleId == _moduleId && j.Status == ExecutionStatus.Running);

        Assert.Equal(1, running);
        return;

        async Task<bool> Attempt(StateMigrationService service)
        {
            try
            {
                await service.Start(_moduleId, _organizationId, StateMigrationTypes.SplitMigrate);
                return true;
            }
            catch (StateMigrationNotAllowedException)
            {
                return false;
            }
        }
    }

    private StateMigrationService CreateService()
    {
        var principalProvider = _fixture.CreatePrincipalProvider(
            _fixture.OrganizationPrincipals["0"][OrganizationRole.Owner].DirectUser.Id,
            PrincipalDiscriminator.User,
            _organizationId);

        var securedRepository = new ModuleSecuredRepository(
            new ModuleRepository(_fixture.CreateDbContext(), principalProvider, _fixture.CreateMockBus(), _fixture.CreateModuleSettings()),
            principalProvider);

        return new StateMigrationService(DbContextFactory(), securedRepository);
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

    private async Task SetModuleEngine(StateManagementEngine? engine)
    {
        await using var db = _fixture.CreateDbContext();
        var module = await db.Modules.FirstAsync(m => m.Id == _moduleId);
        module.Engine = engine;
        await db.SaveChangesAsync();
    }

    private async Task SetNamespaceDefaultEngine(StateManagementEngine? engine)
    {
        await using var db = _fixture.CreateDbContext();
        var module = await db.Modules.AsNoTracking().FirstAsync(m => m.Id == _moduleId);
        var ns = await db.Namespaces.FirstAsync(n => n.Id == module.NamespaceId);
        ns.DefaultEngine = engine;
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

    private async Task AddRunningStateMigration()
    {
        await using var db = _fixture.CreateDbContext();
        db.StateMigrationJobs.Add(new StateMigrationJob
        {
            Id = Guid.NewGuid(),
            ModuleId = _moduleId,
            OrganizationId = _organizationId,
            TimestampStart = DateTimeOffset.UtcNow,
            JobType = StateMigrationTypes.SplitMigrate,
            Status = ExecutionStatus.Running
        });
        await db.SaveChangesAsync();
    }

    private async Task RemoveStateMigrations()
    {
        await using var db = _fixture.CreateDbContext();
        var manual = await db.StateMigrationJobs.Where(j => j.ModuleId == _moduleId).ToListAsync();
        db.StateMigrationJobs.RemoveRange(manual);

        var jobs = await db.ModuleJobs.Where(j => j.ModuleId == _moduleId && j.IsCurrent == true).ToListAsync();
        db.ModuleJobs.RemoveRange(jobs);

        await db.SaveChangesAsync();
    }
}
