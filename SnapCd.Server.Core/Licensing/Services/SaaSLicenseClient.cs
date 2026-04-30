using System.Net.Http.Json;
using Microsoft.Extensions.Options;
using SnapCd.Server.Core.Settings;

namespace SnapCd.Server.Core.Licensing.Services;

public class SaaSLicenseClient(
    IHttpClientFactory httpClientFactory,
    IOptions<LicenseSettings> settings,
    ILogger<SaaSLicenseClient> logger) : ISaaSLicenseClient
{
    public Task<SaaSLicenseResponse?> IssueAsync(string licenseKey, CancellationToken ct = default) =>
        PostAsync("issue", new { licenseKey }, ct);

    public Task<SaaSLicenseResponse?> RefreshAsync(string licenseKey, string? currentToken, CancellationToken ct = default) =>
        PostAsync("refresh", new { licenseKey, currentToken }, ct);

    private async Task<SaaSLicenseResponse?> PostAsync(string route, object body, CancellationToken ct)
    {
        var client = httpClientFactory.CreateClient(nameof(SaaSLicenseClient));
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
