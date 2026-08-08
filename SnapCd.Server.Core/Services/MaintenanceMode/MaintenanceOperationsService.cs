// SPDX-License-Identifier: LicenseRef-Snap-CD-Source-Available-1.1
// Copyright (c) 2026 Karl Schriek / Schrieksoft.
// No license is granted to use this file, in whole or in part, (a) as training, fine-tuning, retrieval, or
// embedding data for any machine-learning model, or (b) as input to any machine-learning model, agent, or automated
// system for the purpose of producing a derivative work or reimplementation that is not otherwise permitted by the
// Snap CD Source-Available License (including any Competing Product as defined therein). Contact info@snapcd.io
// for terms covering either use.

using MassTransit;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using SnapCd.Contracts;
using SnapCd.Server.Core.Database;
using SnapCd.Server.Core.Events.Jobs.Module;
using SnapCd.Server.Core.Events.Runners;
using SnapCd.Server.Core.Enums;
using SnapCd.Server.Core.Events.System;
using SnapCd.Server.Core.Events.Missions;
using SnapCd.Server.Core.Misc.Helpers;

using JobMachine = SnapCd.Server.Core.StateMachine.Jobs.JobStateMachine<
    SnapCd.Server.Core.Entities.Sagas.ApplyJobSaga,
    SnapCd.Server.Core.Events.Jobs.Module.ApplyModuleRequested,
    SnapCd.Server.Core.Events.Jobs.Module.ApplyModuleFailed,
    SnapCd.Server.Core.Events.Jobs.Module.ApplyModuleCompleted,
    SnapCd.Server.Core.Events.Jobs.Module.ApplyModuleCancelled,
    SnapCd.Server.Core.Events.Steps.PlanRequested,
    SnapCd.Server.Core.Events.Steps.PlanCompleted,
    SnapCd.Server.Core.Events.Steps.PlanCancelled,
    SnapCd.Server.Core.Events.Steps.ApplyFromPlanRequested,
    SnapCd.Server.Core.Events.Steps.ApplyFromPlanCompleted,
    SnapCd.Server.Core.Events.Steps.ApplyFromPlanCancelled>;

namespace SnapCd.Server.Core.Services.MaintenanceMode;

public record ResumeSweepResult(int RunnersWoken, int ModulesRequeued, IReadOnlyList<string> Warnings);

public record CancelAllResult(int Cancelled, IReadOnlyList<string> Skipped);

public record CancelMissionsResult(int Requested, int MarkedCancelled, IReadOnlyList<string> Skipped);

/// <summary>
/// The operator actions of the maintenance panel: the resume sweep that un-parks jobs and
/// re-drives queued modules after a window closes, and the failsafe that cancels everything
/// non-terminal. Both are idempotent — republishing wake or cancel events is harmless.
/// </summary>
public class MaintenanceOperationsService
{
    // The reconnect event is a broadcast correlated by runner and instance: one saga in a state
    // the machine does not define faults the delivery for every saga of that runner. Groups
    // containing such sagas are skipped and reported instead of woken.
    private static readonly Lazy<HashSet<string>> ValidJobStates = new(() =>
        new JobMachine(NullLogger<JobMachine>.Instance).States.Select(s => s.Name).ToHashSet());

    private readonly IDbContextFactory<SnapCdDbContext> _dbContextFactory;
    private readonly IBus _bus;
    private readonly TransportReconciliationJob _reconciliationJob;
    private readonly IMaintenanceModeService _maintenanceMode;
    private readonly ILogger<MaintenanceOperationsService> _logger;

    public MaintenanceOperationsService(
        IDbContextFactory<SnapCdDbContext> dbContextFactory,
        IBus bus,
        TransportReconciliationJob reconciliationJob,
        IMaintenanceModeService maintenanceMode,
        ILogger<MaintenanceOperationsService> logger)
    {
        _dbContextFactory = dbContextFactory;
        _bus = bus;
        _reconciliationJob = reconciliationJob;
        _maintenanceMode = maintenanceMode;
        _logger = logger;
    }

    /// <summary>
    /// Performs the work an acting phase is defined by, and records the outcome on the window so
    /// the page reports what happened rather than what should happen. A no-op for waiting phases.
    /// </summary>
    public async Task<ResumeSweepResult?> RunPhaseActionAsync(MaintenancePhase phase)
    {
        switch (phase)
        {
            case MaintenancePhase.Reconciling:
            {
                await _reconciliationJob.ExecuteJob();
                await _maintenanceMode.RecordPhaseActionAsync("Transport timers re-derived from the database.");
                return null;
            }

            case MaintenancePhase.Resuming:
            {
                // The gate has to be down before the sweep publishes: the events it sends land in
                // activities that drop them while a window is open, so sweeping first recovers
                // nothing.
                await _maintenanceMode.DisableAsync();
                var result = await RunResumeSweepAsync();
                var summary = $"Woke {result.RunnersWoken} runner group(s), re-drove {result.ModulesRequeued} queued module(s)."
                              + (result.Warnings.Count > 0 ? $" {result.Warnings.Count} warning(s): {string.Join("; ", result.Warnings)}" : "");
                await _maintenanceMode.RecordPhaseActionAsync(summary);
                return result;
            }
        }

        return null;
    }

    /// <summary>
    /// Whether leaving from this phase would strand work. From Draining nothing is parked yet, and
    /// from Resuming the sweep has already run; in between, jobs are parked and modules queued with
    /// nothing scheduled to wake them, so the window has to be taken through Reconciling and
    /// Resuming rather than simply closed.
    /// </summary>
    public static bool ClosingNeedsRecovery(MaintenancePhase phase)
        => phase is MaintenancePhase.ReadyForMaintenance or MaintenancePhase.Reconciling;

    public async Task<ResumeSweepResult> RunResumeSweepAsync()
    {
        await using var db = await _dbContextFactory.CreateDbContextAsync();
        var warnings = new List<string>();

        var allSagas =
            (await db.ApplyJobSagas.AsNoTracking()
                .Select(s => new { s.CorrelationId, s.OrganizationId, s.RunnerId, s.RunnerInstanceName, s.CurrentState })
                .ToListAsync())
            .Concat(await db.DestroyJobSagas.AsNoTracking()
                .Select(s => new { s.CorrelationId, s.OrganizationId, s.RunnerId, s.RunnerInstanceName, s.CurrentState })
                .ToListAsync())
            .ToList();

        var runnersWoken = 0;
        var parkedGroups = allSagas
            .Where(s => s.CurrentState.EndsWith("WaitingForRunner"))
            .GroupBy(s => (s.OrganizationId, s.RunnerId, s.RunnerInstanceName));

        foreach (var group in parkedGroups)
        {
            var groupSagas = allSagas.Where(s =>
                s.OrganizationId == group.Key.OrganizationId
                && s.RunnerId == group.Key.RunnerId
                && s.RunnerInstanceName == group.Key.RunnerInstanceName).ToList();

            var corrupt = groupSagas.Where(s => !ValidJobStates.Value.Contains(s.CurrentState)).ToList();
            if (corrupt.Count > 0)
            {
                warnings.Add(
                    $"Runner {group.Key.RunnerId}/{group.Key.RunnerInstanceName}: not woken — saga(s) {string.Join(", ", corrupt.Select(c => c.CorrelationId))} in unknown state(s) {string.Join(", ", corrupt.Select(c => c.CurrentState).Distinct())}; resolve them first");
                continue;
            }

            var connection = await db.RunnerConnections.AsNoTracking()
                .FirstOrDefaultAsync(rc => rc.RunnerId == group.Key.RunnerId
                                           && rc.OrganizationId == group.Key.OrganizationId
                                           && rc.InstanceName == group.Key.RunnerInstanceName);
            if (connection == null)
            {
                warnings.Add(
                    $"Runner {group.Key.RunnerId}/{group.Key.RunnerInstanceName}: not connected; its {group.Count()} parked job(s) resume on its next check-in");
                continue;
            }

            await _bus.Publish(new RunnerReconnectedEvent
            {
                OrganizationId = group.Key.OrganizationId,
                RunnerId = group.Key.RunnerId,
                InstanceName = group.Key.RunnerInstanceName!,
                ServerInstanceId = connection.ServerInstanceId
            });
            runnersWoken++;
        }

        var queued = await db.ModuleSagas.AsNoTracking()
            .Where(s => s.QueuedDesiredStateHeadline != null)
            .Select(s => new { s.CorrelationId, s.OrganizationId })
            .ToListAsync();
        foreach (var module in queued)
            await _bus.Publish(new ModuleDependencyCheckRequested
            {
                ModuleId = module.CorrelationId,
                OrganizationId = module.OrganizationId
            });

        _logger.LogInformation(
            "Resume sweep: {Runners} runner group(s) woken, {Modules} queued module(s) re-driven, {Warnings} warning(s)",
            runnersWoken, queued.Count, warnings.Count);
        return new ResumeSweepResult(runnersWoken, queued.Count, warnings);
    }

    /// <summary>
    /// Cancels the mission runs that hold a drain open. A run with a live connection is asked to
    /// stop and reports its own outcome; one whose agent is gone has nothing to ask, so it is
    /// marked cancelled here — otherwise it holds the phase open until its deadline expires.
    /// </summary>
    public async Task<CancelMissionsResult> CancelHoldingMissionsAsync()
    {
        await using var db = await _dbContextFactory.CreateDbContextAsync();
        var skipped = new List<string>();
        var requested = 0;
        var marked = 0;

        var runs = await db.ModuleJobMissionRuns
            .Where(r => r.Status == MissionStatus.Running || r.Status == MissionStatus.AwaitingReconnect)
            .ToListAsync();

        // A run keeps the connection id it was dispatched on, so the id outlives the agent. Only a
        // row in AgentConnections proves the agent is still there to be asked.
        var liveConnections = await db.AgentConnections.AsNoTracking()
            .Select(c => c.SignalRConnectionId)
            .ToListAsync();
        var live = liveConnections.ToHashSet();

        foreach (var run in runs)
        {
            if (run.ServerInstanceId is null
                || run.SignalRConnectionId is null
                || !live.Contains(run.SignalRConnectionId))
            {
                run.Status = MissionStatus.Cancelled;
                run.CancelRequestedAt = DateTime.UtcNow;
                marked++;
                continue;
            }

            run.CancelRequestedAt = DateTime.UtcNow;

            try
            {
                var uri = MassTransitHelpers.GetAgentConsumerEndpoint(
                    run.ServerInstanceId.Value, nameof(CancelMissionRunRequested));
                var endpoint = await _bus.GetSendEndpoint(new Uri(uri));
                await endpoint.Send(new CancelMissionRunRequested
                {
                    RunId = run.Id,
                    OrganizationId = run.OrganizationId,
                    InvocationId = run.InvocationId,
                    AgentConnectionId = run.SignalRConnectionId
                });
                requested++;
            }
            catch (Exception ex)
            {
                skipped.Add($"{run.MissionType} run {run.Id}: {ex.Message}");
            }
        }

        // A run that timed out parks its parent mission at WaitingForAgent, where it waits for an
        // agent that may never return. Those hold no phase open, but they are outstanding work with
        // nothing else to resolve them.
        var stranded = await db.ModuleJobMissions
            .Where(m => m.Status == MissionStatus.WaitingForAgent
                        || m.Status == MissionStatus.BlockedAgentNotAssigned)
            .ToListAsync();

        foreach (var mission in stranded)
        {
            mission.Status = MissionStatus.Cancelled;
            marked++;
        }

        await db.SaveChangesAsync();

        _logger.LogWarning(
            "Maintenance: cancellation requested for {Requested} mission run(s), {Marked} marked cancelled with no live agent, {Skipped} skipped",
            requested, marked, skipped.Count);

        return new CancelMissionsResult(requested, marked, skipped);
    }

    public async Task<CancelAllResult> CancelAllJobsAsync(CancellationType cancellationType)
    {
        await using var db = await _dbContextFactory.CreateDbContextAsync();
        var skipped = new List<string>();

        var sagas =
            (await db.ApplyJobSagas.AsNoTracking().Select(s => new { s.CorrelationId, s.OrganizationId, s.CurrentState }).ToListAsync())
            .Concat(await db.DestroyJobSagas.AsNoTracking().Select(s => new { s.CorrelationId, s.OrganizationId, s.CurrentState }).ToListAsync())
            .ToList();

        var cancelled = 0;
        foreach (var saga in sagas)
        {
            if (saga.CurrentState.StartsWith("Cancelling"))
            {
                skipped.Add($"{saga.CorrelationId}: already cancelling");
                continue;
            }

            // WaitingForApproval has no cancel handler; declining the approval is the cancel there.
            if (saga.CurrentState == "WaitingForApproval")
            {
                skipped.Add($"{saga.CorrelationId}: awaiting approval — decline the approval instead");
                continue;
            }

            if (!saga.CurrentState.EndsWith("Pending") && !saga.CurrentState.EndsWith("WaitingForRunner"))
            {
                skipped.Add($"{saga.CorrelationId}: state {saga.CurrentState} is not cancellable");
                continue;
            }

            await _bus.Publish(new CancelModuleRequested
            {
                CorrelationId = saga.CorrelationId,
                OrganizationId = saga.OrganizationId,
                CancellationType = cancellationType
            });
            cancelled++;
        }

        _logger.LogWarning("Cancel-all issued ({Type}): {Cancelled} job(s) cancelled, {Skipped} skipped", cancellationType, cancelled, skipped.Count);
        return new CancelAllResult(cancelled, skipped);
    }
}
