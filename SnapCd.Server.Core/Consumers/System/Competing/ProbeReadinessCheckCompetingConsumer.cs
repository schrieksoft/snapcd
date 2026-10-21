// SPDX-License-Identifier: LicenseRef-Snap-CD-Source-Available-1.1
// Copyright (c) 2026 Karl Schriek / Schrieksoft.
// No license is granted to use this file, in whole or in part, (a) as training, fine-tuning, retrieval, or
// embedding data for any machine-learning model, or (b) as input to any machine-learning model, agent, or automated
// system for the purpose of producing a derivative work or reimplementation that is not otherwise permitted by the
// Snap CD Source-Available License (including any Competing Product as defined therein). Contact info@snapcd.io
// for terms covering either use.

using MassTransit;
using Microsoft.EntityFrameworkCore;
using SnapCd.Server.Core.Database;
using SnapCd.Server.Core.Entities.Definition.ProbeWaves;
using SnapCd.Server.Core.Entities.Definition;
using SnapCd.Server.Core.Enums;
using SnapCd.Server.Core.Events.System;
using SnapCd.Server.Core.Services.Crud.Jobs;
using SnapCd.Server.Core.Services.RippleProbes;

namespace SnapCd.Server.Core.Consumers.System.Competing;

/// <summary>
/// Starts the Apply that carries a participant's probe, once everything it was waiting on has
/// answered.
///
/// Readiness is recomputed from the participant's contributions on every check rather than carried
/// on the message, so an extra check answers no and a check that arrives out of order still reads
/// the current picture. That is what makes the fan-out below safe: one participant reporting asks
/// every consumer of it to look again, and most of them will not be ready.
///
/// Competing rather than fanout: two servers answering the same check would each start an Apply.
/// </summary>
public class ProbeReadinessCheckCompetingConsumer : IConsumer<ProbeReadinessCheckRequested>
{
    private readonly ILogger<ProbeReadinessCheckCompetingConsumer> _logger;
    private readonly ProbeWaveProgressReaderFactory _progressFactory;
    private readonly IDbContextFactory<SnapCdDbContext> _dbContextFactory;
    private readonly JobService _jobService;
    private readonly SignalEdgeResolverFactory _resolverFactory;

    public ProbeReadinessCheckCompetingConsumer(
        ILogger<ProbeReadinessCheckCompetingConsumer> logger,
        ProbeWaveProgressReaderFactory progressFactory,
        IDbContextFactory<SnapCdDbContext> dbContextFactory,
        JobService jobService,
        SignalEdgeResolverFactory resolverFactory)
    {
        _logger = logger;
        _progressFactory = progressFactory;
        _dbContextFactory = dbContextFactory;
        _jobService = jobService;
        _resolverFactory = resolverFactory;
    }

    public async Task Consume(ConsumeContext<ProbeReadinessCheckRequested> context)
    {
        var probeWaveId = context.Message.ProbeWaveId;
        var moduleId = context.Message.ModuleId;
        var organizationId = context.Message.OrganizationId;

        using var progress = _progressFactory.Create();
        var module = await progress.GetModule(probeWaveId, moduleId, organizationId);

        if (module == null)
        {
            _logger.LogDebug(
                "Probe readiness check found no edge into Module {ModuleId} in wave {ProbeWaveId}",
                moduleId, probeWaveId);
            return;
        }

        if (!module.OwesAProbe)
        {
            _logger.LogDebug(
                "Module {ModuleId} is not ready to probe yet, leaving it to the next check", moduleId);
            return;
        }

        // An upstream that has not reported yet may still bring this Module in on a second edge,
        // and probing now would answer with one upstream's value while another was still coming.
        // A Module reachable by two paths of different lengths is where this bites: the shorter
        // one resolves first and would look like the whole answer.
        if (await AnUpstreamMayStillReport(probeWaveId, moduleId, organizationId))
        {
            _logger.LogDebug(
                "Module {ModuleId} reads an output from a Module that has not reported yet, so it waits",
                moduleId);
            return;
        }

        // A wave exists to inform one approval decision, so it ends when that decision is made.
        // A probe already running is left to finish; nothing new is started.
        if (!await OriginIsAwaitingApproval(probeWaveId, organizationId))
        {
            _logger.LogDebug(
                "Wave {ProbeWaveId} is no longer live, not starting a probe for Module {ModuleId}",
                probeWaveId, moduleId);
            return;
        }

        // A Module already running a job reads its contributions in that job's own probe step, so
        // a second Apply would race the first.
        if (await IsBusy(moduleId, organizationId))
        {
            _logger.LogDebug("Module {ModuleId} is already running a job, not starting a probe", moduleId);
            return;
        }

        // The job id is chosen here and recorded on the participant before the Apply starts, so
        // that the job knows it exists to answer a wave rather than to start one. A probe's Apply
        // is an ordinary Apply, and an ordinary Apply whose plan changes an output starts a wave.
        var jobId = Guid.NewGuid();

        try
        {
            if (!await ClaimProbeJob(probeWaveId, moduleId, organizationId, jobId))
            {
                _logger.LogDebug(
                    "Module {ModuleId} was claimed by another check, not starting a second Apply",
                    moduleId);
                return;
            }

            await _jobService.Apply(moduleId, organizationId, jobId);

            _logger.LogInformation(
                "Propagation: started an Apply to probe Module {ModuleId} in wave {ProbeWaveId}",
                moduleId, probeWaveId);
        }
        catch (Exception ex)
        {
            // A Module that cannot be applied will never answer, and leaving its consumers waiting
            // on it would stall the wave behind a Module that is paused, over quota or no longer
            // configured. Releasing them is what Unanswerable is for.
            _logger.LogWarning(ex,
                "Propagation: could not start an Apply for Module {ModuleId}, releasing what waited on it",
                moduleId);

            using var resolver = _resolverFactory.Create();
            await resolver.ResolveAsUnanswerable(
                probeWaveId, moduleId, organizationId, "its Apply could not be started");
        }
    }

    /// <summary>
    /// Whether some Module this one reads an output from is in the wave and has not answered yet.
    ///
    /// Read from the declared inputs rather than from the wave's edges, because the edge that
    /// would make this Module wait does not exist until the upstream reports. That is the cost of
    /// a wave that grows: its edges describe what has happened, so what might still happen has to
    /// come from the configuration.
    /// </summary>
    private async Task<bool> AnUpstreamMayStillReport(Guid probeWaveId, Guid moduleId, Guid organizationId)
    {
        await using var dbContext = await _dbContextFactory.CreateDbContextAsync();

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

        // One of them has yet to report, so a second edge into this Module may still arrive.
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
    /// Records this job as the Module's probe for this wave, and reports whether the claim was
    /// ours. The unique index on (wave, Module) decides it: two checks racing the same Module both
    /// insert, and only one row survives.
    /// </summary>
    private async Task<bool> ClaimProbeJob(
        Guid probeWaveId,
        Guid moduleId,
        Guid organizationId,
        Guid jobId)
    {
        await using var dbContext = await _dbContextFactory.CreateDbContextAsync();

        dbContext.ModuleProbeAttempts.Add(new ModuleProbeAttempt
        {
            Id = Guid.NewGuid(),
            OrganizationId = organizationId,
            ProbeWaveId = probeWaveId,
            ModuleId = moduleId,
            ProbeModuleJobId = jobId
        });

        try
        {
            await dbContext.SaveChangesAsync();
            return true;
        }
        catch (DbUpdateException)
        {
            // Somebody else is already probing this Module for this wave. Theirs stands.
            return false;
        }
    }

    /// <summary>
    /// Whether the origin's approval decision is still open. A job past it has the answer it was
    /// going to act on, and one that is no longer running cannot act on anything.
    /// </summary>
    private async Task<bool> OriginIsAwaitingApproval(Guid probeWaveId, Guid organizationId)
    {
        await using var dbContext = await _dbContextFactory.CreateDbContextAsync();

        return await dbContext.ProbeWaves
            .AsNoTracking()
            .Where(w => w.Id == probeWaveId && w.OrganizationId == organizationId)
            .Join(dbContext.ModuleJobs.Where(j =>
                    j.Status == ExecutionStatus.Running && j.WaitingForApproval == true),
                w => w.OriginModuleJobId,
                j => j.Id,
                (w, _) => w.Id)
            .AnyAsync();
    }

    private async Task<bool> IsBusy(Guid moduleId, Guid organizationId)
    {
        await using var dbContext = await _dbContextFactory.CreateDbContextAsync();

        return await dbContext.ModuleJobs
            .AsNoTracking()
            .AnyAsync(j =>
                j.ModuleId == moduleId &&
                j.OrganizationId == organizationId &&
                j.Status == ExecutionStatus.Running);
    }
}
