// SPDX-License-Identifier: LicenseRef-Snap-CD-Source-Available-1.1
// Copyright (c) 2026 Karl Schriek / Schrieksoft.
// No license is granted to use this file, in whole or in part, (a) as training, fine-tuning, retrieval, or
// embedding data for any machine-learning model, or (b) as input to any machine-learning model, agent, or automated
// system for the purpose of producing a derivative work or reimplementation that is not otherwise permitted by the
// Snap CD Source-Available License (including any Competing Product as defined therein). Contact info@snapcd.io
// for terms covering either use.

using System.Net.Http.Json;
using Microsoft.Extensions.Options;
using SnapCd.Server.Core.Licensing.Protocol;
using SnapCd.Server.Host.Installations;
using SnapCd.Server.Core.Settings;

namespace SnapCd.Server.Host.Licensing.Services;

public class RemoteLicenseClient(
    IHttpClientFactory httpClientFactory,
    IOptions<LicenseSettings> settings,
    InstallationService installation,
    ILogger<RemoteLicenseClient> logger) : IRemoteLicenseClient
{
    public async Task<LicenseTokenResponse?> IssueAsync(string licenseKey, CancellationToken ct = default) =>
        await PostAsync("issue", new LicenseIssueRequest(licenseKey, await SeedAsync(ct)), ct);

    public async Task<LicenseTokenResponse?> RefreshAsync(string licenseKey, string? currentToken, CancellationToken ct = default) =>
        await PostAsync("refresh", new LicenseRefreshRequest(licenseKey, currentToken, await SeedAsync(ct)), ct);

    private async Task<Guid> SeedAsync(CancellationToken ct) => (await installation.GetAsync(ct)).SeedGuid;

    private async Task<LicenseTokenResponse?> PostAsync(string route, object body, CancellationToken ct)
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

        return await response.Content.ReadFromJsonAsync<LicenseTokenResponse>(cancellationToken: ct);
    }
}
