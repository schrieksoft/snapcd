// SPDX-License-Identifier: LicenseRef-Snap-CD-Source-Available-1.1
// Copyright (c) 2026 Karl Schriek / Schrieksoft.
// No license is granted to use this file, in whole or in part, (a) as training, fine-tuning, retrieval, or
// embedding data for any machine-learning model, or (b) as input to any machine-learning model, agent, or automated
// system for the purpose of producing a derivative work or reimplementation that is not otherwise permitted by the
// Snap CD Source-Available License (including any Competing Product as defined therein). Contact info@snapcd.io
// for terms covering either use.

using System.Net.Http.Json;
using System.Runtime.CompilerServices;
using System.Text.Json;
using System.Text.Json.Serialization;
using SnapCd.Agent.Configuration;
using SnapCd.Agent.Models;
using SnapCd.Contracts;
using SnapCd.Contracts.AgentResults;

namespace SnapCd.Agent.Services.Sidecars;

/// <summary>
/// Default sidecar implementation: an HTTP service exposing <c>POST /invoke</c> (Server-Sent Events)
/// and <c>GET /health</c>. The sidecar process/container lifecycle is managed externally.
/// </summary>
public sealed class HttpAgentSidecar : IAgentSidecar
{
    private readonly SidecarOptions _options;
    private readonly IHttpClientFactory _httpClientFactory;

    public HttpAgentSidecar(SidecarOptions options, IHttpClientFactory httpClientFactory)
    {
        _options = options;
        _httpClientFactory = httpClientFactory;
    }

    public string Name => _options.Name;

    public async IAsyncEnumerable<SidecarStreamEvent> InvokeStreamAsync(
        InvokeRequest request, [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        var client = CreateClient();
        using var httpRequest = new HttpRequestMessage(HttpMethod.Post, "/invoke")
        {
            Content = JsonContent.Create(request)
        };

        using var response = await client.SendAsync(
            httpRequest, HttpCompletionOption.ResponseHeadersRead, cancellationToken);
        response.EnsureSuccessStatusCode();

        await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
        using var reader = new StreamReader(stream);

        while (await reader.ReadLineAsync(cancellationToken) is { } line)
        {
            if (!line.StartsWith("data:", StringComparison.Ordinal))
                continue;

            var payload = line["data:".Length..].Trim();
            if (payload.Length == 0)
                continue;

            var parsed = Parse(payload);
            if (parsed is not null)
                yield return parsed;
        }
    }

    public async Task<bool> IsHealthyAsync(CancellationToken cancellationToken)
    {
        try
        {
            var client = CreateClient();
            using var response = await client.GetAsync("/health", cancellationToken);
            return response.IsSuccessStatusCode;
        }
        catch
        {
            return false;
        }
    }

    private HttpClient CreateClient()
    {
        var client = _httpClientFactory.CreateClient($"sidecar:{_options.Name}");
        client.BaseAddress = new Uri(_options.BaseUrl);
        return client;
    }

    private static SidecarStreamEvent? Parse(string json)
    {
        SseEvent? evt;
        try
        {
            evt = JsonSerializer.Deserialize<SseEvent>(json);
        }
        catch (JsonException)
        {
            return null;
        }

        if (evt is null)
            return null;

        if (evt.Type == "result")
            return new SidecarStreamEvent { IsResult = true, Result = MapResult(evt.Result) };

        if (evt.Type == "milestone")
            return new SidecarStreamEvent { IsMilestone = true, MilestoneKind = evt.Kind, Message = evt.Message };

        return new SidecarStreamEvent { IsResult = false, Level = evt.Level ?? "info", Message = evt.Message };
    }

    private static MissionResultDto MapResult(JsonElement r)
    {
        return new MissionResultDto
        {
            Success = r.TryGetProperty("success", out var s) && s.ValueKind == JsonValueKind.True,
            Summary = GetString(r, "summary"),
            Error = GetString(r, "error"),
            Detail = GetString(r, "detail"),
            DurationSeconds = r.TryGetProperty("duration_seconds", out var d) && d.ValueKind == JsonValueKind.Number ? d.GetDouble() : 0,
            ToolCallsJson = r.TryGetProperty("tool_calls", out var tc) && tc.ValueKind != JsonValueKind.Null ? tc.GetRawText() : null,
            TokensJson = r.TryGetProperty("tokens_used", out var tk) && tk.ValueKind != JsonValueKind.Null ? tk.GetRawText() : null,
            SessionId = GetString(r, "session_id"),
            DiagnosisCategory = ParseDiagnosisCategory(GetString(r, "diagnosis_category")),
        };
    }

    private static DiagnosisCategory? ParseDiagnosisCategory(string? value)
        => string.IsNullOrEmpty(value) ? null
           : Enum.TryParse<DiagnosisCategory>(value, ignoreCase: true, out var c) ? c
           : DiagnosisCategory.Unknown;

    private static string? GetString(JsonElement obj, string name)
        => obj.TryGetProperty(name, out var v) && v.ValueKind == JsonValueKind.String ? v.GetString() : null;

    private sealed class SseEvent
    {
        [JsonPropertyName("type")] public string Type { get; set; } = "";
        [JsonPropertyName("level")] public string? Level { get; set; }
        [JsonPropertyName("kind")] public string? Kind { get; set; }
        [JsonPropertyName("message")] public string? Message { get; set; }
        [JsonPropertyName("result")] public JsonElement Result { get; set; }
    }
}
