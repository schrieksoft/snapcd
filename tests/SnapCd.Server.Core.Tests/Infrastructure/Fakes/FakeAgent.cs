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
using SnapCd.Server.Core.Services.CallerContext;
using CallerCtx = SnapCd.Server.Core.Services.CallerContext.CallerContext;

namespace SnapCd.Server.Core.Tests.Infrastructure.Fakes;

/// <summary>
/// Stands in for a connected agent running Layer-2 missions. Mirrors the AgentHub writes that
/// matter to a maintenance window — heartbeats pushing DeadlineAt forward, and terminal
/// transitions — under the agent's caller scope, so mission progress is subject to the same
/// exemptions the real hub filter grants.
/// </summary>
public class FakeAgent
{
    /// <summary>Mirrors AgentHub.ServerTimeout: the window a heartbeat pushes the deadline to.</summary>
    public static readonly TimeSpan ServerTimeout = TimeSpan.FromMinutes(2);

    private readonly IDbContextFactory<SnapCdDbContext> _dbContextFactory;

    public FakeAgent(IDbContextFactory<SnapCdDbContext> dbContextFactory, Guid organizationId, Guid agentId)
    {
        _dbContextFactory = dbContextFactory;
        OrganizationId = organizationId;
        AgentId = agentId;
    }

    public Guid OrganizationId { get; }
    public Guid AgentId { get; }
    public Guid ServerInstanceId { get; } = Guid.NewGuid();

    /// <summary>Creates a mission and its first run, as the dispatcher would on assignment.</summary>
    public async Task<(Guid MissionId, Guid RunId, Guid InvocationId)> StartMissionAsync(Guid moduleJobId, MissionType missionType)
    {
        using var _ = CallerCtx.Begin(CallerKind.Agent);
        await using var db = await _dbContextFactory.CreateDbContextAsync();

        var missionId = Guid.NewGuid();
        db.ModuleJobMissions.Add(new ModuleJobMission
        {
            Id = missionId,
            OrganizationId = OrganizationId,
            ModuleJobId = moduleJobId,
            MissionId = Guid.NewGuid(),
            AgentId = AgentId,
            MissionType = missionType,
            Status = MissionStatus.Running
        });

        var runId = Guid.NewGuid();
        var invocationId = Guid.NewGuid();
        db.ModuleJobMissionRuns.Add(new ModuleJobMissionRun
        {
            Id = runId,
            OrganizationId = OrganizationId,
            ModuleJobMissionId = missionId,
            ModuleJobId = moduleJobId,
            MissionType = missionType,
            AgentId = AgentId,
            InvocationId = invocationId,
            AttemptNumber = 1,
            Status = MissionStatus.Running,
            StartedAt = DateTime.UtcNow,
            DeadlineAt = DateTime.UtcNow.Add(ServerTimeout),
            ServerInstanceId = ServerInstanceId,
            SignalRConnectionId = $"agent-{AgentId:N}"
        });

        await db.SaveChangesAsync();
        return (missionId, runId, invocationId);
    }

    /// <summary>The 30-second liveness ping: pushes DeadlineAt out and resumes an awaiting run.</summary>
    public async Task HeartbeatAsync(Guid invocationId, DateTime? lastEventAt = null)
    {
        using var _ = CallerCtx.Begin(CallerKind.Agent);
        await using var db = await _dbContextFactory.CreateDbContextAsync();
        var run = await db.ModuleJobMissionRuns.SingleOrDefaultAsync(r => r.InvocationId == invocationId);
        if (run == null) return;
        if (run.Status is MissionStatus.Succeeded or MissionStatus.Failed or MissionStatus.Cancelled or MissionStatus.TimedOut) return;

        if (run.Status == MissionStatus.AwaitingReconnect)
        {
            run.Status = MissionStatus.Running;
            run.ServerInstanceId = ServerInstanceId;
        }

        run.DeadlineAt = DateTime.UtcNow.Add(ServerTimeout);
        if (lastEventAt is not null) run.LastEventAt = lastEventAt;
        await db.SaveChangesAsync();
    }

    public async Task CompleteMissionAsync(Guid invocationId, string? resultSummary = "done")
    {
        using var _ = CallerCtx.Begin(CallerKind.Agent);
        await using var db = await _dbContextFactory.CreateDbContextAsync();
        var run = await db.ModuleJobMissionRuns.SingleAsync(r => r.InvocationId == invocationId);
        run.Status = MissionStatus.Succeeded;
        run.CompletedAt = DateTime.UtcNow;
        run.ResultSummary = resultSummary;

        var mission = await db.ModuleJobMissions.SingleAsync(m => m.Id == run.ModuleJobMissionId);
        mission.Status = MissionStatus.Succeeded;
        mission.CompletedAt = DateTime.UtcNow;
        await db.SaveChangesAsync();
    }

    /// <summary>Drops the connection: the run parks awaiting reconnect, as the hub does on disconnect.</summary>
    public async Task DisconnectAsync(Guid invocationId)
    {
        using var _ = CallerCtx.Begin(CallerKind.Agent);
        await using var db = await _dbContextFactory.CreateDbContextAsync();
        var run = await db.ModuleJobMissionRuns.SingleAsync(r => r.InvocationId == invocationId);
        if (run.Status is MissionStatus.Succeeded or MissionStatus.Failed or MissionStatus.Cancelled or MissionStatus.TimedOut) return;
        run.Status = MissionStatus.AwaitingReconnect;
        run.SignalRConnectionId = null;
        await db.SaveChangesAsync();
    }
}
