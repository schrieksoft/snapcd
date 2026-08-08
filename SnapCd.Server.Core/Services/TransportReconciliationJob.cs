// SPDX-License-Identifier: LicenseRef-Snap-CD-Source-Available-1.1
// Copyright (c) 2026 Karl Schriek / Schrieksoft.
// No license is granted to use this file, in whole or in part, (a) as training, fine-tuning, retrieval, or
// embedding data for any machine-learning model, or (b) as input to any machine-learning model, agent, or automated
// system for the purpose of producing a derivative work or reimplementation that is not otherwise permitted by the
// Snap CD Source-Available License (including any Competing Product as defined therein). Contact info@snapcd.io
// for terms covering either use.

using MassTransit;
using SnapCd.Contracts;
using Microsoft.EntityFrameworkCore;
using SnapCd.Server.Core.Database;
using SnapCd.Server.Core.Events.Steps;
using SnapCd.Server.Core.Entities.Sagas;
using SnapCd.Server.Core.Events.Gatekeeping;
using SnapCd.Server.Core.Events.Missions;
using SnapCd.Server.Core.Events.System;
using SnapCd.Server.Core.Services.ResolvedConfiguration.HelperClasses;
using System.Text.Json;

namespace SnapCd.Server.Core.Services;

/// <summary>
/// Re-derives transport-resident timers from database state. Scheduled messages live in the
/// transport and do not survive a transport change, so every timer the system depends on must
/// be reconstructible from what the database holds. Safe to run at any time: a republished
/// tick supersedes a live cycle instead of forking it, and states without a heartbeat ignore it.
/// </summary>
public class TransportReconciliationJob
{
    private static readonly TimeSpan PastDeadlineGrace = TimeSpan.FromHours(1);

    // An alive agent pushes DeadlineAt forward within seconds of reconnecting.
    private static readonly TimeSpan MissionReconnectGrace = TimeSpan.FromMinutes(5);

    private readonly IDbContextFactory<SnapCdDbContext> _dbContextFactory;
    private readonly IBus _bus;
    private readonly IMessageScheduler _scheduler;
    private readonly QuotaService _quotaService;
    private readonly ILogger<TransportReconciliationJob> _logger;

    public TransportReconciliationJob(
        IDbContextFactory<SnapCdDbContext> dbContextFactory,
        IBus bus,
        IMessageScheduler scheduler,
        QuotaService quotaService,
        ILogger<TransportReconciliationJob> logger)
    {
        _dbContextFactory = dbContextFactory;
        _bus = bus;
        _scheduler = scheduler;
        _quotaService = quotaService;
        _logger = logger;
    }

    public async Task ExecuteJob()
    {
        using var _ = SnapCd.Server.Core.Services.CallerContext.CallerContext.Begin(SnapCd.Server.Core.Services.CallerContext.CallerKind.System);
        await using var dbContext = await _dbContextFactory.CreateDbContextAsync();

        var heartbeats = await RearmHeartbeats(dbContext);
        var approvals = await RearmApprovalTimeouts(dbContext);
        var driftChecks = await RearmDriftChecks(dbContext);
        var debounces = await RearmDebounces(dbContext);
        var missions = await RearmMissionDeadlines(dbContext);
        var selects = await RedriveSelectRunnerInstance(dbContext);
        var strandedCancels = await FlagStrandedCancellations(dbContext);

        _logger.LogInformation(
            "Transport reconciliation complete: {HeartbeatCount} heartbeat ticks republished, {ApprovalCount} approval timeouts rescheduled, {DriftCount} drift checks rescheduled, {DebounceCount} debounces flushed, {MissionCount} mission deadline checks rescheduled, {SelectCount} runner selections re-driven, {StrandedCancelCount} stranded cancellations flagged",
            heartbeats, approvals, driftChecks, debounces, missions, selects, strandedCancels);
    }


    // Republishing the trigger makes a parked debounce re-arm through the machine's own
    // Schedule call, so the flush tick is built and delivered by the current transport.
    private async Task<int> RearmDebounces(SnapCdDbContext dbContext)
    {
        var waiting = await dbContext.Set<ModuleModifiedSaga>().AsNoTracking()
            .Where(s => s.CurrentState == "WaitingForMoreEvents")
            .Select(s => new { s.CorrelationId, s.OrganizationId })
            .ToListAsync();

        foreach (var saga in waiting)
        {
            await _bus.Publish(new ModuleModifiedTriggerRequested
            {
                ModuleId = saga.CorrelationId,
                OrganizationId = saga.OrganizationId
            });

            _logger.LogDebug("Re-armed modified-debounce for module {ModuleId}", saga.CorrelationId);
        }

        return waiting.Count;
    }

    // The deadline check re-arms itself from the run's current DeadlineAt when it fires early,
    // so scheduling at a stale deadline is self-correcting once the agent heartbeats again.
    private async Task<int> RearmMissionDeadlines(SnapCdDbContext dbContext)
    {
        var now = DateTime.UtcNow;

        var active = await dbContext.ModuleJobMissionRuns.AsNoTracking()
            .Where(r => r.Status == MissionStatus.Running || r.Status == MissionStatus.AwaitingReconnect)
            .Select(r => new { r.Id, r.OrganizationId, r.DeadlineAt })
            .ToListAsync();

        foreach (var run in active)
        {
            var checkAt = run.DeadlineAt > now ? run.DeadlineAt : now.Add(MissionReconnectGrace);

            await _scheduler.SchedulePublish(checkAt, new MissionRunDeadlineCheck
            {
                RunId = run.Id,
                OrganizationId = run.OrganizationId
            });

            _logger.LogDebug("Rescheduled mission deadline check for run {RunId} at {CheckAt:O}", run.Id, checkAt);
        }

        return active.Count;
    }


    // SelectRunnerInstancePending is the one pending state without a heartbeat: the request is
    // served by a competing server-side consumer, so a lost message strands the saga with no
    // watchdog. The request is fully reconstructible from the saga row and the selection is
    // idempotent, so it is simply republished.
    private async Task<int> RedriveSelectRunnerInstance(SnapCdDbContext dbContext)
    {
        var stranded = (await dbContext.ApplyJobSagas.AsNoTracking()
                .Where(s => s.CurrentState == "SelectRunnerInstancePending")
                .Select(s => new { s.CorrelationId, s.OrganizationId, s.RunnerId, s.DeclaredJson })
                .ToListAsync())
            .Concat(await dbContext.DestroyJobSagas.AsNoTracking()
                .Where(s => s.CurrentState == "SelectRunnerInstancePending")
                .Select(s => new { s.CorrelationId, s.OrganizationId, s.RunnerId, s.DeclaredJson })
                .ToListAsync())
            .ToList();

        foreach (var saga in stranded)
        {
            await _bus.Publish(new SelectRunnerInstanceRequested
            {
                RunnerId = saga.RunnerId,
                CorrelationId = saga.CorrelationId,
                OrganizationId = saga.OrganizationId,
                Declared = JsonSerializer.Deserialize<ResolvedModule>(saga.DeclaredJson)!
            });

            _logger.LogDebug("Re-drove runner selection for job {JobId}", saga.CorrelationId);
        }

        return stranded.Count;
    }

    // A cancel request in flight uses a transport-scheduled timeout and targets a specific server
    // instance; neither is safely reconstructible here. These sagas need the operator: re-cancel
    // from the UI, or fail the job directly.
    private async Task<int> FlagStrandedCancellations(SnapCdDbContext dbContext)
    {
        string[] cancellingStates = ["CancellingImmediateKill", "CancellingImmediateGraceful"];

        var stranded = (await dbContext.ApplyJobSagas.AsNoTracking()
                .Where(s => cancellingStates.Contains(s.CurrentState))
                .Select(s => new { s.CorrelationId, s.CurrentState })
                .ToListAsync())
            .Concat(await dbContext.DestroyJobSagas.AsNoTracking()
                .Where(s => cancellingStates.Contains(s.CurrentState))
                .Select(s => new { s.CorrelationId, s.CurrentState })
                .ToListAsync())
            .ToList();

        foreach (var saga in stranded)
            _logger.LogWarning(
                "Job {JobId} is stranded in {State}: its cancel request and timeout did not survive the transport; cancel it again or fail it manually",
                saga.CorrelationId, saga.CurrentState);

        return stranded.Count;
    }

    // A non-null DriftCheckScheduleTokenId marks a drift check that was armed and neither fired
    // nor unscheduled. The interval is recomputed with the same precedence the scheduling
    // activity uses, and drift of up to one interval is accepted by design. The fresh token is
    // written back so the superseded-schedule guard can drop any still-live older tick.
    private async Task<int> RearmDriftChecks(SnapCdDbContext dbContext)
    {
        var armed = await dbContext.ModuleSagas
            .Where(s => s.DriftCheckScheduleTokenId != null)
            .Select(s => new
            {
                Saga = s,
                s.Module.DriftCheckEnabled,
                s.Module.DriftCheckIntervalMinutes,
                NamespaceEnabled = s.Module.Namespace.DefaultDriftCheckEnabled,
                NamespaceInterval = s.Module.Namespace.DefaultDriftCheckIntervalMinutes
            })
            .ToListAsync();

        var rearmed = 0;
        foreach (var entry in armed)
        {
            if (!(entry.DriftCheckEnabled ?? entry.NamespaceEnabled ?? false))
            {
                entry.Saga.DriftCheckScheduleTokenId = null;
                continue;
            }

            var quotaLimits = await _quotaService.GetQuotaLimitsAsync(entry.Saga.OrganizationId);
            var interval = entry.DriftCheckIntervalMinutes
                           ?? entry.NamespaceInterval
                           ?? quotaLimits?.DefaultDriftCheckIntervalMinutes
                           ?? 1440;
            var effectiveInterval = Math.Max(interval, quotaLimits?.MinDriftCheckIntervalMinutes ?? 720);

            var scheduled = await _scheduler.SchedulePublish(
                DateTime.UtcNow.AddMinutes(effectiveInterval),
                new DriftCheckScheduled
                {
                    ModuleId = entry.Saga.CorrelationId,
                    OrganizationId = entry.Saga.OrganizationId
                });

            entry.Saga.DriftCheckScheduleTokenId = scheduled.TokenId;
            rearmed++;

            _logger.LogDebug("Rescheduled drift check for module {ModuleId} in {Interval} minutes", entry.Saga.CorrelationId, effectiveInterval);
        }

        await dbContext.SaveChangesAsync();
        return rearmed;
    }

    // The approval deadline is WaitingSince + ApprovalTimeoutMinutes. One that lapsed while no
    // timer could fire gets a grace period instead of killing the job the moment the server returns.
    private async Task<int> RearmApprovalTimeouts(SnapCdDbContext dbContext)
    {
        var now = DateTime.UtcNow;

        var waiting = (await dbContext.ApplyJobSagas.AsNoTracking()
                .Where(s => s.CurrentState == "WaitingForApproval" && s.ApprovalTimeoutMinutes > 0 && s.WaitingSince != null)
                .Select(s => new { s.CorrelationId, s.OrganizationId, s.WaitingSince, s.ApprovalTimeoutMinutes })
                .ToListAsync())
            .Concat(await dbContext.DestroyJobSagas.AsNoTracking()
                .Where(s => s.CurrentState == "WaitingForApproval" && s.ApprovalTimeoutMinutes > 0 && s.WaitingSince != null)
                .Select(s => new { s.CorrelationId, s.OrganizationId, s.WaitingSince, s.ApprovalTimeoutMinutes })
                .ToListAsync())
            .ToList();

        foreach (var saga in waiting)
        {
            var deadline = saga.WaitingSince!.Value.AddMinutes(saga.ApprovalTimeoutMinutes!.Value);
            if (deadline <= now) deadline = now.Add(PastDeadlineGrace);

            await _scheduler.SchedulePublish(deadline, new ApprovalTimeoutReceived
            {
                CorrelationId = saga.CorrelationId,
                OrganizationId = saga.OrganizationId
            });

            _logger.LogDebug("Rescheduled approval timeout for job {JobId} at {Deadline:O}", saga.CorrelationId, deadline);
        }

        return waiting.Count;
    }

    // Saga rows exist only while a job is non-final, so every row gets a bare tick (no
    // scheduling token). Tick-bearing states restart their heartbeat cycle from it; waiting,
    // approval and cancelling states ignore it.
    private async Task<int> RearmHeartbeats(SnapCdDbContext dbContext)
    {
        var sagas = (await dbContext.ApplyJobSagas.AsNoTracking()
                .Select(s => new { s.CorrelationId, s.OrganizationId })
                .ToListAsync())
            .Concat(await dbContext.DestroyJobSagas.AsNoTracking()
                .Select(s => new { s.CorrelationId, s.OrganizationId })
                .ToListAsync())
            .ToList();

        foreach (var saga in sagas)
        {
            await _bus.Publish(new HeartbeatScheduled
            {
                CorrelationId = saga.CorrelationId,
                OrganizationId = saga.OrganizationId
            });

            _logger.LogDebug("Republished heartbeat tick for job {JobId}", saga.CorrelationId);
        }

        return sagas.Count;
    }
}
