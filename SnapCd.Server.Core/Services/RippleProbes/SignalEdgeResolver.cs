// SPDX-License-Identifier: LicenseRef-Snap-CD-Source-Available-1.1
// Copyright (c) 2026 Karl Schriek / Schrieksoft.
// No license is granted to use this file, in whole or in part, (a) as training, fine-tuning, retrieval, or
// embedding data for any machine-learning model, or (b) as input to any machine-learning model, agent, or automated
// system for the purpose of producing a derivative work or reimplementation that is not otherwise permitted by the
// Snap CD Source-Available License (including any Competing Product as defined therein). Contact info@snapcd.io
// for terms covering either use.

using MassTransit;
using System.Text.Json;
using System.Text.Json.Nodes;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using SnapCd.Contracts;
using SnapCd.Server.Core.Events.Gatekeeping;
using SnapCd.Server.Core.Database;
using SnapCd.Server.Core.Entities.Definition.ProbeWaves;
using SnapCd.Server.Core.Entities.Definition.Signals;
using SnapCd.Server.Core.Enums;
using SnapCd.Server.Core.Events.System;

namespace SnapCd.Server.Core.Services.RippleProbes;

public class SignalEdgeResolverFactory(
    IDbContextFactory<SnapCdDbContext> dbFactory,
    RippleProbeConsumerLookupFactory lookupFactory,
    IPublishEndpoint bus,
    ILoggerFactory loggerFactory)
{
    public SignalEdgeResolver Create()
    {
        return new SignalEdgeResolver(
            dbFactory.CreateDbContext(),
            lookupFactory.Create(),
            bus,
            loggerFactory.CreateLogger<SignalEdgeResolver>());
    }
}

/// <summary>
/// Answers the edges leading out of a Module once it has reported, and extends the wave to
/// whoever reads what it changed.
///
/// An edge resolves exactly once, from Potential to one of the three answers, and a consumer
/// probes when none of its edges is Potential any more.
/// </summary>
public class SignalEdgeResolver : IDisposable
{
    private readonly SnapCdDbContext _dbContext;
    private readonly RippleProbeConsumerLookup _lookup;
    private readonly IPublishEndpoint _bus;
    private readonly ILogger<SignalEdgeResolver> _logger;

    public SignalEdgeResolver(
        SnapCdDbContext dbContext,
        RippleProbeConsumerLookup lookup,
        IPublishEndpoint bus,
        ILogger<SignalEdgeResolver> logger)
    {
        _dbContext = dbContext;
        _lookup = lookup;
        _bus = bus;
        _logger = logger;
    }

    /// <summary>
    /// A Module reported signals. Each consumer waiting on it is told whether any of those
    /// signals is one it reads, and the Modules reading them join the wave.
    /// </summary>
    public async Task ResolveAfterSignals(Guid probeWaveId, Guid moduleId, Guid organizationId)
    {
        var signalOutputSetId = await _dbContext.ModuleProbeAttempts
            .AsNoTracking()
            .Where(a => a.ProbeWaveId == probeWaveId && a.ModuleId == moduleId &&
                        a.OrganizationId == organizationId)
            .Select(a => a.SignalOutputSetId)
            .FirstOrDefaultAsync();

        if (signalOutputSetId == null) return;

        var changedNames = await ChangedSignalNames(signalOutputSetId.Value, organizationId);

        var reading = changedNames.Count == 0
            ? []
            : (await _lookup.ListConsumers(moduleId, organizationId, changedNames))
                .Select(c => c.ModuleId)
                .ToHashSet();

        // The wave grows here. A Module reading one of these changed outputs gets an edge now
        // rather than at creation: whether it is reached depends on what this plan turned out to
        // change, which nothing knew when the wave started.
        //
        // A wave informs one approval decision and ends when that decision is made, so it stops
        // growing then. A probe still in flight at that moment reports its result as normal, and
        // the Modules its signals reach are simply not enrolled: there is no longer a decision for
        // their answers to inform.
        if (await IsLive(probeWaveId, organizationId))
            await AddEdges(probeWaveId, moduleId, organizationId, reading);
        else
            _logger.LogDebug(
                "Wave {ProbeWaveId} is closed, so the signals from Module {ModuleId} reach no further",
                probeWaveId, moduleId);

        await Resolve(
            probeWaveId,
            moduleId,
            organizationId,
            consumerModuleId => reading.Contains(consumerModuleId)
                ? SignalEdgeStatus.Confirmed
                : SignalEdgeStatus.NotNeeded);
    }

    /// <summary>
    /// Whether the approval this wave informs is still open. The wave ends with that decision, and
    /// an ended wave neither grows nor asks for another probe.
    /// </summary>
    private async Task<bool> IsLive(Guid probeWaveId, Guid organizationId) =>
        await _dbContext.ProbeWaves.AsNoTracking()
            .Where(w => w.Id == probeWaveId && w.OrganizationId == organizationId)
            .Join(_dbContext.ModuleJobs.Where(j =>
                    j.Status == ExecutionStatus.Running && j.WaitingForApproval == true),
                w => w.OriginModuleJobId, j => j.Id, (w, _) => w.Id)
            .AnyAsync();

    /// <summary>
    /// A Module will never report, because its job failed, it is paused, or it no longer exists.
    /// Its consumers are released rather than left waiting on an answer that is not coming, which
    /// is what Unanswerable is for.
    /// </summary>
    public async Task ResolveAsUnanswerable(
        Guid probeWaveId,
        Guid moduleId,
        Guid organizationId,
        string why)
    {
        _logger.LogWarning(
            "Propagation: Module {ModuleId} will not report ({Why}), releasing what waited on it",
            moduleId, why);

        await Resolve(probeWaveId, moduleId, organizationId, _ => SignalEdgeStatus.Unanswerable);
    }

    /// <summary>
    /// Writes an edge from this Module to each consumer of what it changed.
    ///
    /// Two upstreams reaching one consumer write two rows that share nothing, so there is no row
    /// for them to contend over and no need to create anything before an edge can be written. The
    /// unique index refuses a repeat, which is what makes a second report idempotent.
    /// </summary>
    private async Task AddEdges(
        Guid probeWaveId,
        Guid upstreamModuleId,
        Guid organizationId,
        IReadOnlyCollection<Guid> consumerModuleIds)
    {
        if (consumerModuleIds.Count == 0) return;

        var already = await _dbContext.SignalEdges
            .AsNoTracking()
            .Where(e => e.ProbeWaveId == probeWaveId &&
                        e.UpstreamModuleId == upstreamModuleId &&
                        e.OrganizationId == organizationId)
            .Select(e => e.ConsumerModuleId)
            .ToListAsync();

        var added = 0;

        foreach (var consumerModuleId in consumerModuleIds)
        {
            if (already.Contains(consumerModuleId)) continue;

            _dbContext.SignalEdges.Add(new SignalEdge
            {
                Id = Guid.NewGuid(),
                OrganizationId = organizationId,
                ProbeWaveId = probeWaveId,
                ConsumerModuleId = consumerModuleId,
                UpstreamModuleId = upstreamModuleId,
                Status = SignalEdgeStatus.Potential
            });

            added++;
        }

        if (added == 0) return;

        await _dbContext.SaveChangesAsync();

        _logger.LogInformation(
            "Propagation: wave {ProbeWaveId} reached {Count} further Module(s)", probeWaveId, added);
    }

    private async Task Resolve(
        Guid probeWaveId,
        Guid upstreamModuleId,
        Guid organizationId,
        Func<Guid, SignalEdgeStatus> decide)
    {
        var pending = await _dbContext.SignalEdges
            .Where(e =>
                e.ProbeWaveId == probeWaveId &&
                e.UpstreamModuleId == upstreamModuleId &&
                e.OrganizationId == organizationId &&
                e.Status == SignalEdgeStatus.Potential)
            .ToListAsync();

        if (pending.Count == 0)
        {
            // A leaf consumer has no outgoing edges, so nothing resolves here, but its own report
            // is still a change to what the wave shows.
            await NotifyWave(probeWaveId, organizationId);
            return;
        }

        foreach (var edge in pending)
            edge.Status = decide(edge.ConsumerModuleId);

        await _dbContext.SaveChangesAsync();

        await NotifyWave(probeWaveId, organizationId);

        // Whoever waited on this Module may now be waiting on nothing. Each is asked for an Apply,
        // and the job decides for itself whether it owes a probe. SetNewDesiredState is false, so
        // a probe cannot move a Module towards Applied when its desired state is Destroyed.
        foreach (var edge in pending)
            await _bus.Publish(new GatekeepingJobRequested
            {
                ModuleId = edge.ConsumerModuleId,
                OrganizationId = organizationId,
                DesiredStateHeadline = DesiredStateHeadline.Applied,
                SetNewDesiredState = false
            });
    }

    /// <summary>Tells open pages that what the wave shows has changed.</summary>
    private Task NotifyWave(Guid probeWaveId, Guid organizationId) =>
        _bus.Publish(new ProbeWaveUpdatedEvent
        {
            ProbeWaveId = probeWaveId,
            OrganizationId = organizationId
        });

    /// <summary>
    /// The outputs in a signal set whose value actually changes.
    ///
    /// A set holds every output the plan mentioned, including ones it says are unaffected, because
    /// a reader of the wave wants to see that an output was considered and did not move. Only the
    /// ones that change reach a consumer, so the action decides rather than mere membership: the
    /// two sources of a signal set disagree on whether to include an unchanged output at all, and
    /// reading the action is correct for both.
    /// </summary>
    private async Task<List<string>> ChangedSignalNames(Guid signalOutputSetId, Guid organizationId)
    {
        var stored = await _dbContext.SignalOutputs
            .AsNoTracking()
            .Where(s => s.SignalOutputSetId == signalOutputSetId && s.OrganizationId == organizationId)
            .Select(s => new { s.Name, Json = (s as LiteralSignalOutput)!.SignalJson })
            .ToListAsync();

        var changed = new List<string>();

        foreach (var output in stored)
        {
            // A sensitive output's value lives in the secret store and its row carries no JSON, so
            // nothing here can say whether it changed. Treated as changed: missing the propagation
            // of a secret that moved is worse than probing for one that did not.
            if (output.Json == null)
            {
                changed.Add(output.Name);
                continue;
            }

            if (!IsNoOp(output.Json)) changed.Add(output.Name);
        }

        return changed;
    }

    /// <summary>
    /// Whether the plan says it does nothing to this output. The action is the plan's own, in the
    /// form "tofu show -json" writes it: one element for a simple action, two for a replace.
    /// </summary>
    private static bool IsNoOp(string signalJson)
    {
        try
        {
            var action = JsonNode.Parse(signalJson)?["action"]?.AsArray();
            return action is { Count: 1 } && action[0]?.GetValue<string>() == "no-op";
        }
        catch (Exception e) when (e is JsonException or InvalidOperationException)
        {
            // An unreadable value is not evidence that nothing changed.
            return false;
        }
    }

    public void Dispose()
    {
        _dbContext.Dispose();
        _lookup.Dispose();
    }
}
