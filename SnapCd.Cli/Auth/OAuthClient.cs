// SPDX-License-Identifier: LicenseRef-Snap-CD-Source-Available-1.1
// Copyright (c) 2026 Karl Schriek / Schrieksoft.
// No license is granted to use this file, in whole or in part, (a) as training, fine-tuning, retrieval, or
// embedding data for any machine-learning model, or (b) as input to any machine-learning model, agent, or automated
// system for the purpose of producing a derivative work or reimplementation that is not otherwise permitted by the
// Snap CD Source-Available License (including any Competing Product as defined therein). Contact info@snapcd.io
// for terms covering either use.

using System.Net.Http.Headers;
using System.Text.Json;
using System.Text.Json.Serialization;
using SnapCd.Cli.Configuration;

namespace SnapCd.Cli.Auth;

public sealed record TokenSet(string AccessToken, DateTimeOffset ExpiresOn, string? RefreshToken);

public sealed record Organization(Guid Id, string Name);

public sealed record UserInfo(string? Email, IReadOnlyList<Organization> Organizations);

/// <summary>The Server's OpenIddict endpoints, as the public <c>snapcd-cli</c> client sees them.</summary>
public class OAuthClient
{
    public const string ClientId = "snapcd-cli";
    public const string Scopes = "openid offline_access snapcd_scope email profile";

    private readonly HttpClient _http;
    private readonly string _server;
    private readonly TimeProvider _time;

    public OAuthClient(HttpClient http, string serverUrl, TimeProvider? time = null)
    {
        _http = http;
        _server = serverUrl.TrimEnd('/');
        _time = time ?? TimeProvider.System;
    }

    public static HttpClient CreateHttpClient(CliContext context)
    {
        var handler = new HttpClientHandler();
        if (context.InsecureSkipVerify)
            handler.ServerCertificateCustomValidationCallback = HttpClientHandler.DangerousAcceptAnyServerCertificateValidator;
        return new HttpClient(handler) { Timeout = TimeSpan.FromSeconds(30) };
    }

    public string AuthorizeUrl(string redirectUri, Pkce pkce, string state)
    {
        var query = new Dictionary<string, string>
        {
            ["client_id"] = ClientId,
            ["response_type"] = "code",
            ["redirect_uri"] = redirectUri,
            ["scope"] = Scopes,
            ["code_challenge"] = pkce.Challenge,
            ["code_challenge_method"] = "S256",
            ["state"] = state
        };
        return $"{_server}/connect/authorize?" + string.Join("&",
            query.Select(kv => $"{kv.Key}={Uri.EscapeDataString(kv.Value)}"));
    }

    public Task<TokenSet> ExchangeCodeAsync(string code, string redirectUri, Pkce pkce, CancellationToken ct) =>
        RequestTokenAsync(new Dictionary<string, string>
        {
            ["grant_type"] = "authorization_code",
            ["client_id"] = ClientId,
            ["code"] = code,
            ["redirect_uri"] = redirectUri,
            ["code_verifier"] = pkce.Verifier
        }, ct);

    public Task<TokenSet> RefreshAsync(string refreshToken, CancellationToken ct) =>
        RequestTokenAsync(new Dictionary<string, string>
        {
            ["grant_type"] = "refresh_token",
            ["client_id"] = ClientId,
            ["refresh_token"] = refreshToken
        }, ct);

    public async Task<UserInfo> GetUserInfoAsync(string accessToken, CancellationToken ct)
    {
        using var request = new HttpRequestMessage(HttpMethod.Get, $"{_server}/connect/userinfo");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
        using var response = await SendAsync(request, ct);
        var body = await response.Content.ReadAsStringAsync(ct);
        if (!response.IsSuccessStatusCode)
            throw new CliException($"The Server refused the user info request ({(int)response.StatusCode}).");

        var info = JsonSerializer.Deserialize<UserInfoResponse>(body)
                   ?? throw new CliException("The Server returned no user info.");
        return new UserInfo(info.Email,
            (info.Organizations ?? []).Select(o => new Organization(o.Id, o.Name)).ToList());
    }

    private async Task<TokenSet> RequestTokenAsync(Dictionary<string, string> form, CancellationToken ct)
    {
        using var request = new HttpRequestMessage(HttpMethod.Post, $"{_server}/connect/token")
        {
            Content = new FormUrlEncodedContent(form)
        };
        using var response = await SendAsync(request, ct);
        var body = await response.Content.ReadAsStringAsync(ct);

        if (!response.IsSuccessStatusCode)
        {
            var error = TryRead<ErrorResponse>(body);
            if (error?.Error == "invalid_grant")
                throw new SessionExpiredException(error.Description ?? "The session is no longer valid.");
            throw new CliException(
                $"The Server refused the token request ({(int)response.StatusCode}): {error?.Description ?? error?.Error ?? body}");
        }

        var token = TryRead<TokenResponse>(body)
                    ?? throw new CliException("The Server returned an unreadable token response.");
        return new TokenSet(token.AccessToken, _time.GetUtcNow().AddSeconds(token.ExpiresIn), token.RefreshToken);
    }

    private async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken ct)
    {
        try
        {
            return await _http.SendAsync(request, ct);
        }
        catch (HttpRequestException e)
        {
            throw new CliException($"Could not reach {_server}: {e.Message}");
        }
    }

    private static T? TryRead<T>(string body) where T : class
    {
        try { return JsonSerializer.Deserialize<T>(body); }
        catch (JsonException) { return null; }
    }

    private sealed class TokenResponse
    {
        [JsonPropertyName("access_token")] public string AccessToken { get; set; } = "";
        [JsonPropertyName("expires_in")] public int ExpiresIn { get; set; }
        [JsonPropertyName("refresh_token")] public string? RefreshToken { get; set; }
    }

    private sealed class ErrorResponse
    {
        [JsonPropertyName("error")] public string? Error { get; set; }
        [JsonPropertyName("error_description")] public string? Description { get; set; }
    }

    private sealed class UserInfoResponse
    {
        [JsonPropertyName("email")] public string? Email { get; set; }
        [JsonPropertyName("snapcd_organizations")] public List<OrganizationResponse>? Organizations { get; set; }
    }

    private sealed class OrganizationResponse
    {
        [JsonPropertyName("id")] public Guid Id { get; set; }
        [JsonPropertyName("name")] public string Name { get; set; } = "";
    }
}

/// <summary>The refresh token was refused; only a new login helps.</summary>
public class SessionExpiredException(string message) : CliException(message);
