// SPDX-License-Identifier: LicenseRef-Snap-CD-Source-Available-1.1
// Copyright (c) 2026 Karl Schriek / Schrieksoft.
// No license is granted to use this file, in whole or in part, (a) as training, fine-tuning, retrieval, or
// embedding data for any machine-learning model, or (b) as input to any machine-learning model, agent, or automated
// system for the purpose of producing a derivative work or reimplementation that is not otherwise permitted by the
// Snap CD Source-Available License (including any Competing Product as defined therein). Contact info@snapcd.io
// for terms covering either use.


using System.Text.Json;
using Microsoft.Extensions.Options;
using SnapCd.Server.Host.Installations;
using SnapCd.Server.Core.Telemetry;

namespace SnapCd.Server.Host.Telemetry;

/// <summary>The daily beacon. Sends the snapshot and stores the notices that came back, replacing what was there.</summary>
public class TelemetryReportJob(
    TelemetrySnapshotProvider snapshots,
    TelemetryClient client,
    GitHubReleasesClient releases,
    InstallationService installation,
    IOptions<TelemetrySettings> settings,
    ILogger<TelemetryReportJob> logger)
{
    public async Task ExecuteJob()
    {
        // The release check is independent of the beacon; it stores the latest known version as a side effect.
        await releases.NewerThanRunningAsync();

        if (!settings.Value.Enabled)
        {
            logger.LogDebug("Telemetry is disabled; beacon skipped");
            return;
        }

        var report = await snapshots.BuildAsync();
        var response = await client.SendAsync(report);
        if (response is null) return;

        var now = DateTime.UtcNow;
        await installation.UpdateAsync(row =>
        {
            row.LastTelemetryReportAtUtc = now;
            row.WhatsNewJson = JsonSerializer.Serialize(response.WhatsNew.OrderByDescending(e => e.PublishedUtc).ToList());
        });
        logger.LogDebug("Telemetry beacon sent; {Count} notices", response.WhatsNew.Count);
    }
}
