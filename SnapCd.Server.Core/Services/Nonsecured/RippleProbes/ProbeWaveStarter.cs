// SPDX-License-Identifier: LicenseRef-Snap-CD-Source-Available-1.1
// Copyright (c) 2026 Karl Schriek / Schrieksoft.
// No license is granted to use this file, in whole or in part, (a) as training, fine-tuning, retrieval, or
// embedding data for any machine-learning model, or (b) as input to any machine-learning model, agent, or automated
// system for the purpose of producing a derivative work or reimplementation that is not otherwise permitted by the
// Snap CD Source-Available License (including any Competing Product as defined therein). Contact info@snapcd.io
// for terms covering either use.

using MassTransit;
using Microsoft.EntityFrameworkCore;
using SnapCd.Contracts;
using SnapCd.Server.Core.Database;
using SnapCd.Server.Core.Entities.Definition.ProbeWaves;
using SnapCd.Server.Core.Enums;
using SnapCd.Server.Core.Events.Gatekeeping;
using SnapCd.Server.Core.Events.System;
using SnapCd.Server.Core.Services.Utils.RippleProbes;

namespace SnapCd.Server.Core.Services.Nonsecured.RippleProbes;

public class ProbeWaveStarterFactory(
    IDbContextFactory<SnapCdDbContext> dbFactory,
    RippleProbeConsumerLookupFactory lookupFactory,
    ProbeSignalRecorderFactory recorderFactory,
    IPublishEndpoint bus,
    ILoggerFactory loggerFactory)
{
    public ProbeWaveStarter Create()
    {
        return new ProbeWaveStarter(
            dbFactory.CreateDbContext(),
            lookupFactory.Create(),
            recorderFactory.Create(),
            bus,
            loggerFactory.CreateLogger<ProbeWaveStarter>());
    }
}

/// <summary>
/// Creates a wave from the Module whose plan produced the starting signals.
///
/// Only the Modules this change reaches get an edge at the outset.
/// That is sound because signals flow along the same edges as real outputs: a probe on a Module
/// produces signals on that Module's own outputs, and whoever reads those outputs already has a
/// contribution from it, so no Module becomes reachable part way through.
/// </summary>
public class ProbeWaveStarter : IDisposable
{
    private async Task<List<RippleProbeConsumer>> WithStateToPlanAgainst(
        List<RippleProbeConsumer> consumers,
        Guid organizationId)
    {
        if (consumers.Count == 0) return consumers;

        var moduleIds = consumers.Select(c => c.ModuleId).ToList();

        // An output set exists because a Module applied and reported its outputs, which is the
        // same thing as having state to plan against.
        var haveState = (await _dbContext.OutputSets
                .Where(o =>
                    moduleIds.Contains(o.ModuleId) &&
                    o.OrganizationId == organizationId)
                .Select(o => o.ModuleId)
                .Distinct()
                .ToListAsync())
            .ToHashSet();

        return consumers.Where(c => haveState.Contains(c.ModuleId)).ToList();
    }

    /// <summary>
    /// Drops the Modules whose most recent finished job left them destroyed. Their outputs are
    /// gone, so there is nothing for a consumer to plan against and no Apply to ask them for.
    ///
    /// Best effort by nature: this reads the state as it is now, and a Module can be destroyed
    /// the moment after it passes. A probe owed by a Module destroyed later is answered when the
    /// step runs, not here.
    /// </summary>
    private async Task<List<RippleProbeConsumer>> WithoutDestroyed(
        List<RippleProbeConsumer> consumers,
        Guid organizationId)
    {
        if (consumers.Count == 0) return consumers;

        var moduleIds = consumers.Select(c => c.ModuleId).ToList();

        var destroyed = (await _dbContext.ModuleJobs
                .Where(j =>
                    moduleIds.Contains(j.ModuleId) &&
                    j.OrganizationId == organizationId &&
                    j.ActualStateHeadline != null &&
                    j.TimestampEnd != null)
                .GroupBy(j => j.ModuleId)
                .Select(g => new
                {
                    ModuleId = g.Key,
                    Latest = g.OrderByDescending(j => j.JobNumber)
                        .Select(j => j.ActualStateHeadline)
                        .First()
                })
                .Where(x => x.Latest == ActualStateHeadline.Destroyed)
                .Select(x => x.ModuleId)
                .ToListAsync())
            .ToHashSet();

        return consumers.Where(c => !destroyed.Contains(c.ModuleId)).ToList();
    }

    private readonly SnapCdDbContext _dbContext;
    private readonly RippleProbeConsumerLookup _lookup;
    private readonly ProbeSignalRecorder _recorder;
    private readonly IPublishEndpoint _bus;
    private readonly ILogger<ProbeWaveStarter> _logger;

    public ProbeWaveStarter(
        SnapCdDbContext dbContext,
        RippleProbeConsumerLookup lookup,
        ProbeSignalRecorder recorder,
        IPublishEndpoint bus,
        ILogger<ProbeWaveStarter> logger)
    {
        _dbContext = dbContext;
        _lookup = lookup;
        _recorder = recorder;
        _bus = bus;
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
        IReadOnlyCollection<string> changedOutputNames,
        string? outputSignalsJson = null)
    {
        if (changedOutputNames.Count == 0) return null;

        var consumers = await _lookup.ListConsumers(originModuleId, organizationId, changedOutputNames);

        // A probe plans the consumer with a value predicted for one of its variables. A Module that
        // has never reached a terminal state has no state and no outputs, so that plan would answer
        // a different question from the one asked.
        consumers = await WithStateToPlanAgainst(consumers, organizationId);
        consumers = await WithoutDestroyed(consumers, organizationId);
        if (consumers.Count == 0) return null;

        var wave = new ProbeWave
        {
            Id = Guid.NewGuid(),
            OrganizationId = organizationId,
            OriginModuleJobId = originModuleJobId
        };

        _dbContext.ProbeWaves.Add(wave);

        // One edge per Module this change reaches, Confirmed rather than Potential: the edge
        // exists because the consumer was found to read an output that changed, so the question
        // Potential asks is already answered. The origin answers from its own plan and is never
        // probed, so nothing else would resolve them.
        //
        // A Module further downstream gets its edge when the one above it reports, because
        // whether the change reaches it depends on what that Module's plan turns out to change.
        foreach (var consumer in consumers)
            _dbContext.SignalEdges.Add(new SignalEdge
            {
                Id = Guid.NewGuid(),
                OrganizationId = organizationId,
                ProbeWaveId = wave.Id,
                ConsumerModuleId = consumer.ModuleId,
                UpstreamModuleId = originModuleId,
                Status = SignalEdgeStatus.Confirmed
            });

        await _dbContext.SaveChangesAsync();

        // The origin answers from its own plan rather than by being probed, so its signals are
        // written here. Without them a consumer's inputs document comes out empty, because
        // building one requires every confirmed upstream to have reported.
        if (!string.IsNullOrWhiteSpace(outputSignalsJson))
        {
            await _recorder.Record(
                wave.Id, originModuleId, organizationId, originModuleJobId, outputSignalsJson!);
        }

        _logger.LogInformation(
            "Propagation started from Module {ModuleId}: {Edges} Module(s) waiting on it",
            originModuleId, consumers.Count);

        await _bus.Publish(new ProbeWaveUpdatedEvent
        {
            ProbeWaveId = wave.Id,
            OrganizationId = organizationId,
            ModuleId = originModuleId
        });

        // The origin's edges are written Confirmed, so its consumers are ready the moment the
        // wave exists. Nothing else asks them. The gate re-checks the wave per Module and
        // serialises on the Module's own saga, so two asks cannot both start an Apply.
        foreach (var consumer in consumers)
            await _bus.Publish(new GatekeepingJobRequested
            {
                ModuleId = consumer.ModuleId,
                OrganizationId = organizationId,
                DesiredStateHeadline = DesiredStateHeadline.Applied,
                SetNewDesiredState = false
            });

        return wave.Id;
    }

    public void Dispose()
    {
        _dbContext.Dispose();
        _lookup.Dispose();
        _recorder.Dispose();
    }
}
