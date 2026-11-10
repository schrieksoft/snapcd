// SPDX-License-Identifier: LicenseRef-Snap-CD-Source-Available-1.1
// Copyright (c) 2026 Karl Schriek / Schrieksoft.
// No license is granted to use this file, in whole or in part, (a) as training, fine-tuning, retrieval, or
// embedding data for any machine-learning model, or (b) as input to any machine-learning model, agent, or automated
// system for the purpose of producing a derivative work or reimplementation that is not otherwise permitted by the
// Snap CD Source-Available License (including any Competing Product as defined therein). Contact info@snapcd.io
// for terms covering either use.

using System.Text.Json;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.IdentityModel.Tokens;

namespace SnapCd.Server.Core.Services.Nonsecured.Federation;

/// <summary>
/// Fetches and caches the public signing keys an external issuer publishes, through its OpenID
/// Connect discovery document.
/// </summary>
public class FederationKeyProvider
{
    private readonly HttpClient _httpClient;
    private readonly IMemoryCache _cache;
    private readonly ILogger<FederationKeyProvider> _logger;

    // Issuers rotate keys on the order of days, and a fetch per token request would make the
    // issuer a dependency of every authentication.
    private static readonly TimeSpan CacheDuration = TimeSpan.FromHours(1);

    // The response is from a host named in the database, so it is read with a cap rather than
    // trusted to be small.
    private const int MaxResponseBytes = 512 * 1024;

    public FederationKeyProvider(HttpClient httpClient, IMemoryCache cache, ILogger<FederationKeyProvider> logger)
    {
        _httpClient = httpClient;
        _cache = cache;
        _logger = logger;
    }

    public async Task<IEnumerable<SecurityKey>> GetSigningKeysAsync(string issuer, CancellationToken cancellationToken = default)
    {
        var cacheKey = $"federation-jwks:{issuer}";

        if (_cache.TryGetValue(cacheKey, out IList<SecurityKey>? cached) && cached is { Count: > 0 })
            return cached;

        var keys = await FetchSigningKeysAsync(issuer, cancellationToken);

        _cache.Set(cacheKey, keys, CacheDuration);

        return keys;
    }

    /// <summary>Drops the cached keys for an issuer, so the next validation refetches them.</summary>
    public void Invalidate(string issuer) => _cache.Remove($"federation-jwks:{issuer}");

    private async Task<IList<SecurityKey>> FetchSigningKeysAsync(string issuer, CancellationToken cancellationToken)
    {
        if (!Uri.TryCreate(issuer, UriKind.Absolute, out var issuerUri) || issuerUri.Scheme != Uri.UriSchemeHttps)
            throw new InvalidOperationException($"Issuer '{issuer}' is not an absolute https URL.");

        var discoveryUrl = $"{issuer.TrimEnd('/')}/.well-known/openid-configuration";

        var discovery = await GetJsonAsync(discoveryUrl, cancellationToken);

        if (!discovery.RootElement.TryGetProperty("jwks_uri", out var jwksUriElement) ||
            jwksUriElement.GetString() is not { Length: > 0 } jwksUri)
            throw new InvalidOperationException($"The discovery document at {discoveryUrl} carries no jwks_uri.");

        // The document is served over TLS by the host named in the trust record, so its jwks_uri
        // is as trustworthy as the issuer itself. Kubernetes publishes an in-cluster issuer name
        // and an apiserver IP for the keys, so the two hosts routinely differ.
        if (!Uri.TryCreate(jwksUri, UriKind.Absolute, out var jwksUriParsed) ||
            jwksUriParsed.Scheme != Uri.UriSchemeHttps)
            throw new InvalidOperationException($"The jwks_uri of issuer '{issuer}' is not an absolute https URL.");

        using var jwksDocument = await GetJsonAsync(jwksUri, cancellationToken);

        var keySet = new JsonWebKeySet(jwksDocument.RootElement.GetRawText());
        var keys = keySet.GetSigningKeys();

        if (keys.Count == 0)
            throw new InvalidOperationException($"Issuer '{issuer}' published no signing keys.");

        _logger.LogDebug("Retrieved {KeyCount} signing key(s) for issuer {Issuer}", keys.Count, issuer);

        return keys;
    }

    private async Task<JsonDocument> GetJsonAsync(string url, CancellationToken cancellationToken)
    {
        using var response = await _httpClient.GetAsync(url, HttpCompletionOption.ResponseHeadersRead, cancellationToken);
        response.EnsureSuccessStatusCode();

        await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
        var buffer = new byte[MaxResponseBytes];
        var total = 0;

        while (total < buffer.Length)
        {
            var read = await stream.ReadAsync(buffer.AsMemory(total), cancellationToken);
            if (read == 0) break;
            total += read;
        }

        if (total == buffer.Length)
            throw new InvalidOperationException($"The response from {url} exceeds {MaxResponseBytes} bytes.");

        return JsonDocument.Parse(buffer.AsMemory(0, total));
    }
}
