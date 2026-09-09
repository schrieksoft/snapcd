// SPDX-License-Identifier: LicenseRef-Snap-CD-Source-Available-1.1
// Copyright (c) 2026 Karl Schriek / Schrieksoft.
// No license is granted to use this file, in whole or in part, (a) as training, fine-tuning, retrieval, or
// embedding data for any machine-learning model, or (b) as input to any machine-learning model, agent, or automated
// system for the purpose of producing a derivative work or reimplementation that is not otherwise permitted by the
// Snap CD Source-Available License (including any Competing Product as defined therein). Contact info@snapcd.io
// for terms covering either use.


using System.Net.Http.Json;
using Microsoft.Extensions.Options;
using SnapCd.Server.Core.Settings;
using SnapCd.Server.Core.Telemetry;

namespace SnapCd.Server.Host.Telemetry;

/// <summary>Posts one beacon to the licensing service. Any failure is a null result at Debug; the beacon must never matter to the server.</summary>
public class TelemetryClient(
    IHttpClientFactory httpClientFactory,
    IOptions<LicenseSettings> licenseSettings,
    ILogger<TelemetryClient> logger)
{
    public async Task<TelemetryReportResponse?> SendAsync(TelemetryReportRequest report, CancellationToken ct = default)
    {
        var url = licenseSettings.Value.LicenseServerBaseUrl.TrimEnd('/') + "/api/telemetry";
        try
        {
            var client = httpClientFactory.CreateClient(nameof(TelemetryClient));
            client.Timeout = TimeSpan.FromSeconds(10);
            using var response = await client.PostAsJsonAsync(url, report, ct);
            if (!response.IsSuccessStatusCode)
            {
                logger.LogDebug("Telemetry beacon to {Url} returned {Status}", url, response.StatusCode);
                return null;
            }
            return await response.Content.ReadFromJsonAsync<TelemetryReportResponse>(cancellationToken: ct);
        }
        catch (Exception ex)
        {
            logger.LogDebug(ex, "Telemetry beacon to {Url} failed", url);
            return null;
        }
    }
}
