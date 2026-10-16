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
using SnapCd.Server.Core.Services.DependencyGraph;
using SnapCd.Server.Core.Settings;

namespace SnapCd.Server.Core.Services.RippleProbes;

public class ProbeWaveStarterFactory(
    IDbContextFactory<SnapCdDbContext> dbFactory,
    ApplyModuleGraphServiceFactory graphFactory,
    RippleProbeConsumerLookupFactory lookupFactory,
    IPublishEndpoint bus,
    IOptions<ProbePropagationSettings> settings,
    ILoggerFactory loggerFactory)
{
    public ProbeWaveStarter Create()
    {
        return new ProbeWaveStarter(
            dbFactory.CreateDbContext(),
            graphFactory.Create(),
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
    private readonly ApplyModuleGraphService _graph;
    private readonly RippleProbeConsumerLookup _lookup;
    private readonly IPublishEndpoint _bus;
    private readonly ProbePropagationSettings _settings;
    private readonly ILogger<ProbeWaveStarter> _logger;

    public ProbeWaveStarter(
        SnapCdDbContext dbContext,
        ApplyModuleGraphService graph,
        RippleProbeConsumerLookup lookup,
        IPublishEndpoint bus,
        ProbePropagationSettings settings,
        ILogger<ProbeWaveStarter> logger)
    {
        _dbContext = dbContext;
        _graph = graph;
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

        var consumers = await _lookup.ListConsumers(originModuleId, organizationId, changedOutputNames);
        if (consumers.Count == 0) return null;

        // Participants are the Modules reachable from the origin, which is wider than the Modules
        // reading a changed output: a Module two hops down takes part once the one above it
        // reports. Over-inclusion is safe, because a participant with no contribution is released
        // immediately.
        var reachable = await ReachableModuleIds(originModuleId);

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

            _dbContext.SignalContributions.Add(new SignalContribution
            {
                Id = Guid.NewGuid(),
                OrganizationId = organizationId,
                ProbeWaveParticipantId = consumerParticipantId,
                UpstreamParticipantId = participantIds[originModuleId],
                Status = SignalContributionStatus.Potential
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

        return wave.Id;
    }

    private async Task<List<Guid>> ReachableModuleIds(Guid originModuleId)
    {
        var dependencies = await _graph.ListRecursiveApplyDependenciesForReferencedModule(originModuleId);

        return dependencies
            .Select(d => d.DefinedModuleId)
            .Where(id => id != originModuleId)
            .Distinct()
            .ToList();
    }

    public void Dispose()
    {
        _dbContext.Dispose();
        _graph.Dispose();
        _lookup.Dispose();
    }
}
