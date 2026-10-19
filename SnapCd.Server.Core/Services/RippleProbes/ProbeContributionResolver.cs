// SPDX-License-Identifier: LicenseRef-Snap-CD-Source-Available-1.1
// Copyright (c) 2026 Karl Schriek / Schrieksoft.
// No license is granted to use this file, in whole or in part, (a) as training, fine-tuning, retrieval, or
// embedding data for any machine-learning model, or (b) as input to any machine-learning model, agent, or automated
// system for the purpose of producing a derivative work or reimplementation that is not otherwise permitted by the
// Snap CD Source-Available License (including any Competing Product as defined therein). Contact info@snapcd.io
// for terms covering either use.

using MassTransit;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using SnapCd.Server.Core.Database;
using SnapCd.Server.Core.Entities.Definition.ProbeWaves;
using SnapCd.Server.Core.Enums;
using SnapCd.Server.Core.Events.System;

namespace SnapCd.Server.Core.Services.RippleProbes;

public class ProbeContributionResolverFactory(
    IDbContextFactory<SnapCdDbContext> dbFactory,
    RippleProbeConsumerLookupFactory lookupFactory,
    IPublishEndpoint bus,
    ILoggerFactory loggerFactory)
{
    public ProbeContributionResolver Create()
    {
        return new ProbeContributionResolver(
            dbFactory.CreateDbContext(),
            lookupFactory.Create(),
            bus,
            loggerFactory.CreateLogger<ProbeContributionResolver>());
    }
}

/// <summary>
/// Resolves the edges leading out of a participant once it has answered.
///
/// A contribution resolves exactly once, from Potential to one of the three answers, and a
/// consumer is released when none of its contributions is Potential any more.
/// </summary>
public class ProbeContributionResolver : IDisposable
{
    private readonly SnapCdDbContext _dbContext;
    private readonly RippleProbeConsumerLookup _lookup;
    private readonly IPublishEndpoint _bus;
    private readonly ILogger<ProbeContributionResolver> _logger;

    public ProbeContributionResolver(
        SnapCdDbContext dbContext,
        RippleProbeConsumerLookup lookup,
        IPublishEndpoint bus,
        ILogger<ProbeContributionResolver> logger)
    {
        _dbContext = dbContext;
        _lookup = lookup;
        _bus = bus;
        _logger = logger;
    }

    /// <summary>
    /// A participant reported signals. Each consumer waiting on it is told whether any of those
    /// signals is one it reads.
    /// </summary>
    public async Task ResolveAfterSignals(Guid participantId, Guid organizationId)
    {
        var participant = await _dbContext.ProbeWaveParticipants
            .Where(p => p.Id == participantId && p.OrganizationId == organizationId)
            .Select(p => new { p.Id, p.ModuleId, p.ProbeWaveId, p.SignalOutputSetId })
            .FirstOrDefaultAsync();

        if (participant?.SignalOutputSetId == null) return;

        var signalNames = await _dbContext.SignalOutputs
            .Where(s => s.SignalOutputSetId == participant.SignalOutputSetId.Value &&
                        s.OrganizationId == organizationId)
            .Select(s => s.Name)
            .ToListAsync();

        var reading = signalNames.Count == 0
            ? []
            : (await _lookup.ListConsumers(participant.ModuleId, organizationId, signalNames))
                .Select(c => c.ModuleId)
                .ToHashSet();

        await Resolve(
            participant.Id,
            participant.ProbeWaveId,
            organizationId,
            consumerModuleId => reading.Contains(consumerModuleId)
                ? SignalContributionStatus.Confirmed
                : SignalContributionStatus.NotNeeded);
    }

    /// <summary>
    /// A participant will never report, because its job failed, it is paused, or it no longer
    /// exists. Its consumers are released rather than left waiting on an answer that is not
    /// coming, which is what Unanswerable is for.
    /// </summary>
    public async Task ResolveAsUnanswerable(Guid participantId, Guid organizationId, string why)
    {
        var participant = await _dbContext.ProbeWaveParticipants
            .Where(p => p.Id == participantId && p.OrganizationId == organizationId)
            .Select(p => new { p.Id, p.ProbeWaveId, p.ModuleId })
            .FirstOrDefaultAsync();

        if (participant == null) return;

        _logger.LogWarning(
            "Propagation: Module {ModuleId} will not report ({Why}), releasing what waited on it",
            participant.ModuleId, why);

        await Resolve(
            participant.Id,
            participant.ProbeWaveId,
            organizationId,
            _ => SignalContributionStatus.Unanswerable);
    }

    private async Task Resolve(
        Guid upstreamParticipantId,
        Guid waveId,
        Guid organizationId,
        Func<Guid, SignalContributionStatus> decide)
    {
        var pending = await _dbContext.SignalContributions
            .Where(c =>
                c.UpstreamParticipantId == upstreamParticipantId &&
                c.OrganizationId == organizationId &&
                c.Status == SignalContributionStatus.Potential)
            .ToListAsync();

        if (pending.Count == 0)
        {
            // A leaf consumer has no outgoing edges, so nothing resolves here, but the origin was
            // still waiting on its answer.
            await NotifyWaveOrigin(waveId, organizationId);
            return;
        }

        var consumerModuleIds = await _dbContext.ProbeWaveParticipants
            .Where(p => pending.Select(c => c.ProbeWaveParticipantId).Contains(p.Id))
            .Select(p => new { p.Id, p.ModuleId })
            .ToListAsync();

        foreach (var contribution in pending)
        {
            var consumerModuleId = consumerModuleIds
                .FirstOrDefault(p => p.Id == contribution.ProbeWaveParticipantId)?.ModuleId;

            if (consumerModuleId == null) continue;

            contribution.Status = decide(consumerModuleId.Value);
        }

        await _dbContext.SaveChangesAsync();

        await _bus.Publish(new ProbeWaveUpdatedEvent
        {
            ProbeWaveId = waveId,
            OrganizationId = organizationId
        });

        await NotifyWaveOrigin(waveId, organizationId);
    }

    /// <summary>
    /// Tells the job that started this wave to look at it again. That job is parked waiting for
    /// its consumers, and this resolution may have been the last thing it was waiting on.
    /// </summary>
    private async Task NotifyWaveOrigin(Guid waveId, Guid organizationId)
    {
        var originJobId = await _dbContext.ProbeWaves
            .Where(w => w.Id == waveId && w.OrganizationId == organizationId)
            .Select(w => w.OriginModuleJobId)
            .FirstOrDefaultAsync();

        if (originJobId == Guid.Empty) return;

        await _bus.Publish(new ProbeWaveProgressedEvent
        {
            OriginModuleJobId = originJobId,
            OrganizationId = organizationId
        });
    }

    public void Dispose()
    {
        _dbContext.Dispose();
        _lookup.Dispose();
    }
}
