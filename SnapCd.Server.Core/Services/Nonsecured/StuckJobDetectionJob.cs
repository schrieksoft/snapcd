// SPDX-License-Identifier: LicenseRef-Snap-CD-Source-Available-1.1
// Copyright (c) 2026 Karl Schriek / Schrieksoft.
// No license is granted to use this file, in whole or in part, (a) as training, fine-tuning, retrieval, or
// embedding data for any machine-learning model, or (b) as input to any machine-learning model, agent, or automated
// system for the purpose of producing a derivative work or reimplementation that is not otherwise permitted by the
// Snap CD Source-Available License (including any Competing Product as defined therein). Contact info@snapcd.io
// for terms covering either use.

namespace SnapCd.Server.Core.Services;

public class StuckJobDetectionJob
{
    private readonly StuckJobDetectionService _detectionService;
    private readonly ILogger<StuckJobDetectionJob> _logger;

    public StuckJobDetectionJob(StuckJobDetectionService detectionService, ILogger<StuckJobDetectionJob> logger)
    {
        _detectionService = detectionService;
        _logger = logger;
    }

    public async Task ExecuteJob()
    {
        using var _ = SnapCd.Server.Core.Services.CallerContext.CallerContext.Begin(SnapCd.Server.Core.Services.CallerContext.CallerKind.System);

        var stuck = await _detectionService.FindStuckJobsAsync();
        foreach (var job in stuck)
            _logger.LogWarning(
                "Job {JobId} ({JobType}) has been stuck in {State} for {Stalled} (waiting since {WaitingSince:O})",
                job.JobId, job.JobType, job.State, job.Stalled, job.WaitingSince);

        if (stuck.Count > 0)
            _logger.LogWarning("Stuck job detection: {Count} job(s) need attention", stuck.Count);
    }
}
