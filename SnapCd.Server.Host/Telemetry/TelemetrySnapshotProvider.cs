// SPDX-License-Identifier: LicenseRef-Snap-CD-Source-Available-1.1
// Copyright (c) 2026 Karl Schriek / Schrieksoft.
// No license is granted to use this file, in whole or in part, (a) as training, fine-tuning, retrieval, or
// embedding data for any machine-learning model, or (b) as input to any machine-learning model, agent, or automated
// system for the purpose of producing a derivative work or reimplementation that is not otherwise permitted by the
// Snap CD Source-Available License (including any Competing Product as defined therein). Contact info@snapcd.io
// for terms covering either use.


using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using SnapCd.Server.Core.Database;
using SnapCd.Server.Core.Services;
using SnapCd.Server.Core.Telemetry;
using SnapCd.Server.Host.Installations;

namespace SnapCd.Server.Host.Telemetry;

/// <summary>Builds the beacon from the database. Counts and version only; nothing here names anything.</summary>
public class TelemetrySnapshotProvider(
    IDbContextFactory<SnapCdDbContext> dbContextFactory,
    InstallationService installation,
    IVersionService version)
{
    public async Task<TelemetryReportRequest> BuildAsync(CancellationToken ct = default)
    {
        var row = await installation.GetAsync(ct);
        await using var db = await dbContextFactory.CreateDbContextAsync(ct);
        var moduleCount = await db.Modules.CountAsync(ct);
        var jobsTotal = await db.ModuleJobs.LongCountAsync(ct);

        return new TelemetryReportRequest(row.SeedGuid, version.ShortVersion, moduleCount, jobsTotal);
    }

    public static List<WhatsNewEntryDto> ReadStored(string? json)
    {
        if (string.IsNullOrWhiteSpace(json)) return [];
        try
        {
            return JsonSerializer.Deserialize<List<WhatsNewEntryDto>>(json) ?? [];
        }
        catch (JsonException)
        {
            return [];
        }
    }
}
