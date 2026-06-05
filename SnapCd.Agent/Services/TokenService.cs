// SPDX-License-Identifier: LicenseRef-Snap-CD-Source-Available-1.1
// Copyright (c) 2026 Karl Schriek / Schrieksoft.
// No license is granted to use this file, in whole or in part, (a) as training, fine-tuning, retrieval, or
// embedding data for any machine-learning model, or (b) as input to any machine-learning model, agent, or automated
// system for the purpose of producing a derivative work or reimplementation that is not otherwise permitted by the
// Snap CD Source-Available License (including any Competing Product as defined therein). Contact info@snapcd.io
// for terms covering either use.

using System.Net.Http.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Options;
using SnapCd.Agent.Configuration;

namespace SnapCd.Agent.Services;

/// <summary>
/// Mints and caches agent-attributed JWTs via the client_credentials grant with the custom
/// <c>agent_id</c> parameter, so the token carries the <c>agent_id</c> claim that AgentHub and
/// the MCP endpoint require. Refreshes shortly before expiry.
/// </summary>
public sealed class TokenService
{
    private readonly AgentOptions _options;
    private readonly ServerSettings _server;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILogger<TokenService> _logger;
    private readonly SemaphoreSlim _lock = new(1, 1);

    private string? _cachedToken;
    private DateTimeOffset _expiresAt = DateTimeOffset.MinValue;

    // Refresh this long before the token actually expires.
    private static readonly TimeSpan RefreshSkew = TimeSpan.FromSeconds(30);

    public TokenService(
        IOptions<AgentOptions> options,
        IOptions<ServerSettings> server,
        IHttpClientFactory httpClientFactory,
        ILogger<TokenService> logger)
    {
        _options = options.Value;
        _server = server.Value;
        _httpClientFactory = httpClientFactory;
        _logger = logger;
    }

    public async Task<string> GetCurrentTokenAsync(CancellationToken cancellationToken = default)
    {
        if (_cachedToken is not null && DateTimeOffset.UtcNow < _expiresAt - RefreshSkew)
            return _cachedToken;

        await _lock.WaitAsync(cancellationToken);
        try
        {
            if (_cachedToken is not null && DateTimeOffset.UtcNow < _expiresAt - RefreshSkew)
                return _cachedToken;

            return await RefreshAsync(cancellationToken);
        }
        finally
        {
            _lock.Release();
        }
    }

    private async Task<string> RefreshAsync(CancellationToken cancellationToken)
    {
        var client = _httpClientFactory.CreateClient(nameof(TokenService));
        client.BaseAddress = new Uri(_server.Url);

        var prefixedClientId = $"{_options.OrganizationId}:{_options.ClientId}";

        var form = new Dictionary<string, string>
        {
            ["grant_type"] = "client_credentials",
            ["scope"] = "snapcd_scope",
            ["client_id"] = prefixedClientId,
            ["client_secret"] = _options.ClientSecret,
            ["agent_id"] = _options.AgentId.ToString(),
        };

        using var content = new FormUrlEncodedContent(form);
        using var response = await client.PostAsync("/connect/token", content, cancellationToken);
        response.EnsureSuccessStatusCode();

        var payload = await response.Content.ReadFromJsonAsync<TokenResponse>(cancellationToken)
            ?? throw new InvalidOperationException("Token endpoint returned an empty body.");

        _cachedToken = payload.AccessToken;
        _expiresAt = DateTimeOffset.UtcNow.AddSeconds(payload.ExpiresIn);
        _logger.LogInformation("Refreshed agent token; expires in {ExpiresIn}s.", payload.ExpiresIn);
        return _cachedToken!;
    }

    private sealed class TokenResponse
    {
        [JsonPropertyName("access_token")] public string AccessToken { get; set; } = null!;
        [JsonPropertyName("expires_in")] public int ExpiresIn { get; set; }
    }
}
