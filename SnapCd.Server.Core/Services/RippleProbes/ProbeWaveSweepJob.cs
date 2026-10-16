// SPDX-License-Identifier: LicenseRef-Snap-CD-Source-Available-1.1
// Copyright (c) 2026 Karl Schriek / Schrieksoft.
// No license is granted to use this file, in whole or in part, (a) as training, fine-tuning, retrieval, or
// embedding data for any machine-learning model, or (b) as input to any machine-learning model, agent, or automated
// system for the purpose of producing a derivative work or reimplementation that is not otherwise permitted by the
// Snap CD Source-Available License (including any Competing Product as defined therein). Contact info@snapcd.io
// for terms covering either use.

using Microsoft.Extensions.Logging;
using SnapCd.Server.Core.Services.Crud.Jobs;

namespace SnapCd.Server.Core.Services.RippleProbes;

/// <summary>
/// Starts an Apply for each participant a wave is waiting on. The Apply carries the probe step, so
/// the probe runs as part of an ordinary job the system already knows how to run, approve, cancel
/// and show.
/// </summary>
public class ProbeWaveSweepJob
{
    private readonly ProbeWaveSweepService _sweepService;
    private readonly JobService _jobService;
    private readonly ProbeContributionResolverFactory _resolverFactory;
    private readonly ILogger<ProbeWaveSweepJob> _logger;

    public ProbeWaveSweepJob(
        ProbeWaveSweepService sweepService,
        JobService jobService,
        ProbeContributionResolverFactory resolverFactory,
        ILogger<ProbeWaveSweepJob> logger)
    {
        _sweepService = sweepService;
        _jobService = jobService;
        _resolverFactory = resolverFactory;
        _logger = logger;
    }

    public async Task Run()
    {
        var released = await _sweepService.ListReleased();
        if (released.Count == 0) return;

        _logger.LogInformation("Propagation: {Count} Module(s) ready to probe", released.Count);

        foreach (var participant in released)
            try
            {
                await _jobService.Apply(participant.ModuleId, participant.OrganizationId);

                _logger.LogDebug(
                    "Propagation: started an Apply for Module {ModuleId} in wave {ProbeWaveId}",
                    participant.ModuleId, participant.ProbeWaveId);
            }
            catch (Exception ex)
            {
                // A Module that cannot be applied will never answer, and leaving its consumers
                // waiting on it would stall the wave behind a Module that is paused, over quota or
                // no longer configured. Releasing them is what Unanswerable is for.
                _logger.LogWarning(ex,
                    "Propagation: could not start an Apply for Module {ModuleId}, releasing what waited on it",
                    participant.ModuleId);

                using var resolver = _resolverFactory.Create();
                await resolver.ResolveAsUnanswerable(
                    participant.ParticipantId,
                    participant.OrganizationId,
                    "its Apply could not be started");
            }
    }
}
