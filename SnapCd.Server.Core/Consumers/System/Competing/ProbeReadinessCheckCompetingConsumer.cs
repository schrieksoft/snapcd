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
    private readonly ProbeContributionResolverFactory _resolverFactory;

    public ProbeReadinessCheckCompetingConsumer(
        ILogger<ProbeReadinessCheckCompetingConsumer> logger,
        ProbeWaveProgressReaderFactory progressFactory,
        IDbContextFactory<SnapCdDbContext> dbContextFactory,
        JobService jobService,
        ProbeContributionResolverFactory resolverFactory)
    {
        _logger = logger;
        _progressFactory = progressFactory;
        _dbContextFactory = dbContextFactory;
        _jobService = jobService;
        _resolverFactory = resolverFactory;
    }

    public async Task Consume(ConsumeContext<ProbeReadinessCheckRequested> context)
    {
        var participantId = context.Message.ProbeWaveParticipantId;
        var moduleId = context.Message.ModuleId;
        var organizationId = context.Message.OrganizationId;

        using var progress = _progressFactory.Create();
        var participant = await progress.GetParticipant(participantId, organizationId);

        if (participant == null)
        {
            _logger.LogDebug("Probe readiness check for participant {ParticipantId} found no such row", participantId);
            return;
        }

        if (!participant.OwesAProbe)
        {
            _logger.LogDebug(
                "Module {ModuleId} is not ready to probe yet, leaving it to the next check", moduleId);
            return;
        }

        // A wave whose origin job has stopped has nobody waiting on the answer: it was cancelled,
        // refused, or has already been approved and applied. Starting fresh Applies for it would
        // be a side effect nobody asked for. Consumers already running are left to finish, since
        // they are ordinary applies doing real work.
        if (!await OriginIsStillRunning(participant.ProbeWaveId, organizationId))
        {
            _logger.LogDebug(
                "Wave {ProbeWaveId} is no longer live, not starting a probe for Module {ModuleId}",
                participant.ProbeWaveId, moduleId);
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
            if (!await ClaimProbeJob(participantId, organizationId, jobId))
            {
                _logger.LogDebug(
                    "Participant {ParticipantId} was claimed by another check, not starting a second Apply",
                    participantId);
                return;
            }

            await _jobService.Apply(moduleId, organizationId, jobId);

            _logger.LogInformation(
                "Propagation: started an Apply to probe Module {ModuleId} in wave {ProbeWaveId}",
                moduleId, participant.ProbeWaveId);
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
            await resolver.ResolveAsUnanswerable(participantId, organizationId, "its Apply could not be started");
        }
    }

    /// <summary>
    /// Records this job as the participant's probe, and reports whether the claim was ours. The
    /// update is conditional on the slot still being empty, so two checks racing the same
    /// participant cannot both start an Apply.
    /// </summary>
    private async Task<bool> ClaimProbeJob(Guid participantId, Guid organizationId, Guid jobId)
    {
        await using var dbContext = await _dbContextFactory.CreateDbContextAsync();

        var claimed = await dbContext.ProbeWaveParticipants
            .Where(p =>
                p.Id == participantId &&
                p.OrganizationId == organizationId &&
                p.LastProbeModuleJobId == null)
            .ExecuteUpdateAsync(set => set.SetProperty(p => p.LastProbeModuleJobId, jobId));

        return claimed == 1;
    }

    private async Task<bool> OriginIsStillRunning(Guid probeWaveId, Guid organizationId)
    {
        await using var dbContext = await _dbContextFactory.CreateDbContextAsync();

        return await dbContext.ProbeWaves
            .AsNoTracking()
            .Where(w => w.Id == probeWaveId && w.OrganizationId == organizationId)
            .Join(dbContext.ModuleJobs.Where(j => j.Status == ExecutionStatus.Running),
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
