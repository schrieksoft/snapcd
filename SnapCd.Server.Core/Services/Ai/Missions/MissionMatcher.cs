// SPDX-License-Identifier: LicenseRef-Snap-CD-Source-Available-1.1
// Copyright (c) 2026 Karl Schriek / Schrieksoft.
// No license is granted to use this file, in whole or in part, (a) as training, fine-tuning, retrieval, or
// embedding data for any machine-learning model, or (b) as input to any machine-learning model, agent, or automated
// system for the purpose of producing a derivative work or reimplementation that is not otherwise permitted by the
// Snap CD Source-Available License (including any Competing Product as defined therein). Contact info@snapcd.io
// for terms covering either use.

using MassTransit;
using Microsoft.EntityFrameworkCore;
using SnapCd.Contracts;
using SnapCd.Server.Core.Database;
using SnapCd.Server.Core.Entities.Definition;

namespace SnapCd.Server.Core.Services.Ai.Missions;

/// <summary>
/// Shared Layer-1 mission match-and-dispatch logic. Each event-specific Layer-1 competing consumer
/// (job failed → AutoDiagnose, job succeeded → SummarizeJob, awaiting approval → ApprovalRecommend)
/// delegates here with the single mission type it triggers. Resolves the job's module scope chain,
/// queries the four scoped mission tables for active matches, prefers live-agent matches, falls
/// through to the next match on <see cref="MissionDispatchOutcome.NoLiveConnection"/>, and parks
/// the mission <see cref="MissionStatus.WaitingForAgent"/> if every match is offline — the
/// <c>AgentReconnectedMissionWakeConsumer</c> picks them up when an agent comes online.
/// </summary>
public class MissionMatcher
{
    private readonly IDbContextFactory<SnapCdDbContext> _dbContextFactory;
    private readonly MissionDispatcher _dispatcher;
    private readonly ILogger<MissionMatcher> _logger;

    public MissionMatcher(
        IDbContextFactory<SnapCdDbContext> dbContextFactory,
        MissionDispatcher dispatcher,
        ILogger<MissionMatcher> logger)
    {
        _dbContextFactory = dbContextFactory;
        _dispatcher = dispatcher;
        _logger = logger;
    }

    public async Task MatchAndDispatchAsync(
        Guid moduleId, Guid organizationId, Guid jobId, MissionType triggeredType, CancellationToken ct)
    {
        await using var db = await _dbContextFactory.CreateDbContextAsync(ct);

        var scope = await db.Modules
            .Where(m => m.Id == moduleId && m.OrganizationId == organizationId)
            .Select(m => new { m.NamespaceId, m.Namespace.StackId })
            .FirstOrDefaultAsync(ct);
        if (scope is null) return;

        var matches = await GatherMatchesAsync(db, organizationId, moduleId, scope.NamespaceId, scope.StackId, triggeredType, ct);
        if (matches.Count == 0)
            return;

        var liveAgents = await db.AgentConnections
            .Where(c => c.OrganizationId == organizationId)
            .Select(c => c.AgentId)
            .Distinct()
            .ToListAsync(ct);
        var liveSet = liveAgents.ToHashSet();

        var ordered = matches
            .OrderBy(m => liveSet.Contains(m.AgentId) ? 0 : 1)
            .ThenBy(m => m.MissionId)
            .ToList();

        ModuleJobMission? mission = null;
        var dispatched = false;
        var sawAgentNotAssigned = false;
        var sawNoLiveConnection = false;
        foreach (var match in ordered)
        {
            mission ??= await GetOrCreateMissionAsync(db, organizationId, jobId, match, ct);
            if (mission is null) break;

            if (mission.AgentId != match.AgentId)
            {
                mission.AgentId = match.AgentId;
                await db.SaveChangesAsync(ct);
            }

            var outcome = await _dispatcher.DispatchAsync(db, mission, match.AgentId, attemptNumber: 1, ct);
            if (outcome is MissionDispatchOutcome.Dispatched or MissionDispatchOutcome.AlreadyActive)
            {
                dispatched = true;
                break;
            }
            if (outcome == MissionDispatchOutcome.AgentNotAssigned) sawAgentNotAssigned = true;
            if (outcome == MissionDispatchOutcome.NoLiveConnection) sawNoLiveConnection = true;
        }

        if (mission is not null && !dispatched)
        {
            // Distinct park statuses: NoLiveConnection is operational (agent will come back), AgentNotAssigned
            // is configuration (Agent owner must wire up an assignment). If both were observed across the
            // iteration, prefer WaitingForAgent — the connection path is more likely to self-heal, and once
            // the agent reconnects the supply gate runs again at dispatch time.
            mission.Status = sawNoLiveConnection || !sawAgentNotAssigned
                ? MissionStatus.WaitingForAgent
                : MissionStatus.BlockedAgentNotAssigned;
            await db.SaveChangesAsync(ct);
            _logger.LogInformation(
                "Mission {MissionType} (job {JobId}) parked {Status} — no live match.",
                mission.MissionType, jobId, mission.Status);
        }
    }

    private static async Task<ModuleJobMission?> GetOrCreateMissionAsync(
        SnapCdDbContext db, Guid organizationId, Guid jobId, MissionMatch match, CancellationToken ct)
    {
        var existing = await db.ModuleJobMissions.FirstOrDefaultAsync(
            m => m.ModuleJobId == jobId && m.MissionType == match.MissionType && m.OrganizationId == organizationId, ct);
        if (existing is not null)
            return existing;

        var mission = new ModuleJobMission
        {
            Id = NewId.NextGuid(),
            OrganizationId = organizationId,
            ModuleJobId = jobId,
            MissionId = match.MissionId,
            AgentId = match.AgentId,
            MissionType = match.MissionType,
            SidecarName = match.SidecarName,
            Status = MissionStatus.Pending
        };
        db.ModuleJobMissions.Add(mission);
        try
        {
            await db.SaveChangesAsync(ct);
            return mission;
        }
        catch (DbUpdateException)
        {
            db.ChangeTracker.Clear();
            return await db.ModuleJobMissions.FirstOrDefaultAsync(
                m => m.ModuleJobId == jobId && m.MissionType == match.MissionType && m.OrganizationId == organizationId, ct);
        }
    }

    private static async Task<List<MissionMatch>> GatherMatchesAsync(
        SnapCdDbContext db, Guid orgId, Guid moduleId, Guid namespaceId, Guid stackId,
        MissionType triggeredType, CancellationToken ct)
    {
        var matches = new List<MissionMatch>();

        matches.AddRange(await db.OrganizationMissions
            .Where(x => x.OrganizationId == orgId && !x.IsDisabled && x.MissionType == triggeredType)
            .Select(x => new MissionMatch(x.AgentId, x.Id, x.MissionType, x.SidecarName)).ToListAsync(ct));

        matches.AddRange(await db.StackMissions
            .Where(x => x.OrganizationId == orgId && x.StackId == stackId && !x.IsDisabled && x.MissionType == triggeredType)
            .Select(x => new MissionMatch(x.AgentId, x.Id, x.MissionType, x.SidecarName)).ToListAsync(ct));

        matches.AddRange(await db.ModuleMissions
            .Where(x => x.OrganizationId == orgId && x.ModuleId == moduleId && !x.IsDisabled && x.MissionType == triggeredType)
            .Select(x => new MissionMatch(x.AgentId, x.Id, x.MissionType, x.SidecarName)).ToListAsync(ct));

        matches.AddRange(await db.NamespaceMissions
            .Where(x => x.OrganizationId == orgId && x.NamespaceId == namespaceId && !x.IsDisabled && x.MissionType == triggeredType)
            .Select(x => new MissionMatch(x.AgentId, x.Id, x.MissionType, x.SidecarName)).ToListAsync(ct));

        return matches;
    }

    private readonly record struct MissionMatch(Guid AgentId, Guid MissionId, MissionType MissionType, string? SidecarName);
}
