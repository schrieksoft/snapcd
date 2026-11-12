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
using SnapCd.Server.Core.Tests.Infrastructure;

namespace SnapCd.Server.Core.Tests.Tests.StateMachine;

/// <summary>
/// A RunnerConnectionJob row is the server's only record that a job is being worked on, and it
/// hangs off the connection it was learned on. A disconnect takes the row with the connection, and
/// the reconnect has to put it back: what the server dispatched to an instance is not what is still
/// running there, because a restarted runner reconnects under the same name.
/// </summary>
[Collection("NewRoleBasedSharedFixture")]
public class RunnerConnectionJobLifetimeTests : IAsyncLifetime
{
    private readonly Fixture _fixture;

    private Module _module = null!;
    private Runner _runner = null!;

    public RunnerConnectionJobLifetimeTests(Fixture fixture)
    {
        _fixture = fixture;
    }

    public Task InitializeAsync()
    {
        using var db = _fixture.CreateDbContext();
        _module = db.Modules.Include(m => m.Namespace).First(m => m.Id == _fixture.Modules["0000"].Id);
        _runner = db.Runners.First(r => r.Id == _module.RunnerId);

        return Task.CompletedTask;
    }

    public Task DisposeAsync() => Task.CompletedTask;

    private async Task<(Guid ConnectionId, string InstanceName)> SeedConnection()
    {
        var connectionId = Guid.NewGuid();
        var instanceName = $"lifetime-{Guid.NewGuid():N}"[..30];

        await using var db = _fixture.CreateDbContext();
        db.RunnerConnections.Add(new RunnerConnection
        {
            Id = connectionId,
            OrganizationId = _module.OrganizationId,
            RunnerId = _runner.Id,
            InstanceName = instanceName,
            SignalRConnectionId = $"conn-{connectionId:N}",
            ServerInstanceId = Guid.NewGuid()
        });
        await db.SaveChangesAsync();

        return (connectionId, instanceName);
    }

    private async Task<Guid> SeedRunningJob()
    {
        var jobId = Guid.NewGuid();

        await using var db = _fixture.CreateDbContext();
        db.ModuleJobs.Add(new ModuleJob
        {
            Id = jobId,
            OrganizationId = _module.OrganizationId,
            ModuleId = _module.Id,
            JobType = nameof(ModuleJob),
            Status = ExecutionStatus.Running,
            TimestampStart = DateTimeOffset.UtcNow
        });
        await db.SaveChangesAsync();

        return jobId;
    }

    private async Task SeedConnectionJob(Guid connectionId, Guid jobId)
    {
        await using var db = _fixture.CreateDbContext();
        db.RunnerConnectionJobs.Add(new RunnerConnectionJob
        {
            Id = Guid.NewGuid(),
            OrganizationId = _module.OrganizationId,
            RunnerConnectionId = connectionId,
            ModuleJobId = jobId
        });
        await db.SaveChangesAsync();
    }

    private async Task<int> CountConnectionJobs(Guid jobId)
    {
        await using var db = _fixture.CreateDbContext();
        return await db.RunnerConnectionJobs
            .CountAsync(r => r.ModuleJobId == jobId && r.OrganizationId == _module.OrganizationId);
    }

    [Fact]
    public async Task Deleting_The_Connection_Takes_The_Job_Row_With_It()
    {
        var (connectionId, _) = await SeedConnection();
        var jobId = await SeedRunningJob();
        await SeedConnectionJob(connectionId, jobId);

        Assert.Equal(1, await CountConnectionJobs(jobId));

        // What OnDisconnectedAsync does.
        await using (var db = _fixture.CreateDbContext())
        {
            await db.RunnerConnections
                .Where(rc => rc.Id == connectionId)
                .ExecuteDeleteAsync();
        }

        Assert.Equal(0, await CountConnectionJobs(jobId));
    }

    [Fact]
    public async Task The_Job_Itself_Survives_Its_Connection()
    {
        var (connectionId, _) = await SeedConnection();
        var jobId = await SeedRunningJob();
        await SeedConnectionJob(connectionId, jobId);

        await using (var db = _fixture.CreateDbContext())
        {
            await db.RunnerConnections
                .Where(rc => rc.Id == connectionId)
                .ExecuteDeleteAsync();
        }

        // The cascade runs one way only. A job whose runner dropped is still Running, which is why
        // the server has to decide whether to fail it rather than finding it already gone.
        await using var check = _fixture.CreateDbContext();
        var job = await check.ModuleJobs.SingleAsync(j => j.Id == jobId);

        Assert.Equal(ExecutionStatus.Running, job.Status);
    }

    [Fact]
    public async Task A_Report_On_The_New_Connection_Restores_The_Row()
    {
        var (firstConnectionId, _) = await SeedConnection();
        var jobId = await SeedRunningJob();
        await SeedConnectionJob(firstConnectionId, jobId);

        await using (var db = _fixture.CreateDbContext())
        {
            await db.RunnerConnections
                .Where(rc => rc.Id == firstConnectionId)
                .ExecuteDeleteAsync();
        }

        Assert.Equal(0, await CountConnectionJobs(jobId));

        // The runner reconnects and reports the job it is still working on.
        var (secondConnectionId, _) = await SeedConnection();
        await SeedConnectionJob(secondConnectionId, jobId);

        Assert.Equal(1, await CountConnectionJobs(jobId));

        await using var check = _fixture.CreateDbContext();
        var restored = await check.RunnerConnectionJobs
            .SingleAsync(r => r.ModuleJobId == jobId && r.OrganizationId == _module.OrganizationId);

        Assert.Equal(secondConnectionId, restored.RunnerConnectionId);
    }

    [Fact]
    public async Task A_Runner_That_Reports_Nothing_Leaves_The_Job_Unrecorded()
    {
        var (firstConnectionId, _) = await SeedConnection();
        var jobId = await SeedRunningJob();
        await SeedConnectionJob(firstConnectionId, jobId);

        await using (var db = _fixture.CreateDbContext())
        {
            await db.RunnerConnections
                .Where(rc => rc.Id == firstConnectionId)
                .ExecuteDeleteAsync();
        }

        // A restarted runner reconnects under the same instance name with nothing in flight, so it
        // reports no jobs. The absent row is the answer, and the heartbeat fails the job on it.
        await SeedConnection();

        Assert.Equal(0, await CountConnectionJobs(jobId));
    }
}
