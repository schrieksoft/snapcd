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
using Microsoft.Extensions.Options;
using SnapCd.Server.Core.Database;
using SnapCd.Server.Core.Entities.Definition.ProbeWaves;
using SnapCd.Server.Core.Enums;
using SnapCd.Server.Core.Events.System;
using SnapCd.Server.Core.Settings;

namespace SnapCd.Server.Core.Services.RippleProbes;

public class ProbeWaveStarterFactory(
    IDbContextFactory<SnapCdDbContext> dbFactory,
    RippleProbeConsumerLookupFactory lookupFactory,
    IPublishEndpoint bus,
    IOptions<ProbePropagationSettings> settings,
    ILoggerFactory loggerFactory)
{
    public ProbeWaveStarter Create()
    {
        return new ProbeWaveStarter(
            dbFactory.CreateDbContext(),
            lookupFactory.Create(),
            bus,
            settings.Value,
            loggerFactory.CreateLogger<ProbeWaveStarter>());
    }
}

/// <summary>
/// Creates a wave from the Module whose plan produced the starting signals.
///
/// Participants and contributions are both written at the outset, from the graph as it is then.
/// That is sound because signals flow along the same edges as real outputs: a probe on a Module
/// produces signals on that Module's own outputs, and whoever reads those outputs already has a
/// contribution from it, so no Module becomes reachable part way through.
/// </summary>
public class ProbeWaveStarter : IDisposable
{
    private readonly SnapCdDbContext _dbContext;
    private readonly RippleProbeConsumerLookup _lookup;
    private readonly IPublishEndpoint _bus;
    private readonly ProbePropagationSettings _settings;
    private readonly ILogger<ProbeWaveStarter> _logger;

    public ProbeWaveStarter(
        SnapCdDbContext dbContext,
        RippleProbeConsumerLookup lookup,
        IPublishEndpoint bus,
        ProbePropagationSettings settings,
        ILogger<ProbeWaveStarter> logger)
    {
        _dbContext = dbContext;
        _lookup = lookup;
        _bus = bus;
        _settings = settings;
        _logger = logger;
    }

    /// <summary>
    /// Starts a wave for the outputs this Module's plan says will change, or returns null when
    /// nothing downstream reads any of them.
    /// </summary>
    public async Task<Guid?> Start(
        Guid originModuleId,
        Guid organizationId,
        Guid originModuleJobId,
        IReadOnlyCollection<string> changedOutputNames)
    {
        if (changedOutputNames.Count == 0) return null;

        // A probe's Apply is an ordinary Apply, so its plan reports changed outputs and would
        // start a wave of its own. It exists to answer a wave rather than to begin one, and a
        // wave per probe would nest without bound: each one counts itself as depth zero.
        if (await IsProbingForAWave(originModuleJobId, organizationId))
        {
            _logger.LogDebug(
                "Job {ModuleJobId} is itself a probe, so it answers its wave rather than starting one",
                originModuleJobId);

            return null;
        }

        var consumers = await _lookup.ListConsumers(originModuleId, organizationId, changedOutputNames);
        if (consumers.Count == 0) return null;

        // Participants are the Modules reachable from the origin, which is wider than the Modules
        // reading a changed output: a Module two hops down takes part once the one above it
        // reports. Over-inclusion is safe, because a participant with no contribution is released
        // immediately.
        var reachable = await ReachableModuleIds(originModuleId, organizationId);

        var wave = new ProbeWave
        {
            Id = Guid.NewGuid(),
            OrganizationId = organizationId,
            OriginModuleId = originModuleId,
            OriginModuleJobId = originModuleJobId,
            MaxDepth = _settings.MaxDepth
        };

        _dbContext.ProbeWaves.Add(wave);

        // The origin is a participant: its own plan produced the starting signals, so it answers
        // for itself rather than being probed.
        var participantIds = new Dictionary<Guid, Guid>();
        foreach (var moduleId in reachable.Append(originModuleId).Distinct())
        {
            var participant = new ProbeWaveParticipant
            {
                Id = Guid.NewGuid(),
                OrganizationId = organizationId,
                ProbeWaveId = wave.Id,
                ModuleId = moduleId
            };

            _dbContext.ProbeWaveParticipants.Add(participant);
            participantIds[moduleId] = participant.Id;
        }

        var edges = 0;
        foreach (var consumer in consumers)
        {
            if (!participantIds.TryGetValue(consumer.ModuleId, out var consumerParticipantId)) continue;

            // Confirmed rather than Potential: these edges exist because the consumer was found
            // to read a changed output, so the question Potential asks is already answered. The
            // origin answers from its own plan and is never probed, so nothing else would resolve
            // them.
            _dbContext.SignalContributions.Add(new SignalContribution
            {
                Id = Guid.NewGuid(),
                OrganizationId = organizationId,
                ProbeWaveParticipantId = consumerParticipantId,
                UpstreamParticipantId = participantIds[originModuleId],
                Status = SignalContributionStatus.Confirmed
            });

            edges++;
        }

        await _dbContext.SaveChangesAsync();

        _logger.LogInformation(
            "Propagation started from Module {ModuleId}: {Participants} Modules, {Edges} waiting on it",
            originModuleId, participantIds.Count, edges);

        await _bus.Publish(new ProbeWaveUpdatedEvent
        {
            ProbeWaveId = wave.Id,
            OrganizationId = organizationId,
            ModuleId = originModuleId
        });

        // The origin's edges are written Confirmed, so its consumers are released the moment the
        // wave exists. Nothing else would ask them.
        foreach (var consumer in consumers)
        {
            if (!participantIds.TryGetValue(consumer.ModuleId, out var participantId)) continue;

            await _bus.Publish(new ProbeReadinessCheckRequested
            {
                ProbeWaveParticipantId = participantId,
                ModuleId = consumer.ModuleId,
                OrganizationId = organizationId
            });
        }

        return wave.Id;
    }

    /// <summary>
    /// Whether this job was started to probe for a wave. The readiness check records the job on
    /// the participant before starting it, so the row exists by the time a plan completes.
    /// </summary>
    private async Task<bool> IsProbingForAWave(Guid moduleJobId, Guid organizationId)
    {
        return await _dbContext.ProbeWaveParticipants
            .AsNoTracking()
            .AnyAsync(p =>
                p.LastProbeModuleJobId == moduleJobId &&
                p.OrganizationId == organizationId);
    }

    /// <summary>
    /// The Modules downstream of the origin, transitively.
    ///
    /// The destroy direction of the closure, which is the one that walks from a Module to those
    /// that depend on it. The apply direction walks the other way, to the Modules it depends on,
    /// which is the opposite of where a signal travels.
    ///
    /// Projected to ids rather than read as entities: the closure view carries denormalised state
    /// columns this does not need, and a stale value in one of them would throw here.
    /// </summary>
    private async Task<List<Guid>> ReachableModuleIds(Guid originModuleId, Guid organizationId)
    {
        return await _dbContext.RecursiveDestroyDependencies
            .AsNoTracking()
            .Where(rd =>
                rd.RootModuleId == originModuleId &&
                rd.RootOrganizationId == organizationId &&
                rd.DefinedModuleId != originModuleId)
            .Select(rd => rd.DefinedModuleId)
            .Distinct()
            .ToListAsync();
    }

    public void Dispose()
    {
        _dbContext.Dispose();
        _lookup.Dispose();
    }
}
