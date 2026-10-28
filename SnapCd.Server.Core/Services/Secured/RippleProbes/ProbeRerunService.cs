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
using SnapCd.Contracts;
using SnapCd.Server.Core.Database;
using SnapCd.Server.Core.Entities.Definition.ProbeWaves;
using SnapCd.Server.Core.Enums;
using SnapCd.Server.Core.Events.Gatekeeping;
using SnapCd.Server.Core.Repositories.Organizations.Secured;
using SnapCd.Server.Core.Services.PrincipalProvider;

namespace SnapCd.Server.Core.Services.RippleProbes;

public class ProbeRerunServiceFactory(
    IDbContextFactory<SnapCdDbContext> dbContextFactory,
    ProbeWaveStarterFactory starterFactory,
    ModuleJobSecuredRepositoryFactory jobsFactory,
    IPublishEndpoint bus,
    ILoggerFactory loggerFactory)
{
    public ProbeRerunService Create(IPrincipalProvider? principalProvider = null) =>
        new(dbContextFactory.CreateDbContext(), starterFactory,
            jobsFactory.Create(principalProvider), bus,
            loggerFactory.CreateLogger<ProbeRerunService>());
}

/// <summary>
/// Asks a Module for its prediction again.
///
/// A probe answers for the moment it ran: the code, the declaration and the input values it
/// resolved. Any of those can move afterwards, and the answer stops holding without anything
/// saying so. The approver is the one who decides whether the difference matters, so re-running is
/// something they ask for rather than something that happens to them.
///
/// Every entry point checks permission itself. CanRetry and CanRerun answer the same question
/// without acting, for a caller that has to decide whether to offer the action at all.
/// </summary>
public class ProbeRerunService(
    SnapCdDbContext dbContext,
    ProbeWaveStarterFactory starterFactory,
    ModuleJobSecuredRepository jobs,
    IPublishEndpoint bus,
    ILogger<ProbeRerunService> logger) : IDisposable
{
    /// <summary>
    /// Whether a retry may be asked for. The permission hangs off the Module the origin job ran
    /// against, which this resolves; a job that is gone answers false. RetryPropagation checks
    /// again when asked to act, so a caller uses this to decide what to offer.
    /// </summary>
    public async Task<bool> CanRetry(Guid originModuleJobId, Guid organizationId)
    {
        var originModuleId = await dbContext.ModuleJobs
            .AsNoTracking()
            .Where(j => j.Id == originModuleJobId && j.OrganizationId == organizationId)
            .Select(j => (Guid?)j.ModuleId)
            .FirstOrDefaultAsync();

        return originModuleId is { } id && CanRerun(id, organizationId);
    }

    /// <summary>
    /// Whether another prediction may be asked for. Both asks start an Apply on the Module the
    /// wave came from, so they take the same permission as running one directly. Reading the
    /// panel is a weaker bar than running what it offers.
    /// </summary>
    public bool CanRerun(Guid originModuleId, Guid organizationId) =>
        jobs.CanRunJob(originModuleId, organizationId);

    /// <summary>
    /// Starts the propagation a job's plan should have started. The plan is already recorded, so
    /// what runs again is the asking, from what that plan reported.
    /// </summary>
    public async Task<string?> RetryPropagation(Guid originModuleJobId, Guid organizationId)
    {
        var job = await dbContext.ModuleJobs
            .AsNoTracking()
            .Where(j => j.Id == originModuleJobId && j.OrganizationId == organizationId)
            .Select(j => new
            {
                j.ModuleId,
                j.Status,
                j.WaitingForApproval,
                j.OutputsModifyList,
                j.OutputsDestroyList,
                j.OutputsRecreateList,
                j.OutputSignalsJson
            })
            .FirstOrDefaultAsync();

        if (job == null) return "that job no longer exists";

        if (!CanRerun(job.ModuleId, organizationId))
            return "you are not allowed to run jobs on this Module";

        if (job.Status != ExecutionStatus.Running || job.WaitingForApproval != true)
            return "the approval this would inform has already been decided";

        if (await dbContext.ProbeWaves.AnyAsync(w =>
                w.OriginModuleJobId == originModuleJobId && w.OrganizationId == organizationId))
            return "a propagation has already started from this job";

        // The same set the plan produced: what a consumer already reads can move, a created output
        // has no value anyone has consumed yet.
        var changed = Split(job.OutputsModifyList)
            .Concat(Split(job.OutputsDestroyList))
            .Concat(Split(job.OutputsRecreateList))
            .Distinct()
            .ToList();

        if (changed.Count == 0) return "this plan changed no output that anything reads";

        using var starter = starterFactory.Create();

        var waveId = await starter.Start(
            job.ModuleId, organizationId, originModuleJobId, changed, job.OutputSignalsJson);

        if (waveId == null) return "nothing downstream reads what this plan changed";

        await ClearFailure(originModuleJobId, organizationId);

        logger.LogInformation(
            "Propagation asked for again on job {ModuleJobId}", originModuleJobId);

        return null;
    }

    private async Task ClearFailure(Guid originModuleJobId, Guid organizationId)
    {
        var job = await dbContext.ModuleJobs
            .FirstOrDefaultAsync(j => j.Id == originModuleJobId && j.OrganizationId == organizationId);

        if (job == null) return;

        job.PropagationError = null;
        await dbContext.SaveChangesAsync();
    }

    private static IEnumerable<string> Split(string? list) =>
        string.IsNullOrWhiteSpace(list)
            ? []
            : list.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

    /// <summary>
    /// Why a re-probe was not asked for, or null where it was.
    /// </summary>
    public async Task<string?> Rerun(Guid probeWaveId, Guid moduleId, Guid organizationId)
    {
        var wave = await dbContext.ProbeWaves
            .AsNoTracking()
            .Where(w => w.Id == probeWaveId && w.OrganizationId == organizationId)
            .Select(w => new { w.OriginModuleJobId, OriginModuleId = w.OriginModuleJob.ModuleId })
            .FirstOrDefaultAsync();

        if (wave == null) return "that propagation no longer exists";

        if (!CanRerun(wave.OriginModuleId, organizationId))
            return "you are not allowed to run jobs on this Module";

        // A wave informs one approval decision and ends when that decision is made. Asking a
        // Module to predict again for a decision already taken would answer nothing.
        var stillOpen = await dbContext.ModuleJobs
            .AsNoTracking()
            .AnyAsync(j => j.Id == wave.OriginModuleJobId &&
                           j.OrganizationId == organizationId &&
                           j.Status == ExecutionStatus.Running &&
                           j.WaitingForApproval == true);

        if (!stillOpen) return "the approval this propagation informs has already been decided";

        var owesAnAnswer = await dbContext.SignalEdges
            .AsNoTracking()
            .AnyAsync(e => e.ProbeWaveId == probeWaveId &&
                           e.ConsumerModuleId == moduleId &&
                           e.OrganizationId == organizationId);

        if (!owesAnAnswer) return "that Module is not part of this propagation";

        logger.LogInformation(
            "Re-probe asked for on Module {ModuleId} in wave {ProbeWaveId}", moduleId, probeWaveId);

        // The attempt that owes the new answer, with no result and no job yet. A Module's latest
        // attempt in a wave is what decides whether it still owes one, so this is what reopens
        // the question the earlier attempt closed. The probe's dispatch claims this row.
        dbContext.ModuleProbeAttempts.Add(new ModuleProbeAttempt
        {
            Id = Guid.NewGuid(),
            OrganizationId = organizationId,
            ProbeWaveId = probeWaveId,
            ModuleId = moduleId,
            Timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()
        });

        await dbContext.SaveChangesAsync();

        // The same ask the wave makes of a Module it reaches: the gate decides whether it can run
        // now, and queues it against its dependencies where it cannot.
        await bus.Publish(new GatekeepingJobRequested
        {
            ModuleId = moduleId,
            OrganizationId = organizationId,
            DesiredStateHeadline = DesiredStateHeadline.Applied,
            SetNewDesiredState = false
        });

        return null;
    }

    public void Dispose()
    {
        jobs.Dispose();
        dbContext.Dispose();
    }
}
