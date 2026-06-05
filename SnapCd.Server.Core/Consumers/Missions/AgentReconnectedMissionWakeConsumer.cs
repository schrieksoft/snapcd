using MassTransit;
using Microsoft.EntityFrameworkCore;
using SnapCd.Contracts;
using SnapCd.Server.Core.Database;
using SnapCd.Server.Core.Entities.Definition;
using SnapCd.Server.Core.Events.Agents;
using SnapCd.Server.Core.Services.Ai.Missions;

namespace SnapCd.Server.Core.Consumers.Missions;

/// <summary>
/// Wakes parked missions when an agent reconnects — the runner's <c>XWaitingForRunner</c> +
/// <c>RunnerReconnectedEvent</c> pattern, saga-less. Scans <see cref="ModuleJobMission"/> rows that
/// Layer 1 (or the deadline-check retry) left as <see cref="MissionStatus.WaitingForAgent"/> for this
/// org, and re-tries dispatch via the reconnecting agent — but only for the ones the reconnecting
/// agent actually covers (its scope chain has an active mission row of the right type for the job's
/// module). Competing — one instance per reconnect handles all parked missions.
/// </summary>
public class AgentReconnectedMissionWakeConsumer : IConsumer<AgentReconnectedEvent>
{
    private readonly IDbContextFactory<SnapCdDbContext> _dbContextFactory;
    private readonly MissionDispatcher _dispatcher;
    private readonly ILogger<AgentReconnectedMissionWakeConsumer> _logger;

    public AgentReconnectedMissionWakeConsumer(
        IDbContextFactory<SnapCdDbContext> dbContextFactory,
        MissionDispatcher dispatcher,
        ILogger<AgentReconnectedMissionWakeConsumer> logger)
    {
        _dbContextFactory = dbContextFactory;
        _dispatcher = dispatcher;
        _logger = logger;
    }

    public async Task Consume(ConsumeContext<AgentReconnectedEvent> context)
    {
        var msg = context.Message;
        var ct = context.CancellationToken;
        await using var db = await _dbContextFactory.CreateDbContextAsync(ct);

        var parked = await db.ModuleJobMissions
            .Where(m => m.OrganizationId == msg.OrganizationId && m.Status == MissionStatus.WaitingForAgent)
            .ToListAsync(ct);
        if (parked.Count == 0)
            return;

        foreach (var mission in parked)
        {
            if (!await AgentCoversMissionAsync(db, msg.AgentId, mission, ct))
                continue;

            var nextAttempt = (await db.ModuleJobMissionRuns
                .Where(r => r.ModuleJobMissionId == mission.Id && r.OrganizationId == mission.OrganizationId)
                .Select(r => (int?)r.AttemptNumber)
                .MaxAsync(ct)) ?? 0;

            mission.AgentId = msg.AgentId;
            await db.SaveChangesAsync(ct);

            var outcome = await _dispatcher.DispatchAsync(db, mission, msg.AgentId, nextAttempt + 1, ct);
            if (outcome == MissionDispatchOutcome.Dispatched)
            {
                mission.Status = MissionStatus.Pending;
                await db.SaveChangesAsync(ct);
                _logger.LogInformation(
                    "Woken parked mission {MissionId} ({MissionType}, job {JobId}) via agent {AgentId} reconnect.",
                    mission.Id, mission.MissionType, mission.ModuleJobId, msg.AgentId);
            }
            // Anything other than Dispatched: stays parked (race / lock contention / connection dropped
            // again). Next reconnect tick — for this or another covering agent — will try again.
        }
    }

    /// <summary>
    /// Does <paramref name="agentId"/> have an active mission row of <c>mission.MissionType</c> whose
    /// scope contains the job's module? Mirrors the scope check in <c>ApplyJobFailedCompetingConsumer.GatherMatches</c>,
    /// narrowed to one agent + one type.
    /// </summary>
    private static async Task<bool> AgentCoversMissionAsync(
        SnapCdDbContext db, Guid agentId, ModuleJobMission mission, CancellationToken ct)
    {
        var orgId = mission.OrganizationId;
        var moduleId = await db.ModuleJobs
            .Where(j => j.Id == mission.ModuleJobId && j.OrganizationId == orgId)
            .Select(j => j.ModuleId)
            .FirstOrDefaultAsync(ct);
        if (moduleId == Guid.Empty) return false;

        var module = await db.Modules
            .FirstOrDefaultAsync(m => m.Id == moduleId && m.OrganizationId == orgId, ct);
        if (module is null) return false;

        var namespaceId = module.NamespaceId;
        var stackId = await db.Namespaces
            .Where(n => n.Id == namespaceId && n.OrganizationId == orgId)
            .Select(n => (Guid?)n.StackId)
            .FirstOrDefaultAsync(ct);

        var type = mission.MissionType;

        if (await db.OrganizationMissions.AnyAsync(
                x => x.OrganizationId == orgId && x.AgentId == agentId && x.MissionType == type && !x.IsDisabled, ct))
            return true;
        if (await db.ModuleMissions.AnyAsync(
                x => x.OrganizationId == orgId && x.ModuleId == moduleId && x.AgentId == agentId && x.MissionType == type && !x.IsDisabled, ct))
            return true;
        if (await db.NamespaceMissions.AnyAsync(
                x => x.OrganizationId == orgId && x.NamespaceId == namespaceId && x.AgentId == agentId && x.MissionType == type && !x.IsDisabled, ct))
            return true;
        if (stackId is { } sid && await db.StackMissions.AnyAsync(
                x => x.OrganizationId == orgId && x.StackId == sid && x.AgentId == agentId && x.MissionType == type && !x.IsDisabled, ct))
            return true;

        return false;
    }
}
