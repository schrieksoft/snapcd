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

namespace SnapCd.Server.Core.Services.Nonsecured.RippleProbes;

/// <summary>What an apply should do when it reaches the probe decision.</summary>
public enum ProbeReadiness
{
    /// <summary>No wave wants anything from this Module. Read the outputs and finish.</summary>
    NothingToWaitFor,

    /// <summary>An upstream has not answered yet, so no probe runs on this apply.</summary>
    WaitForSignals,

    /// <summary>Every upstream has answered and at least one carries signals this Module reads.</summary>
    Probe,

    /// <summary>Every upstream has answered and none carries anything this Module reads.</summary>
    NothingToProbe,

    /// <summary>This Module's engine cannot plan with unknown values, so no probe can answer.</summary>
    EngineCannotProbe
}

public class ProbeReadinessReaderFactory(IDbContextFactory<SnapCdDbContext> dbFactory)
{
    public ProbeReadinessReader Create() => new(dbFactory.CreateDbContext());
}

/// <summary>
/// Reads whether a Module's participation in a wave is settled yet.
///
/// Read when the apply reaches the probe decision rather than when the job was created, so a
/// contribution that resolved while this apply was running is taken into account.
/// </summary>
public class ProbeReadinessReader : IDisposable
{
    private readonly SnapCdDbContext _dbContext;

    public ProbeReadinessReader(SnapCdDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<ProbeReadiness> Read(Guid moduleId, Guid organizationId)
    {
        // A Module's latest attempt in a wave is what says whether it still owes that wave an
        // answer: an earlier attempt's result is a record of what was predicted, and asking again
        // adds an attempt with no result rather than clearing the one that has it.
        var reported = (await _dbContext.ModuleProbeAttempts
                .AsNoTracking()
                .Where(a => a.ModuleId == moduleId && a.OrganizationId == organizationId)
                .Select(a => new { a.ProbeWaveId, a.Timestamp, a.SignalOutputSetId })
                .ToListAsync())
            .GroupBy(a => a.ProbeWaveId)
            .Where(g => g.OrderByDescending(a => a.Timestamp).First().SignalOutputSetId != null)
            .Select(g => g.Key)
            .ToList();

        var statuses = await _dbContext.SignalEdges
            .AsNoTracking()
            .Where(e =>
                e.ConsumerModuleId == moduleId &&
                e.OrganizationId == organizationId &&
                !reported.Contains(e.ProbeWaveId))
            .Select(e => e.Status)
            .ToListAsync();

        if (statuses.Count == 0) return ProbeReadiness.NothingToWaitFor;

        if (statuses.Any(s => s == SignalEdgeStatus.Potential))
            return ProbeReadiness.WaitForSignals;

        if (!statuses.Any(s => s == SignalEdgeStatus.Confirmed))
            return ProbeReadiness.NothingToProbe;

        return await EngineSupportsProbes(moduleId, organizationId)
            ? ProbeReadiness.Probe
            : ProbeReadiness.EngineCannotProbe;
    }

    /// <summary>
    /// A probe plans with values that are not yet known, which needs a planner that carries an
    /// unknown through. The Module's own engine, else its Namespace's default, else OpenTofu.
    /// </summary>
    private async Task<bool> EngineSupportsProbes(Guid moduleId, Guid organizationId)
    {
        var engines = await _dbContext.Modules
            .AsNoTracking()
            .Where(m => m.Id == moduleId && m.OrganizationId == organizationId)
            .Select(m => new { m.Engine, NamespaceDefault = m.Namespace.DefaultEngine })
            .FirstOrDefaultAsync();

        var engine = engines?.Engine ?? engines?.NamespaceDefault ?? StateManagementEngine.OpenTofu;

        return engine.SupportsProbes();
    }

    public void Dispose()
    {
        _dbContext.Dispose();
    }
}
