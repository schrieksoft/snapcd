// SPDX-License-Identifier: LicenseRef-Snap-CD-Source-Available-1.1
// Copyright (c) 2026 Karl Schriek / Schrieksoft.
// No license is granted to use this file, in whole or in part, (a) as training, fine-tuning, retrieval, or
// embedding data for any machine-learning model, or (b) as input to any machine-learning model, agent, or automated
// system for the purpose of producing a derivative work or reimplementation that is not otherwise permitted by the
// Snap CD Source-Available License (including any Competing Product as defined therein). Contact info@snapcd.io
// for terms covering either use.

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using SnapCd.Server.Core.Database;
using SnapCd.Server.Core.Entities.Definition;
using SnapCd.Server.Core.Enums;

namespace SnapCd.Server.Core.Services.RippleProbes;

public class ProbeWaveReadinessFactory(
    IDbContextFactory<SnapCdDbContext> dbFactory,
    ProbeWaveProgressReaderFactory progressFactory,
    ILoggerFactory loggerFactory)
{
    public ProbeWaveReadiness Create()
    {
        return new ProbeWaveReadiness(
            dbFactory,
            progressFactory,
            loggerFactory.CreateLogger<ProbeWaveReadiness>());
    }
}

/// <summary>
/// Whether a Module can usefully be probed for a wave right now.
///
/// Readiness is recomputed from the Module's edges on every ask rather than carried on a message,
/// so an extra ask answers no and one that arrives out of order still reads the current picture.
/// </summary>
public class ProbeWaveReadiness(
    IDbContextFactory<SnapCdDbContext> dbContextFactory,
    ProbeWaveProgressReaderFactory progressFactory,
    ILogger<ProbeWaveReadiness> logger)
{
    public async Task<bool> IsReady(Guid probeWaveId, Guid moduleId, Guid organizationId)
    {
        using var progress = progressFactory.Create();
        var module = await progress.GetModule(probeWaveId, moduleId, organizationId);

        if (module == null)
        {
            logger.LogDebug(
                "No edge into Module {ModuleId} in wave {ProbeWaveId}", moduleId, probeWaveId);
            return false;
        }

        if (!module.OwesAProbe)
        {
            logger.LogDebug(
                "Module {ModuleId} is not ready to probe yet, leaving it to the next ask", moduleId);
            return false;
        }

        if (await AnUpstreamMayStillReport(probeWaveId, moduleId, organizationId))
        {
            logger.LogDebug(
                "Module {ModuleId} reads an output from a Module that has not reported yet, so it waits",
                moduleId);
            return false;
        }

        if (!await OriginIsAwaitingApproval(probeWaveId, organizationId))
        {
            logger.LogDebug(
                "Wave {ProbeWaveId} is no longer live, not probing Module {ModuleId}",
                probeWaveId, moduleId);
            return false;
        }

        return true;
    }

    /// <summary>
    /// Whether some Module this one reads an output from is in the wave and has not answered yet.
    /// Probing now would answer with one upstream's value while another was still coming, which
    /// bites on a Module reachable by two paths of different lengths.
    /// </summary>
    private async Task<bool> AnUpstreamMayStillReport(Guid probeWaveId, Guid moduleId, Guid organizationId)
    {
        await using var dbContext = await dbContextFactory.CreateDbContextAsync();

        // Queried per concrete type: the hierarchy is one table and EF can only filter on a leaf,
        // so an OfType on the shared base does not translate. All three kinds count, including the
        // output-set edge, where the consumer reads whatever the producer publishes.
        var producerModuleIds = new List<Guid>();

        producerModuleIds.AddRange(await ProducersFor<ModuleParamFromOutput>(dbContext, moduleId, organizationId));
        producerModuleIds.AddRange(await ProducersFor<ModuleEnvVarFromOutput>(dbContext, moduleId, organizationId));
        producerModuleIds.AddRange(await ProducersFor<ModuleParamFromOutputSet>(dbContext, moduleId, organizationId));

        producerModuleIds = producerModuleIds.Distinct().ToList();

        if (producerModuleIds.Count == 0) return false;

        // The upstreams this wave has actually reached, which is the set that could still send
        // this Module a second edge. One that the wave never reached cannot.
        var inWave = await dbContext.SignalEdges
            .AsNoTracking()
            .Where(e => e.ProbeWaveId == probeWaveId && e.OrganizationId == organizationId)
            .Select(e => e.ConsumerModuleId)
            .Distinct()
            .ToListAsync();

        var pendingProducers = producerModuleIds
            .Where(id => id != moduleId && inWave.Contains(id))
            .ToList();

        if (pendingProducers.Count == 0) return false;

        var reportedCount = await dbContext.ModuleProbeAttempts
            .AsNoTracking()
            .CountAsync(a => a.ProbeWaveId == probeWaveId &&
                             a.OrganizationId == organizationId &&
                             a.SignalOutputSetId != null &&
                             pendingProducers.Contains(a.ModuleId));

        return reportedCount < pendingProducers.Count;
    }

    private static async Task<List<Guid>> ProducersFor<TEntity>(
        SnapCdDbContext dbContext,
        Guid moduleId,
        Guid organizationId)
        where TEntity : ModuleInputWithOutputModuleId
    {
        return await dbContext.Set<TEntity>()
            .AsNoTracking()
            .Where(i => i.ModuleId == moduleId && i.OrganizationId == organizationId)
            .Select(i => i.OutputModuleId)
            .Distinct()
            .ToListAsync();
    }

    /// <summary>
    /// Whether the origin's approval decision is still open. A wave informs one decision, so it
    /// ends when that decision is made.
    /// </summary>
    private async Task<bool> OriginIsAwaitingApproval(Guid probeWaveId, Guid organizationId)
    {
        await using var dbContext = await dbContextFactory.CreateDbContextAsync();

        return await dbContext.ProbeWaves.AsNoTracking()
            .Where(w => w.Id == probeWaveId && w.OrganizationId == organizationId)
            .Join(dbContext.ModuleJobs.Where(j =>
                    j.Status == ExecutionStatus.Running && j.WaitingForApproval == true),
                w => w.OriginModuleJobId, j => j.Id, (w, _) => w.Id)
            .AnyAsync();
    }
}
