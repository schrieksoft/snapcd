// SPDX-License-Identifier: LicenseRef-Snap-CD-Source-Available-1.1
// Copyright (c) 2026 Karl Schriek / Schrieksoft.
// No license is granted to use this file, in whole or in part, (a) as training, fine-tuning, retrieval, or
// embedding data for any machine-learning model, or (b) as input to any machine-learning model, agent, or automated
// system for the purpose of producing a derivative work or reimplementation that is not otherwise permitted by the
// Snap CD Source-Available License (including any Competing Product as defined therein). Contact info@snapcd.io
// for terms covering either use.

using System.Text.Json;
using System.Text.Json.Serialization;

using SnapCd.Runner.Utils;

namespace SnapCd.Runner.Services;

public class ServicePrincipalTokenService
{
    private readonly HttpClient _httpClient;

    public ServicePrincipalTokenService(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public Task<TokenResponse> GetAccessTokenAsync(string authServerUrl, Guid organizationId, string clientId, string clientSecret, CancellationToken cancellationToken = default)
        => RequestTokenAsync(authServerUrl, organizationId, clientId,
            new KeyValuePair<string, string>("client_secret", clientSecret), cancellationToken);

    /// <summary>
    /// Exchanges a token issued by the caller's own platform for a Snap CD access token. The
    /// platform rotates the file, so it is read on every call.
    /// </summary>
    public async Task<TokenResponse> GetAccessTokenWithAssertionAsync(string authServerUrl, Guid organizationId, string clientId, string assertionTokenPath, CancellationToken cancellationToken = default)
    {
        var path = PathUtils.ExpandTilde(assertionTokenPath);

        if (!File.Exists(path))
            throw new FileNotFoundException($"Federated credential token file not found at {path}.", path);

        var assertion = (await File.ReadAllTextAsync(path, cancellationToken)).Trim();

        if (string.IsNullOrEmpty(assertion))
            throw new InvalidOperationException($"Federated credential token file {path} is empty.");

        return await RequestTokenAsync(authServerUrl, organizationId, clientId,
            new KeyValuePair<string, string>(SnapCdFederatedAssertionParameter, assertion),
            cancellationToken,
            new KeyValuePair<string, string>(SnapCdFederatedAssertionTypeParameter, JwtBearerClientAssertionType));
    }

    // Not the standard client_assertion: that name is handled by OpenIddict's private_key_jwt
    // support on the Server, which expects the client itself to have signed the assertion.
    private const string SnapCdFederatedAssertionParameter = "snapcd_federated_assertion";
    private const string SnapCdFederatedAssertionTypeParameter = "snapcd_federated_assertion_type";
    private const string JwtBearerClientAssertionType = "urn:ietf:params:oauth:client-assertion-type:jwt-bearer";

    private async Task<TokenResponse> RequestTokenAsync(
        string authServerUrl,
        Guid organizationId,
        string clientId,
        KeyValuePair<string, string> credential,
        CancellationToken cancellationToken,
        params KeyValuePair<string, string>[] extraParameters)
    {
        var tokenUrl = $"{authServerUrl}/connect/token";
        var prefixedClientId = $"{organizationId}:{clientId}";

        var parameters = new List<KeyValuePair<string, string>>
        {
            new("grant_type", "client_credentials"),
            new("scope", "snapcd_scope"),
            new("client_id", prefixedClientId),
            credential
        };
        parameters.AddRange(extraParameters);

        var content = new FormUrlEncodedContent(parameters);

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