// SPDX-License-Identifier: LicenseRef-Snap-CD-Source-Available-1.1
// Copyright (c) 2026 Karl Schriek / Schrieksoft.
// No license is granted to use this file, in whole or in part, (a) as training, fine-tuning, retrieval, or
// embedding data for any machine-learning model, or (b) as input to any machine-learning model, agent, or automated
// system for the purpose of producing a derivative work or reimplementation that is not otherwise permitted by the
// Snap CD Source-Available License (including any Competing Product as defined therein). Contact info@snapcd.io
// for terms covering either use.

using Microsoft.Extensions.Options;
using RestSharp;
using SnapCd.Server.Core.Settings;

namespace SnapCd.Server.Core.Services.Email.Transport;

public class PostmarkEmailTransport : IEmailTransport
{
    private readonly ILogger<PostmarkEmailTransport> _logger;
    private readonly PostmarkEmailTransportSettings _settings;

    public PostmarkEmailTransport(
        IOptions<PostmarkEmailTransportSettings> options,
        ILogger<PostmarkEmailTransport> logger)
    {
        _logger = logger;
        _settings = options.Value;
    }

    public async Task<bool> SendAsync(string toEmail, string subject, string htmlContent, string? plainTextContent = null)
    {
        if (string.IsNullOrEmpty(_settings.ApiKey))
            throw new InvalidOperationException("Postmark ApiKey is not configured");

        using var client = new RestClient(new RestClientOptions("https://api.postmarkapp.com"));
        var request = new RestRequest("email", Method.Post);
        request.AddHeader("Accept", "application/json");
        request.AddHeader("X-Postmark-Server-Token", _settings.ApiKey);
        request.AddJsonBody(new
        {
            From = $"{_settings.FromName} <{_settings.FromEmail}>",
            To = toEmail,
            Subject = subject,
            HtmlBody = htmlContent,
            TextBody = plainTextContent ?? htmlContent,
            MessageStream = "outbound",
        });

        var response = await client.ExecuteAsync(request);
        if (response.IsSuccessful)
        {
            _logger.LogInformation("Email to {Email} queued successfully via Postmark.", toEmail);
            return true;
        }

        _logger.LogError("Postmark send failed for {Email}. Status: {Status}. Body: {Body}", toEmail, response.StatusCode, response.Content);
        throw new InvalidOperationException($"Failed to send email to \"{toEmail}\" with Postmark. Status: {response.StatusCode}. Body: {response.Content}");
    }
}
