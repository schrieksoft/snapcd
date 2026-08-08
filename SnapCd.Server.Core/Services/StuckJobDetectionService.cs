// SPDX-License-Identifier: LicenseRef-Snap-CD-Source-Available-1.1
// Copyright (c) 2026 Karl Schriek / Schrieksoft.
// No license is granted to use this file, in whole or in part, (a) as training, fine-tuning, retrieval, or
// embedding data for any machine-learning model, or (b) as input to any machine-learning model, agent, or automated
// system for the purpose of producing a derivative work or reimplementation that is not otherwise permitted by the
// Snap CD Source-Available License (including any Competing Product as defined therein). Contact info@snapcd.io
// for terms covering either use.

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using SnapCd.Server.Core.Database;
using SnapCd.Server.Core.Settings;

namespace SnapCd.Server.Core.Services;

public record StuckJob(Guid JobId, string JobType, Guid OrganizationId, Guid ModuleId, string State, DateTime WaitingSince, TimeSpan Stalled);

/// <summary>
/// Finds job sagas resting in a waiting state for longer than plausible. The heartbeat cycle
/// polices the *Pending states; this covers the states it cannot: parked *WaitingForRunner
/// sagas, approval waits without a configured timeout, and stranded cancellations. All three
/// state families record WaitingSince on entry.
/// </summary>
public class StuckJobDetectionService
{
    private readonly IDbContextFactory<SnapCdDbContext> _dbContextFactory;
    private readonly StuckJobDetectionSettings _settings;

    public StuckJobDetectionService(IDbContextFactory<SnapCdDbContext> dbContextFactory, IOptions<StuckJobDetectionSettings> settings)
    {
        _dbContextFactory = dbContextFactory;
        _settings = settings.Value;
    }

    public async Task<List<StuckJob>> FindStuckJobsAsync()
    {
        var now = DateTime.UtcNow;
        await using var db = await _dbContextFactory.CreateDbContextAsync();

        var candidates =
            (await db.ApplyJobSagas.AsNoTracking()
                .Where(s => s.WaitingSince != null)
                .Select(s => new { s.CorrelationId, s.OrganizationId, s.ModuleId, s.CurrentState, s.WaitingSince })
                .ToListAsync())
            .Select(s => (s.CorrelationId, s.OrganizationId, s.ModuleId, s.CurrentState, s.WaitingSince, JobType: "Apply"))
            .Concat((await db.DestroyJobSagas.AsNoTracking()
                    .Where(s => s.WaitingSince != null)
                    .Select(s => new { s.CorrelationId, s.OrganizationId, s.ModuleId, s.CurrentState, s.WaitingSince })
                    .ToListAsync())
                .Select(s => (s.CorrelationId, s.OrganizationId, s.ModuleId, s.CurrentState, s.WaitingSince, JobType: "Destroy")));

        var stuck = new List<StuckJob>();
        foreach (var saga in candidates)
        {
            var threshold = ThresholdFor(saga.CurrentState);
            if (threshold == null) continue;

            var stalled = now - saga.WaitingSince!.Value;
            if (stalled > threshold.Value)
                stuck.Add(new StuckJob(saga.CorrelationId, saga.JobType, saga.OrganizationId, saga.ModuleId, saga.CurrentState, saga.WaitingSince.Value, stalled));
        }

        return stuck;
    }

    private TimeSpan? ThresholdFor(string state)
    {
        if (state.EndsWith("WaitingForRunner")) return TimeSpan.FromMinutes(_settings.RunnerWaitThresholdMinutes);
        if (state == "WaitingForApproval") return TimeSpan.FromMinutes(_settings.ApprovalWaitThresholdMinutes);
        if (state.StartsWith("Cancelling")) return TimeSpan.FromMinutes(_settings.CancellingThresholdMinutes);
        return null;
    }
}
