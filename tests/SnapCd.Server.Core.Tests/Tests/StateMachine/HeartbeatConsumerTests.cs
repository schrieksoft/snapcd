// SPDX-License-Identifier: LicenseRef-Snap-CD-Source-Available-1.1
// Copyright (c) 2026 Karl Schriek / Schrieksoft.
// No license is granted to use this file, in whole or in part, (a) as training, fine-tuning, retrieval, or
// embedding data for any machine-learning model, or (b) as input to any machine-learning model, agent, or automated
// system for the purpose of producing a derivative work or reimplementation that is not otherwise permitted by the
// Snap CD Source-Available License (including any Competing Product as defined therein). Contact info@snapcd.io
// for terms covering either use.

using MassTransit;
using MassTransit.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using SnapCd.Contracts;
using SnapCd.Server.Core.Consumers.Tasks;
using SnapCd.Server.Core.Database;
using SnapCd.Server.Core.Entities.Definition;
using SnapCd.Server.Core.Enums;
using SnapCd.Server.Core.Events.Steps;
using SnapCd.Server.Core.Services;
using SnapCd.Server.Core.Tests.Infrastructure;
using Xunit;

namespace SnapCd.Server.Core.Tests.Tests.StateMachine;

/// <summary>
/// The heartbeat reads liveness from whichever of the three connection tables holds the job's
/// row. A job id is unique across them, so the id alone decides; a job family missing from the
/// read answers HeartbeatFailed on every tick and stops the job with nothing in the log.
/// </summary>
[Collection("NewRoleBasedSharedFixture")]
public class HeartbeatConsumerTests : IAsyncLifetime
{
    private readonly Fixture _fixture;
    private ServiceProvider _provider = null!;
    private ITestHarness _harness = null!;
    private Module _module = null!;
    private Guid _runnerConnectionId;
    private readonly List<Guid> _jobs = [];

    public HeartbeatConsumerTests(Fixture fixture) => _fixture = fixture;

    public async Task InitializeAsync()
    {
        await using (var db = _fixture.CreateDbContext())
        {
            _module = db.Modules.Include(m => m.Namespace).First(m => m.Id == _fixture.Modules["0000"].Id);

            var existing = db.RunnerConnections
                .FirstOrDefault(rc => rc.RunnerId == _module.RunnerId && rc.InstanceName == Instance);
            if (existing == null)
            {
                existing = new RunnerConnection
                {
                    Id = Guid.NewGuid(),
                    OrganizationId = _module.OrganizationId,
                    RunnerId = _module.RunnerId,
                    InstanceName = Instance,
                    SignalRConnectionId = $"{Instance}-connection",
                    ServerInstanceId = Guid.NewGuid()
                };
                db.RunnerConnections.Add(existing);
                await db.SaveChangesAsync();
            }

            _runnerConnectionId = existing.Id;
        }

        var services = new ServiceCollection();
        services.AddLogging();
        services.AddDbContextFactory<SnapCdDbContext>(o => o.UseSqlServer(_fixture.ConnectionString));
        services.AddMassTransitTestHarness(x =>
        {
            x.AddConsumer<ModuleJobHeartbeatConsumer>();
            x.AddConsumer<StateMigrationHeartbeatConsumer>();
        });

        _provider = services.BuildServiceProvider(true);
        _harness = _provider.GetRequiredService<ITestHarness>();
        _harness.TestTimeout = TimeSpan.FromSeconds(10);
        await _harness.Start();
    }

    private const string Instance = "heartbeat-consumer-tests";

    public async Task DisposeAsync()
    {
        await _provider.DisposeAsync();

        await using var db = _fixture.CreateDbContext();
        await db.RunnerConnectionJobs.Where(r => _jobs.Contains(r.ModuleJobId)).ExecuteDeleteAsync();
        await db.RunnerConnectionStateMigrations.Where(r => _jobs.Contains(r.StateMigrationJobId)).ExecuteDeleteAsync();
        await db.ModuleJobs.Where(j => _jobs.Contains(j.Id)).ExecuteDeleteAsync();
        await db.StateMigrationJobs.Where(j => _jobs.Contains(j.Id)).ExecuteDeleteAsync();
        await db.RunnerConnections.Where(rc => rc.InstanceName == Instance).ExecuteDeleteAsync();
    }

    [Fact]
    public async Task A_Fresh_Deployment_Job_Heartbeat_Succeeds() =>
        Assert.True(await HeartbeatSucceeds<ModuleJobHeartbeatRequested>(await SeedModuleJob(ageSeconds: 5)));

    [Fact]
    public async Task A_Fresh_State_Migration_Heartbeat_Succeeds() =>
        Assert.True(await HeartbeatSucceeds<StateMigrationHeartbeatRequested>(await SeedStateMigration(ageSeconds: 5)));

    [Fact]
    public async Task A_Job_With_No_Connection_Row_Fails() =>
        Assert.False(await HeartbeatSucceeds<ModuleJobHeartbeatRequested>(Guid.NewGuid()));

    [Fact]
    public async Task The_First_Report_On_A_State_Migration_Leaves_It_Live()
    {
        var jobId = await SeedStateMigrationJobOnly();

        await new RunnerConnectionStateMigrationService(
            _provider.GetRequiredService<IDbContextFactory<SnapCdDbContext>>().CreateDbContext())
            .CreateOrUpdate(_module.OrganizationId, jobId, "Plan", _module.RunnerId, Instance);

        Assert.True(await HeartbeatSucceeds<StateMigrationHeartbeatRequested>(jobId));
    }

    private async Task<Guid> SeedStateMigrationJobOnly()
    {
        var jobId = Guid.NewGuid();
        _jobs.Add(jobId);

        await using var db = _fixture.CreateDbContext();
        db.StateMigrationJobs.Add(new StateMigrationJob
        {
            Id = jobId,
            OrganizationId = _module.OrganizationId,
            ModuleId = _fixture.Modules["0100"].Id,
            TimestampStart = DateTimeOffset.UtcNow,
            JobType = StateMigrationTypes.SplitMigrate,
            Status = ExecutionStatus.Running
        });
        await db.SaveChangesAsync();
        return jobId;
    }

    /// <summary>
    /// TRequest is the family. A family whose consumer does not exist cannot be named here, which
    /// is the point of the split: the old shared message let a third family compile and fail at
    /// runtime.
    /// </summary>
    private async Task<bool> HeartbeatSucceeds<TRequest>(Guid jobId)
        where TRequest : HeartbeatRequestedBase, new()
    {
        var client = _harness.Bus.CreateRequestClient<TRequest>();
        var response = await client.GetResponse<HeartbeatCompleted, HeartbeatFailed>(new TRequest
        {
            CorrelationId = jobId,
            OrganizationId = _module.OrganizationId,
            RunnerId = _module.RunnerId,
            RunnerInstanceName = Instance
        });

        return response.Is<HeartbeatCompleted>(out _);
    }

    private async Task<Guid> SeedModuleJob(int ageSeconds)
    {
        var jobId = Guid.NewGuid();
        _jobs.Add(jobId);

        await using var db = _fixture.CreateDbContext();
        db.ModuleJobs.Add(new ModuleJob
        {
            Id = jobId,
            OrganizationId = _module.OrganizationId,
            ModuleId = _module.Id,
            TimestampStart = DateTimeOffset.UtcNow,
            Status = ExecutionStatus.Running,
            JobType = "Apply",
            IsCurrent = null
        });
        db.RunnerConnectionJobs.Add(new RunnerConnectionJob
        {
            Id = Guid.NewGuid(),
            OrganizationId = _module.OrganizationId,
            RunnerConnectionId = _runnerConnectionId,
            ModuleJobId = jobId,
            TaskName = "Plan",
            ModifiedDateTime = DateTime.UtcNow.AddSeconds(-ageSeconds)
        });
        await db.SaveChangesAsync();
        return jobId;
    }

    private async Task<Guid> SeedStateMigration(int ageSeconds)
    {
        var jobId = Guid.NewGuid();
        _jobs.Add(jobId);

        await using var db = _fixture.CreateDbContext();
        db.StateMigrationJobs.Add(new StateMigrationJob
        {
            Id = jobId,
            OrganizationId = _module.OrganizationId,
            ModuleId = _fixture.Modules["0001"].Id,
            TimestampStart = DateTimeOffset.UtcNow,
            JobType = StateMigrationTypes.SplitMigrate,
            Status = ExecutionStatus.Running
        });
        db.RunnerConnectionStateMigrations.Add(new RunnerConnectionStateMigration
        {
            Id = Guid.NewGuid(),
            OrganizationId = _module.OrganizationId,
            RunnerConnectionId = _runnerConnectionId,
            StateMigrationJobId = jobId,
            TaskName = "Plan",
            ModifiedDateTime = DateTime.UtcNow.AddSeconds(-ageSeconds)
        });
        await db.SaveChangesAsync();
        return jobId;
    }
}
