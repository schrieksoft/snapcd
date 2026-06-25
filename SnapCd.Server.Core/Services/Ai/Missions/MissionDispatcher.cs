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
using SnapCd.Server.Core.Events.Missions;
using SnapCd.Server.Core.Licensing.Models;
using SnapCd.Server.Core.Licensing.Services;
using SnapCd.Server.Core.Misc.Helpers;
using SnapCd.Server.Core.Repositories.Organizations.Nonsecured;

namespace SnapCd.Server.Core.Services.Ai.Missions;

public enum MissionDispatchOutcome
{
    Dispatched,
    NoLiveConnection,
    AlreadyActive,
    NotLicensed,
    AgentNotAssigned
}

/// <summary>
/// Claims a <see cref="ModuleJobMissionRun"/> under the filtered-unique lock, schedules its deadline
/// check, and directed-Sends the per-mission request to the instance owning the agent's connection.
/// Shared by Layer-1 dispatch, the deadline-check retry, and manual rerun so the claim + lock + send
/// path exists exactly once.
/// </summary>
public class MissionDispatcher
{
    // Time to the agent's first MissionStarted ack before the watchdog fires (generous: the agent
    // loop + Claude can be slow to first output). Subsequent deadlines are heartbeat-driven.
    public static readonly TimeSpan StartGrace = TimeSpan.FromMinutes(3);

    private readonly IBus _bus;
    private readonly IMessageScheduler _scheduler;
    private readonly ILogger<MissionDispatcher> _logger;
    private readonly ILicenseInfoProvider _licenseInfoProvider;
    private readonly AgentSupplyResolver _agentSupply;
    private readonly ModuleJobMissionRunRepositoryFactory _runRepositoryFactory;

    public MissionDispatcher(IBus bus, IMessageScheduler scheduler, ILogger<MissionDispatcher> logger, ILicenseInfoProvider licenseInfoProvider, AgentSupplyResolver agentSupply, ModuleJobMissionRunRepositoryFactory runRepositoryFactory)
    {
        _bus = bus;
        _scheduler = scheduler;
        _logger = logger;
        _licenseInfoProvider = licenseInfoProvider;
        _agentSupply = agentSupply;
        _runRepositoryFactory = runRepositoryFactory;
    }

    /// <summary>
    /// Claim + dispatch one attempt for <paramref name="mission"/> against the given
    /// <paramref name="agentId"/>. Operates on the caller's <paramref name="db"/> (so a retry can move
    /// the prior run terminal in the same transaction before claiming). The caller decides which agent
    /// to try — Layer 1 iterates live-first matches with fallback on <see cref="MissionDispatchOutcome.NoLiveConnection"/>;
    /// the run's <c>AgentId</c> reflects who actually received the dispatch.
    /// </summary>
    public async Task<MissionDispatchOutcome> DispatchAsync(
        SnapCdDbContext db, ModuleJobMission mission, Guid agentId, int attemptNumber, CancellationToken ct)
    {
        var licenseInfo = await _licenseInfoProvider.GetLicenseInfoAsync(mission.OrganizationId);
        if (!licenseInfo.Includes(Feature.AiAgents))
        {
            _logger.LogInformation(
                "Mission {MissionType} (job {JobId}) not dispatched: organization {OrganizationId} is not licensed for AiAgents.",
                mission.MissionType, mission.ModuleJobId, mission.OrganizationId);
            return MissionDispatchOutcome.NotLicensed;
        }

        var connection = await db.AgentConnections
            .FirstOrDefaultAsync(c => c.AgentId == agentId && c.OrganizationId == mission.OrganizationId, ct);
        if (connection is null)
        {
            _logger.LogInformation(
                "Mission {MissionType} (job {JobId}) has no live connection for agent {AgentId}; not dispatched.",
                mission.MissionType, mission.ModuleJobId, agentId);
            return MissionDispatchOutcome.NoLiveConnection;
        }

        var moduleId = await db.ModuleJobs
            .Where(j => j.Id == mission.ModuleJobId && j.OrganizationId == mission.OrganizationId)
            .Select(j => j.ModuleId)
            .FirstOrDefaultAsync(ct);

        if (!await _agentSupply.IsAgentSuppliedToModule(agentId, moduleId, mission.OrganizationId))
        {
            _logger.LogInformation(
                "Mission {MissionType} (job {JobId}) not dispatched: agent {AgentId} is not assigned to module {ModuleId} (no covering Agent{{Stack,Namespace,Module}}Assignment and IsSuppliedToAllModules is false).",
                mission.MissionType, mission.ModuleJobId, agentId, moduleId);
            return MissionDispatchOutcome.AgentNotAssigned;
        }

        var invocationId = NewId.NextGuid();
        var deadlineAt = DateTime.UtcNow.Add(StartGrace);

        var run = new ModuleJobMissionRun
        {
            Id = NewId.NextGuid(),
            OrganizationId = mission.OrganizationId,
            ModuleJobMissionId = mission.Id,
            ModuleJobId = mission.ModuleJobId,
            MissionType = mission.MissionType,
            AgentId = agentId,
            InvocationId = invocationId,
            AttemptNumber = attemptNumber,
            Status = MissionStatus.Pending,
            DeadlineAt = deadlineAt,
            AgentConnectionId = connection.Id,
            ServerInstanceId = connection.ServerInstanceId,
            SignalRConnectionId = connection.SignalRConnectionId
        };
        // Claim through the non-secured repository so the run gets its audit fields + a CreatedEvent.
        // System-attributed (this runs in a consumer, no principal). Uses the repo's own context — that's
        // safe because the lock is a DB-level filtered-unique index, not a same-transaction guarantee:
        // a racing claim still throws DbUpdateException, and on a retry the caller has already moved +
        // committed the prior run terminal before calling here (see MissionRunDeadlineCheckConsumer).
        using var runRepo = _runRepositoryFactory.Create();
        try
        {
            await runRepo.Create(run);
        }
        catch (DbUpdateException)
        {
            // Filtered-unique lock violation: a non-terminal run for this (job, type) already exists.
            // Two consumers raced; this one loses. The DB is the arbiter — no double-run.
            _logger.LogInformation(
                "Mission {MissionType} (job {JobId}) already has an active run; claim dropped (locked).",
                mission.MissionType, mission.ModuleJobId);
            return MissionDispatchOutcome.AlreadyActive;
        }

        await _scheduler.SchedulePublish(deadlineAt,
            new MissionRunDeadlineCheck { RunId = run.Id, OrganizationId = mission.OrganizationId }, ct);

        MissionRequestedBase message = mission.MissionType switch
        {
            MissionType.AutoDiagnose => new AutoDiagnoseMissionRequested { JobId = mission.ModuleJobId, ModuleId = moduleId },
            MissionType.ApprovalRecommend => new ApprovalRecommendMissionRequested { JobId = mission.ModuleJobId, ModuleId = moduleId },
            MissionType.SummarizeJob => new SummarizeJobMissionRequested { JobId = mission.ModuleJobId, ModuleId = moduleId },
            MissionType.AutoFix => new AutoFixMissionRequested { JobId = mission.ModuleJobId, ModuleId = moduleId },
            _ => throw new InvalidOperationException($"MissionDispatcher does not handle {mission.MissionType} (job-scoped only).")
        };

        message.InvocationId = invocationId;
        message.RunId = run.Id;
        message.OrganizationId = mission.OrganizationId;
        message.AgentId = agentId;
        message.MissionId = mission.MissionId;
        message.AgentConnectionId = connection.SignalRConnectionId;
        message.SidecarName = mission.SidecarName;

        var endpointUri = MassTransitHelpers.GetAgentConsumerEndpoint(connection.ServerInstanceId, message.GetType().Name);
        var endpoint = await _bus.GetSendEndpoint(new Uri(endpointUri));
        await endpoint.Send(message, message.GetType(), ct);

        _logger.LogInformation(
            "Dispatched {MissionType} run {RunId} (attempt {Attempt}) for job {JobId} to instance {ServerInstanceId}.",
            mission.MissionType, run.Id, attemptNumber, mission.ModuleJobId, connection.ServerInstanceId);
        return MissionDispatchOutcome.Dispatched;
    }
}
