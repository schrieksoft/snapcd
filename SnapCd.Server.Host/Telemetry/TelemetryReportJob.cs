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

/// <summary>The daily beacon. Sends the snapshot and stores what came back on the installation row.</summary>
public class TelemetryReportJob(
    TelemetrySnapshotProvider snapshots,
    TelemetryClient client,
    InstallationService installation,
    IOptions<TelemetrySettings> settings,
    ILogger<TelemetryReportJob> logger)
{
    public const int StoredEntries = 10;

    public async Task ExecuteJob()
    {
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
            if (response.LatestVersion is not null)
            {
                row.LatestKnownVersion = response.LatestVersion;
                row.LatestKnownVersionCheckedAtUtc = now;
            }
            if (response.WhatsNew.Count > 0)
            {
                var merged = TelemetrySnapshotProvider.ReadStored(row.WhatsNewJson)
                    .Where(stored => response.WhatsNew.All(fresh => fresh.Id != stored.Id))
                    .Concat(response.WhatsNew)
                    .OrderByDescending(e => e.PublishedUtc)
                    .Take(StoredEntries)
                    .ToList();
                row.WhatsNewJson = JsonSerializer.Serialize(merged);
            }
        });
        logger.LogDebug("Telemetry beacon sent; latest version {Latest}, {Count} new entries", response.LatestVersion, response.WhatsNew.Count);
    }
}
