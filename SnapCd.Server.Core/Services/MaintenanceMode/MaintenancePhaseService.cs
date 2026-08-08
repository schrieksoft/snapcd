// SPDX-License-Identifier: LicenseRef-Snap-CD-Source-Available-1.1
// Copyright (c) 2026 Karl Schriek / Schrieksoft.
// No license is granted to use this file, in whole or in part, (a) as training, fine-tuning, retrieval, or
// embedding data for any machine-learning model, or (b) as input to any machine-learning model, agent, or automated
// system for the purpose of producing a derivative work or reimplementation that is not otherwise permitted by the
// Snap CD Source-Available License (including any Competing Product as defined therein). Contact info@snapcd.io
// for terms covering either use.

using Microsoft.EntityFrameworkCore;
using SnapCd.Server.Core.Database;
using SnapCd.Server.Core.Enums;
using SnapCd.Contracts;

namespace SnapCd.Server.Core.Services.MaintenanceMode;

/// <summary>One thing that must be true before the current phase can be left.</summary>
public record PhaseCriterion(string Name, string Detail, bool IsMet, int? Count = null, IReadOnlyList<PhaseBlocker>? Blockers = null);

/// <summary>A specific item holding a criterion back, so "why can I not proceed" is answerable here.</summary>
public record PhaseBlocker(string Id, string Title, string State, string Detail, string? Link = null);

public record PhaseReadiness(MaintenancePhase Phase, IReadOnlyList<PhaseCriterion> Criteria)
{
    public bool CanAdvance => Criteria.All(c => c.IsMet);
    public MaintenancePhase? NextPhase => Phase switch
    {
        MaintenancePhase.Draining => MaintenancePhase.ReadyForMaintenance,
        MaintenancePhase.ReadyForMaintenance => MaintenancePhase.Reconciling,
        MaintenancePhase.Reconciling => MaintenancePhase.Resuming,
        _ => null
    };
}

/// <summary>
/// Evaluates whether the window may leave its current phase, and names what is holding it back.
/// The waiting phases (Draining, Parked, Silent) converge on their own; the acting phases
/// (Reconciling, Resuming) are driven by the operator or the automatic advance.
/// </summary>
public class MaintenancePhaseService
{
    private readonly IDbContextFactory<SnapCdDbContext> _dbContextFactory;
    private readonly TransportProbeService _transportProbe;

    public MaintenancePhaseService(IDbContextFactory<SnapCdDbContext> dbContextFactory, TransportProbeService transportProbe)
    {
        _dbContextFactory = dbContextFactory;
        _transportProbe = transportProbe;
    }

    public async Task<PhaseReadiness> EvaluateAsync(MaintenancePhase phase, Entities.Definition.MaintenanceMode? window = null)
        => new(phase, phase switch
        {
            MaintenancePhase.Draining => await DrainingCriteriaAsync(),
            // Silent is where the disruptive work happens: only the operator knows it is done.
            MaintenancePhase.ReadyForMaintenance =>
            [
                new PhaseCriterion("Disruptive work complete",
                    "Nothing is running and the transport is idle. Advance when the work this window was opened for is finished.",
                    false)
            ],
            MaintenancePhase.Reconciling => [ActionCriterion("Timers re-derived", window)],
            MaintenancePhase.Resuming => [ActionCriterion("Jobs resumed", window)],
            _ => []
        });

    // An acting phase is complete when its action has run; the summary it recorded is its status.
    private static PhaseCriterion ActionCriterion(string name, Entities.Definition.MaintenanceMode? window)
        => window?.PhaseActionCompletedAt is { } completedAt
            ? new PhaseCriterion(name, $"{window.PhaseActionSummary} ({completedAt:u})", true)
            : new PhaseCriterion(name, "Running...", false);

    private async Task<IReadOnlyList<PhaseCriterion>> DrainingCriteriaAsync()
    {
        await using var db = await _dbContextFactory.CreateDbContextAsync();
        var inFlight = await JobsInStatesAsync(db, s => s.EndsWith("Pending"));
        var cancelling = await JobsInStatesAsync(db, s => s.StartsWith("Cancelling"));
        var missions = await RunningMissionsAsync(db);

        return
        [
            new PhaseCriterion(
                "No job mid-step", "Every running task has finished and its job has parked.",
                inFlight.Count == 0, inFlight.Count, inFlight),
            new PhaseCriterion(
                "No cancellation outstanding", "Cancellations resolve in seconds; one still open is stranded.",
                cancelling.Count == 0, cancelling.Count, cancelling),
            new PhaseCriterion(
                "No mission mid-run",
                "A mission runs on an agent and writes its result back; interrupting it loses the run.",
                missions.Count == 0, missions.Count, missions),
            await TransportIdleCriterionAsync()
        ];
    }

    private async Task<PhaseCriterion> TransportIdleCriterionAsync()
    {
        var depths = await _transportProbe.GetDepthsAsync();
        if (depths.ProbeError != null)
            return new PhaseCriterion("Transport idle", $"Transport probe failed: {depths.ProbeError}", false);

        var busy = depths.Queues
            .Where(q => !TransportDepths.IsDiagnosticQueue(q.Queue) && q.Active > 0)
            .Select(q => new PhaseBlocker(q.Queue, q.Queue, "active", $"{q.Active} message(s) waiting"))
            .ToList();

        return new PhaseCriterion(
            "Transport idle",
            "No active messages outside diagnostic queues. Scheduled messages are re-derived, never drained.",
            busy.Count == 0, busy.Count, busy);
    }

    /// <summary>
    /// Missions occupying an agent. Statuses that park (waiting for an agent, blocked on
    /// assignment) are excluded: like a parked job they survive the window untouched.
    /// </summary>
    private static async Task<List<PhaseBlocker>> RunningMissionsAsync(SnapCdDbContext db)
    {
        var runs = await db.ModuleJobMissionRuns.AsNoTracking()
            .Where(r => r.Status == MissionStatus.Running || r.Status == MissionStatus.AwaitingReconnect)
            .Select(r => new { r.Id, r.MissionType, r.Status, r.ModuleJobId, r.AttemptNumber, r.DeadlineAt })
            .ToListAsync();

        return runs
            .Select(r => new PhaseBlocker(
                r.Id.ToString(),
                $"{r.MissionType} (job {r.ModuleJobId.ToString()[..8]})",
                r.Status.ToString(),
                $"attempt {r.AttemptNumber}, deadline {r.DeadlineAt:u}"))
            .ToList();
    }

    private static async Task<List<PhaseBlocker>> JobsInStatesAsync(SnapCdDbContext db, Func<string, bool> statePredicate)
    {
        var apply = await db.ApplyJobSagas.AsNoTracking()
            .Select(s => new { s.CorrelationId, s.CurrentState, s.ModuleId, s.RunnerInstanceName, s.WaitingSince })
            .ToListAsync();
        var destroy = await db.DestroyJobSagas.AsNoTracking()
            .Select(s => new { s.CorrelationId, s.CurrentState, s.ModuleId, s.RunnerInstanceName, s.WaitingSince })
            .ToListAsync();

        var moduleNames = await db.Modules.AsNoTracking()
            .Select(m => new { m.Id, m.Name })
            .ToDictionaryAsync(m => m.Id, m => m.Name);

        return apply.Select(s => (Saga: s, Type: "Apply"))
            .Concat(destroy.Select(s => (Saga: s, Type: "Destroy")))
            .Where(x => statePredicate(x.Saga.CurrentState))
            .Select(x => new PhaseBlocker(
                x.Saga.CorrelationId.ToString(),
                moduleNames.TryGetValue(x.Saga.ModuleId, out var name) ? $"{name} ({x.Type})" : $"{x.Type} job",
                x.Saga.CurrentState,
                x.Saga.RunnerInstanceName is { Length: > 0 } runner ? $"runner {runner}" : "no runner assigned",
                $"/Job/{x.Saga.CorrelationId}"))
            .ToList();
    }
}
