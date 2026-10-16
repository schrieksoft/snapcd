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

namespace SnapCd.Server.Core.Services.RippleProbes;

/// <summary>A participant whose upstreams have all answered and which has not yet probed.</summary>
public record ReleasedParticipant(Guid ParticipantId, Guid ProbeWaveId, Guid ModuleId, Guid OrganizationId);

/// <summary>
/// Finds participants a wave is ready for.
///
/// A sweep rather than the upstream starting its consumers directly, because the releasing event is
/// a contribution resolving, which happens throughout a wave's life and not only at its start. A
/// participant released later, by a second upstream resolving, would have nobody to start it.
/// </summary>
public class ProbeWaveSweepService
{
    private readonly IDbContextFactory<SnapCdDbContext> _dbContextFactory;

    public ProbeWaveSweepService(IDbContextFactory<SnapCdDbContext> dbContextFactory)
    {
        _dbContextFactory = dbContextFactory;
    }

    /// <summary>
    /// Participants that should now apply: every contribution resolved, at least one confirmed, no
    /// signals of their own yet, and no job already running.
    ///
    /// A participant with no confirmed contribution has nothing to probe for, so it is left alone
    /// rather than applied: starting an apply nobody asked for would be a side effect, not a probe.
    /// </summary>
    public async Task<List<ReleasedParticipant>> ListReleased(int take = 50)
    {
        await using var dbContext = await _dbContextFactory.CreateDbContextAsync();

        var candidates = await dbContext.ProbeWaveParticipants
            .AsNoTracking()
            .Where(p => p.SignalOutputSetId == null)
            .Select(p => new
            {
                p.Id,
                p.ProbeWaveId,
                p.ModuleId,
                p.OrganizationId,
                Statuses = p.Contributions.Select(c => c.Status).ToList()
            })
            .ToListAsync();

        var released = candidates
            .Where(p =>
                p.Statuses.Count > 0 &&
                p.Statuses.All(st => st != SignalContributionStatus.Potential) &&
                p.Statuses.Any(st => st == SignalContributionStatus.Confirmed))
            .ToList();

        if (released.Count == 0) return [];

        // A Module already running a job is left alone: its probe step reads the contributions
        // itself, so starting a second apply would race the first.
        var moduleIds = released.Select(p => p.ModuleId).Distinct().ToList();
        var busy = await dbContext.ModuleJobs
            .AsNoTracking()
            .Where(j => moduleIds.Contains(j.ModuleId) && j.Status == ExecutionStatus.Running)
            .Select(j => j.ModuleId)
            .Distinct()
            .ToListAsync();

        return released
            .Where(p => !busy.Contains(p.ModuleId))
            .Take(take)
            .Select(p => new ReleasedParticipant(p.Id, p.ProbeWaveId, p.ModuleId, p.OrganizationId))
            .ToList();
    }
}
