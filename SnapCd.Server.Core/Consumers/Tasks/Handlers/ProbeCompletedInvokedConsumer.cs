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
using SnapCd.Server.Core.Events.Handlers;
using SnapCd.Server.Core.Events.Steps;
using SnapCd.Server.Core.Events.Steps.Base;
using SnapCd.Server.Core.Services.RippleProbes;

namespace SnapCd.Server.Core.Consumers.Tasks.Handlers;

/// <summary>
/// Stores what a probe reported, then answers the saga.
///
/// The apply has already succeeded by the time a probe runs, so a failure to record cannot make
/// the job wrong: the saga is told the step completed either way and carries on to Output. That is
/// the opposite of the outputs read, where a storage failure does fault the job.
/// </summary>
public abstract class ProbeCompletedInvokedConsumer<TInvoked, TCompleted> : IConsumer<TInvoked>
    where TInvoked : ProbeCompletedInvokedBase
    where TCompleted : StepResponseBase, new()
{
    private readonly ILogger _logger;
    private readonly SnapCdDbContext _dbContext;
    private readonly ProbeSignalRecorder _recorder;
    private readonly SignalEdgeResolver _resolver;
    private readonly IBus _bus;

    protected ProbeCompletedInvokedConsumer(
        ILogger logger,
        SnapCdDbContext dbContext,
        ProbeSignalRecorder recorder,
        SignalEdgeResolver resolver,
        IBus bus)
    {
        _logger = logger;
        _dbContext = dbContext;
        _recorder = recorder;
        _resolver = resolver;
        _bus = bus;
    }

    public async Task Consume(ConsumeContext<TInvoked> context)
    {
        var jobId = context.Message.JobId;

        try
        {
            if (string.IsNullOrWhiteSpace(context.Message.SignalsJson))
            {
                _logger.LogDebug("Probe for job {JobId} reported no signals", jobId);
            }
            else
            {
                var jobView = await _dbContext.ModuleJobs
                    .Where(x => x.Id == jobId)
                    .Select(x => new { x.OrganizationId, x.ModuleId })
                    .FirstOrDefaultAsync();

                if (jobView == null)
                {
                    _logger.LogWarning("Could not find module job {JobId} to store probe signals", jobId);
                }
                else
                {
                    // The wave the saga recorded when it dispatched this probe. A job probes once
                    // per wave it owes, so its attempt rows alone cannot say which one replied.
                    var probeWaveId = await _dbContext.ApplyJobSagas
                        .AsNoTracking()
                        .Where(s => s.CorrelationId == jobId)
                        .Select(s => s.ProbingForWaveId)
                        .FirstOrDefaultAsync();

                    if (probeWaveId == null)
                    {
                        _logger.LogWarning(
                            "Probe for job {JobId} reported signals with no wave claiming it", jobId);
                    }
                    else
                    {
                        await _recorder.Record(
                            probeWaveId.Value,
                            jobView.ModuleId,
                            jobView.OrganizationId,
                            jobId,
                            context.Message.SignalsJson!);

                        _logger.LogDebug("Stored probe signals for job {JobId}", jobId);

                        // Whoever waited on this Module now has an answer, and a consumer whose
                        // last edge this was is released by it.
                        await _resolver.ResolveAfterSignals(
                            probeWaveId.Value, jobView.ModuleId, jobView.OrganizationId);
                    }
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to store probe signals for job {JobId}", jobId);
        }

        await _bus.Publish(new TCompleted { CorrelationId = jobId });

        _logger.LogDebug("Probe completion processed for job {JobId}", jobId);
    }
}

public class ApplyProbeCompletedInvokedConsumer(
    ILogger<ApplyProbeCompletedInvokedConsumer> logger,
    SnapCdDbContext dbContext,
    ProbeSignalRecorder recorder,
    SignalEdgeResolver resolver,
    IBus bus)
    : ProbeCompletedInvokedConsumer<ApplyProbeCompletedInvoked, ApplyProbeCompleted>(
        logger, dbContext, recorder, resolver, bus);
