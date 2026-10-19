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
    private readonly ProbeWaveProgressReaderFactory _progressFactory;

    public ProbeWaveSweepService(
        IDbContextFactory<SnapCdDbContext> dbContextFactory,
        ProbeWaveProgressReaderFactory progressFactory)
    {
        _dbContextFactory = dbContextFactory;
        _progressFactory = progressFactory;
    }

    /// <summary>
    /// Participants that should now apply: every contribution resolved, at least one confirmed, no
    /// signals of their own yet, no job already running, and a wave whose origin job is still
    /// running.
    ///
    /// A participant with no confirmed contribution has nothing to probe for, so it is left alone
    /// rather than applied: starting an apply nobody asked for would be a side effect, not a probe.
    /// </summary>
    public async Task<List<ReleasedParticipant>> ListReleased(int take = 50)
    {
        using var progress = _progressFactory.Create();
        var released = (await progress.ListUnsignalled())
            .Where(p => p.OwesAProbe)
            .ToList();

        if (released.Count == 0) return [];

        await using var dbContext = await _dbContextFactory.CreateDbContextAsync();

        // A wave whose origin job is no longer running has nobody waiting on the answer: it was
        // cancelled, refused, timed out or has already been approved and applied. Starting fresh
        // applies for it would be a side effect nobody asked for. Consumers already running are
        // left alone, since they are ordinary applies doing real work.
        var waveIds = released.Select(p => p.ProbeWaveId).Distinct().ToList();
        var liveWaveIds = await dbContext.ProbeWaves
            .AsNoTracking()
            .Where(w => waveIds.Contains(w.Id))
            .Join(dbContext.ModuleJobs.Where(j => j.Status == ExecutionStatus.Running),
                w => w.OriginModuleJobId,
                j => j.Id,
                (w, _) => w.Id)
            .Distinct()
            .ToListAsync();

        released = released.Where(p => liveWaveIds.Contains(p.ProbeWaveId)).ToList();
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
            .Select(p => new ReleasedParticipant(p.ParticipantId, p.ProbeWaveId, p.ModuleId, p.OrganizationId))
            .ToList();
    }
}
