using System.Text.Json;
using System.Text.Json.Serialization;

namespace SnapCd.Runner.Services;

public class ServicePrincipalTokenService
{
    private readonly HttpClient _httpClient;

    public ServicePrincipalTokenService(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public async Task<TokenResponse> GetAccessTokenAsync(string authServerUrl, Guid organizationId, string clientId, string clientSecret, CancellationToken cancellationToken = default)
    {
        var tokenUrl = $"{authServerUrl}/connect/token";
        var prefixedClientId = $"{organizationId}:{clientId}";

        var content = new FormUrlEncodedContent(new[]
        {
            new KeyValuePair<string, string>("grant_type", "client_credentials"),
            new KeyValuePair<string, string>("scope", "snapcd_scope"),
            new KeyValuePair<string, string>("client_id", prefixedClientId),
            new KeyValuePair<string, string>("client_secret", clientSecret)
        });

        using var request = new HttpRequestMessage(HttpMethod.Post, tokenUrl);
        request.Content = content;
        request.Headers.Add("Accept", "application/json");

        using var response = await _httpClient.SendAsync(request, cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            var errorBody = await response.Content.ReadAsStringAsync(cancellationToken);
            throw new HttpRequestException($"Token request failed. Status: {response.StatusCode}, URL: {tokenUrl}, Response: {errorBody}");
        }

        var responseBody = await response.Content.ReadAsStringAsync(cancellationToken);
        return JsonSerializer.Deserialize<TokenResponse>(responseBody, new JsonSerializerOptions { PropertyNameCaseInsensitive = true })
               ?? throw new InvalidOperationException("Failed to deserialize token response.");
    }
}

public class TokenResponse
{
    [JsonPropertyName("access_token")] public string AccessToken { get; set; } = string.Empty;

    [JsonPropertyName("token_type")] public string TokenType { get; set; } = string.Empty;

    [JsonPropertyName("expires_in")] public int ExpiresIn { get; set; }
}