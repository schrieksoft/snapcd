// SPDX-License-Identifier: LicenseRef-Snap-CD-Source-Available-1.1
// Copyright (c) 2026 Karl Schriek / Schrieksoft.
// No license is granted to use this file, in whole or in part, (a) as training, fine-tuning, retrieval, or
// embedding data for any machine-learning model, or (b) as input to any machine-learning model, agent, or automated
// system for the purpose of producing a derivative work or reimplementation that is not otherwise permitted by the
// Snap CD Source-Available License (including any Competing Product as defined therein). Contact info@snapcd.io
// for terms covering either use.

using Microsoft.EntityFrameworkCore;
using SnapCd.Contracts;
using SnapCd.Server.Core.Database;
using SnapCd.Server.Core.Entities.Definition;
using SnapCd.Server.Core.Enums;
using SnapCd.Server.Core.Tests.Infrastructure;
using Xunit;

namespace SnapCd.Server.Core.Tests.Tests.Services;

/// <summary>
/// At most one run per (job, mission type) may be non-terminal, enforced by a filtered unique
/// index rather than by application logic. The filter is on a mutable column, so a write that
/// moves a finished run back among the live ones collides with whatever replaced it.
/// </summary>
[Collection("NewRoleBasedSharedFixture")]
public class MissionRunActiveLockTests : IAsyncLifetime
{
    private readonly Fixture _fixture;
    private Module _module = null!;
    private Guid _jobId;
    private Guid _missionId;

    public MissionRunActiveLockTests(Fixture fixture) => _fixture = fixture;

    public async Task InitializeAsync()
    {
        await using var db = _fixture.CreateDbContext();
        _module = db.Modules.First(m => m.Id == _fixture.Modules["0000"].Id);

        _jobId = Guid.NewGuid();
        _missionId = Guid.NewGuid();

        db.ModuleJobs.Add(new ModuleJob
        {
            Id = _jobId,
            OrganizationId = _module.OrganizationId,
            ModuleId = _module.Id,
            TimestampStart = DateTimeOffset.UtcNow,
            Status = ExecutionStatus.Running,
            JobType = "Apply",
            IsCurrent = null
        });
        db.ModuleJobMissions.Add(new ModuleJobMission
        {
            Id = _missionId,
            OrganizationId = _module.OrganizationId,
            ModuleJobId = _jobId,
            MissionId = Guid.NewGuid(),
            AgentId = Guid.NewGuid(),
            MissionType = MissionType.SummarizeJob
        });
        await db.SaveChangesAsync();
    }

    public async Task DisposeAsync()
    {
        await using var db = _fixture.CreateDbContext();
        await db.ModuleJobMissionRuns.Where(r => r.ModuleJobId == _jobId).ExecuteDeleteAsync();
        await db.ModuleJobMissions.Where(m => m.ModuleJobId == _jobId).ExecuteDeleteAsync();
        await db.ModuleJobs.Where(j => j.Id == _jobId).ExecuteDeleteAsync();
    }

    /// <summary>
    /// The watchdog times a silent run out and dispatches another, then the first agent finally
    /// answers. Writing it back to Running is what AgentHub.MissionStarted refuses, and this is
    /// what happens to the write if it does not.
    /// </summary>
    [Fact]
    public async Task A_Finished_Run_Cannot_Rejoin_The_Live_Ones()
    {
        var timedOut = await AddRun(1, MissionStatus.TimedOut);
        await AddRun(2, MissionStatus.Pending);

        await using var db = _fixture.CreateDbContext();
        var run = await db.ModuleJobMissionRuns.SingleAsync(r => r.Id == timedOut);
        run.Status = MissionStatus.Running;

        await Assert.ThrowsAnyAsync<DbUpdateException>(() => db.SaveChangesAsync());
    }

    /// <summary>
    /// The same write with nothing holding the slot. Two terminal runs coexist happily, which is
    /// what makes the index a lock on the live ones rather than on attempts.
    /// </summary>
    [Fact]
    public async Task A_Finished_Run_May_Rejoin_When_Nothing_Replaced_It()
    {
        var timedOut = await AddRun(1, MissionStatus.TimedOut);
        await AddRun(2, MissionStatus.Failed);

        await using var db = _fixture.CreateDbContext();
        var run = await db.ModuleJobMissionRuns.SingleAsync(r => r.Id == timedOut);
        run.Status = MissionStatus.Running;

        await db.SaveChangesAsync();

        var reread = await db.ModuleJobMissionRuns.SingleAsync(r => r.Id == timedOut);
        Assert.Equal(MissionStatus.Running, reread.Status);
    }

    private async Task<Guid> AddRun(int attempt, MissionStatus status)
    {
        var id = Guid.NewGuid();

        await using var db = _fixture.CreateDbContext();
        db.ModuleJobMissionRuns.Add(new ModuleJobMissionRun
        {
            Id = id,
            OrganizationId = _module.OrganizationId,
            ModuleJobMissionId = _missionId,
            ModuleJobId = _jobId,
            MissionType = MissionType.SummarizeJob,
            AgentId = Guid.NewGuid(),
            InvocationId = Guid.NewGuid(),
            AttemptNumber = attempt,
            Status = status,
            DeadlineAt = DateTime.UtcNow.AddMinutes(2)
        });
        await db.SaveChangesAsync();

        return id;
    }
}
