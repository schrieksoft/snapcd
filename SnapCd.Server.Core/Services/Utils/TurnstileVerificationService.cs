// SPDX-License-Identifier: LicenseRef-Snap-CD-Source-Available-1.1
// Copyright (c) 2026 Karl Schriek / Schrieksoft.
// No license is granted to use this file, in whole or in part, (a) as training, fine-tuning, retrieval, or
// embedding data for any machine-learning model, or (b) as input to any machine-learning model, agent, or automated
// system for the purpose of producing a derivative work or reimplementation that is not otherwise permitted by the
// Snap CD Source-Available License (including any Competing Product as defined therein). Contact info@snapcd.io
// for terms covering either use.

using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Options;
using SnapCd.Server.Core.Licensing.Services;
using SnapCd.Server.Core.Settings;

namespace SnapCd.Server.Core.Services;

public class TurnstileVerificationService
{
    private readonly HttpClient _httpClient;
    private readonly TurnstileSettings _settings;
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<TurnstileVerificationService> _logger;
    private const string VerifyUrl = "https://challenges.cloudflare.com/turnstile/v0/siteverify";

    public TurnstileVerificationService(
        HttpClient httpClient,
        IOptions<TurnstileSettings> settings,
        IServiceProvider serviceProvider,
        ILogger<TurnstileVerificationService> logger)
    {
        _httpClient = httpClient;
        _settings = settings.Value;
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    public async Task<TurnstileVerificationResult> VerifyAsync(string? token, string? remoteIp = null)
    {
        if (!await TurnstileGatingService.ShouldEnableTurnstileAsync(_serviceProvider))
        {
            return TurnstileVerificationResult.Success();
        }

        if (string.IsNullOrWhiteSpace(token))
        {
            _logger.LogWarning("Turnstile verification failed: no token provided");
            return TurnstileVerificationResult.Failure("Please complete the verification challenge.");
        }

        try
        {
            var formData = new Dictionary<string, string>
            {
                ["secret"] = _settings.SecretKey,
                ["response"] = token
            };

            if (!string.IsNullOrEmpty(remoteIp))
            {
                formData["remoteip"] = remoteIp;
            }

            var response = await _httpClient.PostAsync(
                VerifyUrl,
                new FormUrlEncodedContent(formData));

            var json = await response.Content.ReadAsStringAsync();
            var result = JsonSerializer.Deserialize<TurnstileApiResponse>(json);

            if (result?.Success == true)
            {
                _logger.LogDebug("Turnstile verification succeeded");
                return TurnstileVerificationResult.Success();
            }

            var errorCodes = result?.ErrorCodes ?? [];
            _logger.LogWarning("Turnstile verification failed: {ErrorCodes}",
                string.Join(", ", errorCodes));

            return TurnstileVerificationResult.Failure(
                "Verification failed. Please try again.",
                errorCodes);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Turnstile verification error");
            return TurnstileVerificationResult.Failure(
                "Verification service unavailable. Please try again.");
        }
    }
}

public class TurnstileVerificationResult
{
    public bool IsSuccess { get; private init; }
    public string? ErrorMessage { get; private init; }
    public string[] ErrorCodes { get; private init; } = [];

    public static TurnstileVerificationResult Success() => new() { IsSuccess = true };

    public static TurnstileVerificationResult Failure(string message, string[]? errorCodes = null) =>
        new()
        {
            IsSuccess = false,
            ErrorMessage = message,
            ErrorCodes = errorCodes ?? []
        };
}

internal class TurnstileApiResponse
{
    [JsonPropertyName("success")]
    public bool Success { get; set; }

    [JsonPropertyName("error-codes")]
    public string[]? ErrorCodes { get; set; }

    [JsonPropertyName("challenge_ts")]
    public string? ChallengeTimestamp { get; set; }

    [JsonPropertyName("hostname")]
    public string? Hostname { get; set; }
}
