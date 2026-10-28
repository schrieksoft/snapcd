// SPDX-License-Identifier: LicenseRef-Snap-CD-Source-Available-1.1
// Copyright (c) 2026 Karl Schriek / Schrieksoft.
// No license is granted to use this file, in whole or in part, (a) as training, fine-tuning, retrieval, or
// embedding data for any machine-learning model, or (b) as input to any machine-learning model, agent, or automated
// system for the purpose of producing a derivative work or reimplementation that is not otherwise permitted by the
// Snap CD Source-Available License (including any Competing Product as defined therein). Contact info@snapcd.io
// for terms covering either use.

using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using SnapCd.Contracts;
using SnapCd.Server.Core.Services.Integrations.Connections;

namespace SnapCd.Server.Core.Services.Integrations.Codecs;

public sealed class SlackCodec : IIntegrationCodec
{
    /// <summary>Returned in place of a secret on display, and recognised on update as "keep existing".</summary>
    public const string SecretMask = "••••••••";

    private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNameCaseInsensitive = true };

    private readonly IHttpClientFactory _httpClientFactory;

    public SlackCodec(IHttpClientFactory httpClientFactory) => _httpClientFactory = httpClientFactory;

    public IntegrationType Type => IntegrationType.Slack;

    public IIntegrationConnection Deserialize(string json)
        => JsonSerializer.Deserialize<SlackConnection>(json, JsonOptions) ?? new SlackConnection();

    public string Serialize(IIntegrationConnection connection)
        => JsonSerializer.Serialize((SlackConnection)connection);

    public IIntegrationConnection FromInput(JsonElement input, IIntegrationConnection? existing)
    {
        var incoming = input.Deserialize<SlackConnection>(JsonOptions) ?? new SlackConnection();
        var prior = existing as SlackConnection;

        var botToken = string.IsNullOrEmpty(incoming.BotToken) || incoming.BotToken == SecretMask
            ? prior?.BotToken ?? string.Empty
            : incoming.BotToken;

        return new SlackConnection
        {
            BotToken = botToken,
            DefaultChannel = incoming.DefaultChannel
        };
    }

    public object ToRedactedView(IIntegrationConnection connection)
    {
        var c = (SlackConnection)connection;
        // A dictionary (not an anonymous type) so callers — the Blazor edit form especially — can read
        // fields by key in-process, and it still serialises to the same JSON object for the API.
        return new Dictionary<string, object?>
        {
            ["botToken"] = string.IsNullOrEmpty(c.BotToken) ? string.Empty : SecretMask,
            ["defaultChannel"] = c.DefaultChannel
        };
    }

    public IReadOnlyList<string> Validate(IIntegrationConnection connection)
    {
        var c = (SlackConnection)connection;
        var errors = new List<string>();
        if (string.IsNullOrWhiteSpace(c.BotToken)) errors.Add("Bot token is required.");
        if (string.IsNullOrWhiteSpace(c.DefaultChannel)) errors.Add("Default channel is required.");
        return errors;
    }

    public async Task<IntegrationSendResult> SendAsync(IIntegrationConnection connection, string text, string? threadId, CancellationToken ct)
    {
        var c = (SlackConnection)connection;
        var client = _httpClientFactory.CreateClient();
        using var req = new HttpRequestMessage(HttpMethod.Post, "https://slack.com/api/chat.postMessage");
        req.Headers.Authorization = new AuthenticationHeaderValue("Bearer", c.BotToken);
        var payload = new Dictionary<string, object?> { ["channel"] = c.DefaultChannel, ["text"] = text };
        if (!string.IsNullOrEmpty(threadId)) payload["thread_ts"] = threadId;
        req.Content = JsonContent.Create(payload);

        try
        {
            using var resp = await client.SendAsync(req, ct);
            var body = await resp.Content.ReadAsStringAsync(ct);
            using var doc = JsonDocument.Parse(body);
            var root = doc.RootElement;
            var ok = root.TryGetProperty("ok", out var okEl) && okEl.GetBoolean();
            if (!ok)
                return new IntegrationSendResult(false, null, root.TryGetProperty("error", out var e) ? e.GetString() : "unknown_error");
            var ts = root.TryGetProperty("ts", out var tsEl) ? tsEl.GetString() : null;
            return new IntegrationSendResult(true, ts, null);
        }
        catch (Exception ex)
        {
            return new IntegrationSendResult(false, null, ex.Message);
        }
    }

    public async Task<IntegrationTestResult> TestConnectionAsync(IIntegrationConnection connection, CancellationToken ct)
    {
        var c = (SlackConnection)connection;
        var client = _httpClientFactory.CreateClient();
        using var req = new HttpRequestMessage(HttpMethod.Post, "https://slack.com/api/auth.test");
        req.Headers.Authorization = new AuthenticationHeaderValue("Bearer", c.BotToken);

        try
        {
            using var resp = await client.SendAsync(req, ct);
            var body = await resp.Content.ReadAsStringAsync(ct);
            using var doc = JsonDocument.Parse(body);
            var ok = doc.RootElement.TryGetProperty("ok", out var okEl) && okEl.GetBoolean();
            return ok
                ? new IntegrationTestResult(true, null)
                : new IntegrationTestResult(false, doc.RootElement.TryGetProperty("error", out var e) ? e.GetString() : "unknown_error");
        }
        catch (Exception ex)
        {
            return new IntegrationTestResult(false, ex.Message);
        }
    }
}
