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

namespace SnapCd.Server.Host.Licensing.Services;

public class RemoteLicenseClient(
    IHttpClientFactory httpClientFactory,
    IOptions<LicenseSettings> settings,
    ILogger<RemoteLicenseClient> logger) : IRemoteLicenseClient
{
    public Task<SaaSLicenseResponse?> IssueAsync(string licenseKey, CancellationToken ct = default) =>
        PostAsync("issue", new { licenseKey }, ct);

    public Task<SaaSLicenseResponse?> RefreshAsync(string licenseKey, string? currentToken, CancellationToken ct = default) =>
        PostAsync("refresh", new { licenseKey, currentToken }, ct);

    private async Task<SaaSLicenseResponse?> PostAsync(string route, object body, CancellationToken ct)
    {
        var client = httpClientFactory.CreateClient(nameof(RemoteLicenseClient));
        client.Timeout = TimeSpan.FromSeconds(30);

        var baseUrl = settings.Value.LicenseServerBaseUrl.TrimEnd('/');
        var url = $"{baseUrl}/api/licenses/{route}";

        var response = await client.PostAsJsonAsync(url, body, ct);
        if (!response.IsSuccessStatusCode)
        {
            var content = await response.Content.ReadAsStringAsync(ct);
            logger.LogWarning("SaaS license {Route} returned {Status}: {Body}", route, response.StatusCode, content);
            return null;
        }

        return await response.Content.ReadFromJsonAsync<SaaSLicenseResponse>(cancellationToken: ct);
    }
}
