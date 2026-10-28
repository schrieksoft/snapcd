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

namespace SnapCd.Server.Core.Services.Nonsecured.Ai.Missions;

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
    private readonly SnapCdDbContext _dbContext;
    private readonly MissionDispatcher _dispatcher;
    private readonly ILogger<MissionMatcher> _logger;

    public MissionMatcher(
        SnapCdDbContext dbContext,
        MissionDispatcher dispatcher,
        ILogger<MissionMatcher> logger)
    {
        _dbContext = dbContext;
        _dispatcher = dispatcher;
        _logger = logger;
    }

    /// <summary>
    /// Returns <c>true</c> if at least one mission of <paramref name="triggeredType"/> was configured for
    /// the job's scope (whether or not it dispatched to a live agent — an offline match parks and wakes
    /// later). Callers use this to express precedence between trigger-sharing mission types.
    /// </summary>
    public async Task<bool> MatchAndDispatchAsync(
        Guid moduleId, Guid organizationId, Guid jobId, MissionType triggeredType, CancellationToken ct)
    {

        var scope = await _dbContext.Modules
            .Where(m => m.Id == moduleId && m.OrganizationId == organizationId)
            .Select(m => new { m.NamespaceId, m.Namespace.StackId })
            .FirstOrDefaultAsync(ct);
        if (scope is null) return false;

        var matches = await GatherMatchesAsync(_dbContext, organizationId, moduleId, scope.NamespaceId, scope.StackId, triggeredType, ct);
        if (matches.Count == 0)
            return false;

        var liveAgents = await _dbContext.AgentConnections
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
            mission ??= await GetOrCreateMissionAsync(_dbContext, organizationId, jobId, match, ct);
            if (mission is null) break;

            if (mission.AgentId != match.AgentId)
            {
                mission.AgentId = match.AgentId;
                await _dbContext.SaveChangesAsync(ct);
            }

            var outcome = await _dispatcher.DispatchAsync(_dbContext, mission, match.AgentId, attemptNumber: 1, ct);
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
            await _dbContext.SaveChangesAsync(ct);
            _logger.LogInformation(
                "Mission {MissionType} (job {JobId}) parked {Status} — no live match.",
                mission.MissionType, jobId, mission.Status);
        }

        return true;
    }

    private static async Task<ModuleJobMission?> GetOrCreateMissionAsync(
        SnapCdDbContext _dbContext, Guid organizationId, Guid jobId, MissionMatch match, CancellationToken ct)
    {
        var existing = await _dbContext.ModuleJobMissions.FirstOrDefaultAsync(
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
        _dbContext.ModuleJobMissions.Add(mission);
        try
        {
            await _dbContext.SaveChangesAsync(ct);
            return mission;
        }
        catch (DbUpdateException)
        {
            _dbContext.ChangeTracker.Clear();
            return await _dbContext.ModuleJobMissions.FirstOrDefaultAsync(
                m => m.ModuleJobId == jobId && m.MissionType == match.MissionType && m.OrganizationId == organizationId, ct);
        }
    }

    private static async Task<List<MissionMatch>> GatherMatchesAsync(
        SnapCdDbContext _dbContext, Guid orgId, Guid moduleId, Guid namespaceId, Guid stackId,
        MissionType triggeredType, CancellationToken ct)
    {
        var matches = new List<MissionMatch>();

        matches.AddRange(await _dbContext.OrganizationMissions
            .Where(x => x.OrganizationId == orgId && !x.IsDisabled && x.MissionType == triggeredType)
            .Select(x => new MissionMatch(x.AgentId, x.Id, x.MissionType, x.SidecarName)).ToListAsync(ct));

        matches.AddRange(await _dbContext.StackMissions
            .Where(x => x.OrganizationId == orgId && x.StackId == stackId && !x.IsDisabled && x.MissionType == triggeredType)
            .Select(x => new MissionMatch(x.AgentId, x.Id, x.MissionType, x.SidecarName)).ToListAsync(ct));

        matches.AddRange(await _dbContext.ModuleMissions
            .Where(x => x.OrganizationId == orgId && x.ModuleId == moduleId && !x.IsDisabled && x.MissionType == triggeredType)
            .Select(x => new MissionMatch(x.AgentId, x.Id, x.MissionType, x.SidecarName)).ToListAsync(ct));

        matches.AddRange(await _dbContext.NamespaceMissions
            .Where(x => x.OrganizationId == orgId && x.NamespaceId == namespaceId && !x.IsDisabled && x.MissionType == triggeredType)
            .Select(x => new MissionMatch(x.AgentId, x.Id, x.MissionType, x.SidecarName)).ToListAsync(ct));

        return matches;
    }

    private readonly record struct MissionMatch(Guid AgentId, Guid MissionId, MissionType MissionType, string? SidecarName);
}
