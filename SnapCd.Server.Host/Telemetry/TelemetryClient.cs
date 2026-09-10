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

/// <summary>Posts one beacon to the licensing service. Transient failures are retried in-process with a short backoff, three attempts in all; anything else is a null result at Debug. The beacon must never matter to the server.</summary>
public class TelemetryClient(
    IHttpClientFactory httpClientFactory,
    IOptions<LicenseSettings> licenseSettings,
    ILogger<TelemetryClient> logger)
{
    /// <summary>Waits before the second and third attempt.</summary>
    public static readonly TimeSpan[] DefaultBackoff = [TimeSpan.FromSeconds(10), TimeSpan.FromMinutes(1)];

    public TimeSpan[] Backoff { get; init; } = DefaultBackoff;

    public async Task<TelemetryReportResponse?> SendAsync(TelemetryReportRequest report, CancellationToken ct = default)
    {
        var url = licenseSettings.Value.LicenseServerBaseUrl.TrimEnd('/') + "/api/telemetry";
        for (var attempt = 0; ; attempt++)
        {
            var (response, transient) = await TryOnceAsync(url, report, ct);
            if (response is not null) return response;
            if (!transient || attempt >= Backoff.Length) return null;

            logger.LogDebug("Telemetry beacon attempt {Attempt} failed; retrying in {Delay}", attempt + 1, Backoff[attempt]);
            await Task.Delay(Backoff[attempt], ct);
        }
    }

    private async Task<(TelemetryReportResponse? Response, bool Transient)> TryOnceAsync(string url, TelemetryReportRequest report, CancellationToken ct)
    {
        try
        {
            var client = httpClientFactory.CreateClient(nameof(TelemetryClient));
            client.Timeout = TimeSpan.FromSeconds(10);
            using var response = await client.PostAsJsonAsync(url, report, ct);
            if (response.IsSuccessStatusCode)
            {
                return (await response.Content.ReadFromJsonAsync<TelemetryReportResponse>(cancellationToken: ct), false);
            }

            var status = (int)response.StatusCode;
            // Server-side trouble and throttling may clear; a rejected payload will not.
            var transient = status >= 500 || status is 408 or 429;
            logger.LogDebug("Telemetry beacon to {Url} returned {Status}", url, response.StatusCode);
            return (null, transient);
        }
        catch (OperationCanceledException) when (ct.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception ex)
        {
            logger.LogDebug(ex, "Telemetry beacon to {Url} failed", url);
            return (null, true);
        }
    }
}
