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

/// <summary>What an upstream's apply should do when it reaches the point of waiting for its wave.</summary>
public enum ProbeWaveSettlement
{
    /// <summary>This job started no wave. Carry on to approval.</summary>
    NoWave,

    /// <summary>At least one Module still owes an answer. Park.</summary>
    Waiting,

    /// <summary>Everything that was going to answer has. Carry on to approval.</summary>
    Settled
}

/// <summary>
/// How far one Module has got in one wave: the edges into it, and whether it has reported.
/// </summary>
public record ModuleProgress(
    Guid ProbeWaveId,
    Guid ModuleId,
    Guid OrganizationId,
    bool HasSignalled,
    List<SignalEdgeStatus> EdgeStatuses)
{
    /// <summary>Every edge into it has resolved, so nothing upstream is still owed.</summary>
    public bool IsResolved =>
        EdgeStatuses.Count > 0 &&
        EdgeStatuses.All(s => s != SignalEdgeStatus.Potential);

    /// <summary>
    /// Resolved with something to probe for. A Module resolved with nothing confirmed has no
    /// probe to run, so it owes nobody an answer.
    /// </summary>
    public bool OwesAProbe =>
        IsResolved &&
        !HasSignalled &&
        EdgeStatuses.Any(s => s == SignalEdgeStatus.Confirmed);

    /// <summary>
    /// This Module is why the wave is not finished: either an upstream still owes it an answer,
    /// or it owes one itself.
    /// </summary>
    public bool IsOutstanding => !HasSignalled && (!IsResolved || OwesAProbe);
}

public class ProbeWaveProgressReaderFactory(IDbContextFactory<SnapCdDbContext> dbFactory)
{
    public ProbeWaveProgressReader Create() => new(dbFactory.CreateDbContext());
}

/// <summary>
/// Where a wave has got to, per Module.
///
/// The readiness check and the upstream's wait ask two different questions of the same rows, and
/// an answer they disagreed on would mean a wave that releases nobody while its origin thinks it
/// is done, or an origin waiting on a Module that will never be started. Both read from here.
/// </summary>
public class ProbeWaveProgressReader : IDisposable
{
    private readonly SnapCdDbContext _dbContext;

    public ProbeWaveProgressReader(SnapCdDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    /// <summary>Every Module an edge points at in this wave, with how far it has got.</summary>
    public async Task<List<ModuleProgress>> ListByWave(Guid probeWaveId, Guid organizationId)
    {
        var edges = await _dbContext.SignalEdges
            .AsNoTracking()
            .Where(e => e.ProbeWaveId == probeWaveId && e.OrganizationId == organizationId)
            .Select(e => new { e.ConsumerModuleId, e.Status })
            .ToListAsync();

        if (edges.Count == 0) return [];

        var signalled = await SignalledModuleIds(probeWaveId, organizationId);

        return edges
            .GroupBy(e => e.ConsumerModuleId)
            .Select(g => new ModuleProgress(
                probeWaveId,
                g.Key,
                organizationId,
                signalled.Contains(g.Key),
                g.Select(e => e.Status).ToList()))
            .ToList();
    }

    /// <summary>
    /// One Module's progress in one wave, or null when no edge points at it: a Module nothing
    /// reaches is not part of the wave at all.
    /// </summary>
    public async Task<ModuleProgress?> GetModule(Guid probeWaveId, Guid moduleId, Guid organizationId)
    {
        var statuses = await _dbContext.SignalEdges
            .AsNoTracking()
            .Where(e => e.ProbeWaveId == probeWaveId &&
                        e.ConsumerModuleId == moduleId &&
                        e.OrganizationId == organizationId)
            .Select(e => e.Status)
            .ToListAsync();

        if (statuses.Count == 0) return null;

        var signalled = await _dbContext.ModuleProbeAttempts
            .AsNoTracking()
            .AnyAsync(a => a.ProbeWaveId == probeWaveId &&
                           a.ModuleId == moduleId &&
                           a.OrganizationId == organizationId &&
                           a.SignalOutputSetId != null);

        return new ModuleProgress(probeWaveId, moduleId, organizationId, signalled, statuses);
    }

    public async Task<ProbeWaveSettlement> ReadForOriginJob(Guid originModuleJobId, Guid organizationId)
    {
        var waveIds = await _dbContext.ProbeWaves.AsNoTracking()
            .Where(w => w.OriginModuleJobId == originModuleJobId && w.OrganizationId == organizationId)
            .Select(w => w.Id)
            .ToListAsync();

        if (waveIds.Count == 0) return ProbeWaveSettlement.NoWave;

        foreach (var waveId in waveIds)
        {
            var modules = await ListByWave(waveId, organizationId);

            // The origin answers from its own plan, so no edge points at it and it is never
            // outstanding on its own account.
            if (modules.Any(m => m.IsOutstanding)) return ProbeWaveSettlement.Waiting;
        }

        return ProbeWaveSettlement.Settled;
    }

    private async Task<HashSet<Guid>> SignalledModuleIds(Guid probeWaveId, Guid organizationId)
    {
        var ids = await _dbContext.ModuleProbeAttempts
            .AsNoTracking()
            .Where(a => a.ProbeWaveId == probeWaveId &&
                        a.OrganizationId == organizationId &&
                        a.SignalOutputSetId != null)
            .Select(a => a.ModuleId)
            .ToListAsync();

        return ids.ToHashSet();
    }

    public void Dispose()
    {
        _dbContext.Dispose();
    }
}
