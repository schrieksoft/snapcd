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
/// One participant's statuses, which both the sweep and the settlement question are answered from.
/// </summary>
public record ParticipantProgress(
    Guid ParticipantId,
    Guid ProbeWaveId,
    Guid ModuleId,
    Guid OrganizationId,
    bool HasSignalled,
    List<SignalContributionStatus> ContributionStatuses)
{
    /// <summary>
    /// Nothing waits on this participant and nothing will probe it. Participants are over-included
    /// from reachability, so most of a wide graph is in this state and must not hold a wave open.
    /// </summary>
    public bool IsBystander => ContributionStatuses.Count == 0;

    /// <summary>Every contribution has resolved, so nothing upstream is still owed.</summary>
    public bool IsResolved =>
        ContributionStatuses.Count > 0 &&
        ContributionStatuses.All(s => s != SignalContributionStatus.Potential);

    /// <summary>
    /// Resolved with something to probe for. A participant resolved with nothing confirmed has no
    /// probe to run, so it owes nobody an answer.
    /// </summary>
    public bool OwesAProbe =>
        IsResolved &&
        !HasSignalled &&
        ContributionStatuses.Any(s => s == SignalContributionStatus.Confirmed);

    /// <summary>
    /// This participant is why the wave is not finished: either an upstream still owes it an
    /// answer, or it owes one itself.
    /// </summary>
    public bool IsOutstanding =>
        !IsBystander &&
        !HasSignalled &&
        (!IsResolved || OwesAProbe);
}

public class ProbeWaveProgressReaderFactory(IDbContextFactory<SnapCdDbContext> dbFactory)
{
    public ProbeWaveProgressReader Create() => new(dbFactory.CreateDbContext());
}

/// <summary>
/// Where a wave has got to, per participant.
///
/// The sweep and the upstream's wait ask two different questions of the same rows, and an answer
/// they disagreed on would mean a wave that releases nobody while its origin thinks it is done, or
/// an origin that waits on a participant the sweep will never start. Both read from here.
/// </summary>
public class ProbeWaveProgressReader : IDisposable
{
    private readonly SnapCdDbContext _dbContext;

    public ProbeWaveProgressReader(SnapCdDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<List<ParticipantProgress>> ListByWave(Guid probeWaveId, Guid organizationId)
    {
        return await Read(p => p.ProbeWaveId == probeWaveId && p.OrganizationId == organizationId);
    }

    /// <summary>One participant, or null when the row is gone.</summary>
    public async Task<ParticipantProgress?> GetParticipant(Guid participantId, Guid organizationId)
    {
        var rows = await Read(p => p.Id == participantId && p.OrganizationId == organizationId);
        return rows.FirstOrDefault();
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
            var participants = await ListByWave(waveId, organizationId);

            // The origin answers from its own plan, so it is never outstanding on its own account.
            if (participants.Any(p => p.IsOutstanding)) return ProbeWaveSettlement.Waiting;
        }

        return ProbeWaveSettlement.Settled;
    }

    private async Task<List<ParticipantProgress>> Read(
        System.Linq.Expressions.Expression<Func<Entities.Definition.ProbeWaves.ProbeWaveParticipant, bool>> where)
    {
        var rows = await _dbContext.ProbeWaveParticipants
            .AsNoTracking()
            .Where(where)
            .Select(p => new
            {
                p.Id,
                p.ProbeWaveId,
                p.ModuleId,
                p.OrganizationId,
                HasSignalled = p.SignalOutputSetId != null,
                Statuses = p.Contributions.Select(c => c.Status).ToList()
            })
            .ToListAsync();

        return rows
            .Select(r => new ParticipantProgress(
                r.Id, r.ProbeWaveId, r.ModuleId, r.OrganizationId, r.HasSignalled, r.Statuses))
            .ToList();
    }

    public void Dispose()
    {
        _dbContext.Dispose();
    }
}
