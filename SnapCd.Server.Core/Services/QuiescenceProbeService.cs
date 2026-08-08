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
using SnapCd.Server.Core.Enums;

namespace SnapCd.Server.Core.Services;

/// <summary>
/// Unfinished mission runs grouped by type and status. <see cref="HoldsDrainOpen"/> marks the
/// statuses that occupy an agent; the rest are parked and survive a window untouched.
/// </summary>
public record MissionBreakdown(string MissionType, string Status, int Count, bool HoldsDrainOpen);

public record DrainStatus(
    int PendingJobs,
    int ParkedJobs,
    int AwaitingApproval,
    int Cancelling,
    int QueuedForMaintenance,
    int QueuedOther,
    int ActiveMissions,
    IReadOnlyList<StuckJob> StuckJobs,
    IReadOnlyList<MissionBreakdown> Missions)
{
    /// <summary>
    /// The database half of the Parked-to-Silent gate: parked, approval-waiting and queued jobs
    /// are safe rest states, but nothing may be mid-step or mid-cancel.
    /// </summary>
    public bool IsDatabaseQuiet => PendingJobs == 0 && Cancelling == 0;
}

/// <summary>
/// The drain board's data source: where every job rests, from the database's point of view.
/// The transport half of the probe (queue depths) is a separate concern — a job saga row is
/// authoritative for what the job is doing; the transport only carries how it moves.
/// </summary>
public class QuiescenceProbeService
{
    private readonly IDbContextFactory<SnapCdDbContext> _dbContextFactory;
    private readonly StuckJobDetectionService _stuckJobDetection;

    public QuiescenceProbeService(IDbContextFactory<SnapCdDbContext> dbContextFactory, StuckJobDetectionService stuckJobDetection)
    {
        _dbContextFactory = dbContextFactory;
        _stuckJobDetection = stuckJobDetection;
    }

    public async Task<DrainStatus> GetDrainStatusAsync()
    {
        await using var db = await _dbContextFactory.CreateDbContextAsync();

        var jobStates =
            (await db.ApplyJobSagas.AsNoTracking().Select(s => s.CurrentState).ToListAsync())
            .Concat(await db.DestroyJobSagas.AsNoTracking().Select(s => s.CurrentState).ToListAsync())
            .ToList();

        var pending = jobStates.Count(s => s.EndsWith("Pending"));
        var parked = jobStates.Count(s => s.EndsWith("WaitingForRunner"));
        var approval = jobStates.Count(s => s == "WaitingForApproval");
        var cancelling = jobStates.Count(s => s.StartsWith("Cancelling"));

        var queuedReasons = await db.ModuleSagas.AsNoTracking()
            .Where(s => s.QueuedDesiredStateHeadline != null)
            .Select(s => s.QueuedReason)
            .ToListAsync();
        var queuedMaintenance = queuedReasons.Count(r => r == QueuedReason.Maintenance);
        var queuedOther = queuedReasons.Count - queuedMaintenance;

        var activeMissions = await db.ModuleJobMissionRuns.AsNoTracking()
            .CountAsync(r => r.Status != MissionStatus.Succeeded
                             && r.Status != MissionStatus.Failed
                             && r.Status != MissionStatus.Cancelled
                             && r.Status != MissionStatus.TimedOut);

        var missionBreakdown = (await db.ModuleJobMissionRuns.AsNoTracking()
                .Where(r => r.Status != MissionStatus.Succeeded
                            && r.Status != MissionStatus.Failed
                            && r.Status != MissionStatus.Cancelled
                            && r.Status != MissionStatus.TimedOut)
                .GroupBy(r => new { r.Status, r.MissionType })
                .Select(g => new { g.Key.Status, g.Key.MissionType, Count = g.Count() })
                .ToListAsync())
            .Select(x => new MissionBreakdown(x.MissionType.ToString(), x.Status.ToString(), x.Count,
                x.Status is MissionStatus.Running or MissionStatus.AwaitingReconnect))
            .OrderByDescending(x => x.HoldsDrainOpen)
            .ThenBy(x => x.MissionType)
            .ToList();

        var stuck = await _stuckJobDetection.FindStuckJobsAsync();

        return new DrainStatus(pending, parked, approval, cancelling, queuedMaintenance, queuedOther, activeMissions, stuck, missionBreakdown);
    }
}
