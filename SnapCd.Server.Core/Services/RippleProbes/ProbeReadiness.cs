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
    NothingToProbe
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
        // A participant that has already produced its signal set has answered for its wave, so a
        // later apply of the same Module is not that wave's business.
        var participantIds = await _dbContext.ProbeWaveParticipants.AsNoTracking()
            .Where(p =>
                p.ModuleId == moduleId &&
                p.OrganizationId == organizationId &&
                p.SignalOutputSetId == null)
            .Select(p => p.Id)
            .ToListAsync();

        if (participantIds.Count == 0) return ProbeReadiness.NothingToWaitFor;

        var statuses = await _dbContext.SignalContributions.AsNoTracking()
            .Where(c =>
                participantIds.Contains(c.ProbeWaveParticipantId) &&
                c.OrganizationId == organizationId)
            .Select(c => c.Status)
            .ToListAsync();

        if (statuses.Count == 0) return ProbeReadiness.NothingToWaitFor;

        if (statuses.Any(s => s == SignalContributionStatus.Potential))
            return ProbeReadiness.WaitForSignals;

        return statuses.Any(s => s == SignalContributionStatus.Confirmed)
            ? ProbeReadiness.Probe
            : ProbeReadiness.NothingToProbe;
    }

    public void Dispose()
    {
        _dbContext.Dispose();
    }
}
