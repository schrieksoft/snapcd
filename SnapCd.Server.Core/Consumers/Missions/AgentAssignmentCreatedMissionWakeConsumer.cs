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
using SnapCd.Server.Core.Events.Repository.Organization;
using SnapCd.Server.Core.Services.Ai.Missions;

namespace SnapCd.Server.Core.Consumers.Missions;

/// <summary>
/// Wakes parked missions when an <c>Agent{Scope}Assignment</c> is created — the supply-side analogue of
/// <see cref="AgentReconnectedMissionWakeConsumer"/>. Scans <see cref="ModuleJobMission"/> rows that are
/// parked <see cref="MissionStatus.BlockedAgentNotAssigned"/> for the affected agent, and re-tries dispatch;
/// the dispatcher's own supply check decides whether the new assignment actually covers the mission's scope
/// (so a new <c>AgentNamespaceAssignment</c> wakes a parked <c>ModuleMission</c> only if that module is in
/// the relevant namespace — the dispatcher's <see cref="AgentSupplyResolver"/> evaluates the OR-chain).
/// Also handles <see cref="AgentUpdatedEvent"/> for the <c>IsAssignedToAllModules</c> flag flipping to
/// true. Competing — one instance per event handles all the parked missions for the affected agent.
/// </summary>
public class AgentAssignmentCreatedMissionWakeConsumer :
    IConsumer<AgentStackAssignmentCreatedEvent>,
    IConsumer<AgentNamespaceAssignmentCreatedEvent>,
    IConsumer<AgentModuleAssignmentCreatedEvent>,
    IConsumer<AgentUpdatedEvent>
{
    private readonly IDbContextFactory<SnapCdDbContext> _dbContextFactory;
    private readonly MissionDispatcher _dispatcher;
    private readonly ILogger<AgentAssignmentCreatedMissionWakeConsumer> _logger;

    public AgentAssignmentCreatedMissionWakeConsumer(
        IDbContextFactory<SnapCdDbContext> dbContextFactory,
        MissionDispatcher dispatcher,
        ILogger<AgentAssignmentCreatedMissionWakeConsumer> logger)
    {
        _dbContextFactory = dbContextFactory;
        _dispatcher = dispatcher;
        _logger = logger;
    }

    public Task Consume(ConsumeContext<AgentStackAssignmentCreatedEvent> context) =>
        WakeAsync(context.Message.Data.AgentId, context.Message.OrganizationId, context.CancellationToken);

    public Task Consume(ConsumeContext<AgentNamespaceAssignmentCreatedEvent> context) =>
        WakeAsync(context.Message.Data.AgentId, context.Message.OrganizationId, context.CancellationToken);

    public Task Consume(ConsumeContext<AgentModuleAssignmentCreatedEvent> context) =>
        WakeAsync(context.Message.Data.AgentId, context.Message.OrganizationId, context.CancellationToken);

    public Task Consume(ConsumeContext<AgentUpdatedEvent> context)
    {
        // Only wake when the org-wide flag is currently true. UpdatedEvent doesn't carry a before/after
        // diff, but the wake is harmless when the flag is true regardless of whether THIS update flipped
        // it (parked missions would already have woken on the original flip). When the flag is false the
        // call is a no-op short-circuit — no parked-mission scan needed.
        return context.Message.Data.IsAssignedToAllModules
            ? WakeAsync(context.Message.Data.Id, context.Message.OrganizationId, context.CancellationToken)
            : Task.CompletedTask;
    }

    private async Task WakeAsync(Guid agentId, Guid organizationId, CancellationToken ct)
    {
        await using var db = await _dbContextFactory.CreateDbContextAsync(ct);

        var parked = await db.ModuleJobMissions
            .Where(m => m.OrganizationId == organizationId
                     && m.AgentId == agentId
                     && m.Status == MissionStatus.BlockedAgentNotAssigned)
            .ToListAsync(ct);
        if (parked.Count == 0)
            return;

        foreach (var mission in parked)
        {
            var lastAttempt = (await db.ModuleJobMissionRuns
                .Where(r => r.ModuleJobMissionId == mission.Id && r.OrganizationId == mission.OrganizationId)
                .Select(r => (int?)r.AttemptNumber)
                .MaxAsync(ct)) ?? 0;

            var outcome = await _dispatcher.DispatchAsync(db, mission, agentId, lastAttempt + 1, ct);

            if (outcome == MissionDispatchOutcome.Dispatched)
            {
                mission.Status = MissionStatus.Pending;
                await db.SaveChangesAsync(ct);
                _logger.LogInformation(
                    "Woken parked mission {MissionId} ({MissionType}, job {JobId}) — agent {AgentId} now supplied to the target scope.",
                    mission.Id, mission.MissionType, mission.ModuleJobId, agentId);
            }
            // Other outcomes (still NoLiveConnection, still AgentNotAssigned for a different mission's
            // scope, AlreadyActive from a racing wake, etc.) leave the mission as-is — the dispatcher's
            // gates decided we're not ready and we'll get the next wake.
        }
    }
}
