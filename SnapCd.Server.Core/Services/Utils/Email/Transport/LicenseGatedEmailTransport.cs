// SPDX-License-Identifier: LicenseRef-Snap-CD-Source-Available-1.1
// Copyright (c) 2026 Karl Schriek / Schrieksoft.
// No license is granted to use this file, in whole or in part, (a) as training, fine-tuning, retrieval, or
// embedding data for any machine-learning model, or (b) as input to any machine-learning model, agent, or automated
// system for the purpose of producing a derivative work or reimplementation that is not otherwise permitted by the
// Snap CD Source-Available License (including any Competing Product as defined therein). Contact info@snapcd.io
// for terms covering either use.

using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.DependencyInjection;
using SnapCd.Server.Core.Licensing.Services;

namespace SnapCd.Server.Core.Services.Email.Transport;

/// <summary>
/// Decorates the configured premium <see cref="IEmailTransport"/> with a licence check.
/// When the active licence does not include the PremiumEmailProvider feature, every call
/// is silently routed to <see cref="NoOpEmailTransport"/> instead.
/// </summary>
public class LicenseGatedEmailTransport : IEmailTransport
{
    public const string ConfiguredKey = "configured";
    public const string NoOpKey = "noop";

    private const string CacheKey = "premium-email-allowed";
    private static readonly TimeSpan CacheTtl = TimeSpan.FromMinutes(1);

    private readonly IEmailTransport _configured;
    private readonly IEmailTransport _noOp;
    private readonly IPremiumEmailPolicy _policy;
    private readonly IMemoryCache _cache;
    private readonly ILogger<LicenseGatedEmailTransport> _logger;

    public LicenseGatedEmailTransport(
        [FromKeyedServices(ConfiguredKey)] IEmailTransport configured,
        [FromKeyedServices(NoOpKey)] IEmailTransport noOp,
        IPremiumEmailPolicy policy,
        IMemoryCache cache,
        ILogger<LicenseGatedEmailTransport> logger)
    {
        _configured = configured;
        _noOp = noOp;
        _policy = policy;
        _cache = cache;
        _logger = logger;
    }

    public async Task<bool> SendAsync(string toEmail, string subject, string htmlContent, string? plainTextContent = null)
    {
        if (await IsDeliveryActiveAsync())
        {
            return await _configured.SendAsync(toEmail, subject, htmlContent, plainTextContent);
        }

        _logger.LogDebug("Email send routed to NoOp (PremiumEmailProvider feature not licensed).");
        return await _noOp.SendAsync(toEmail, subject, htmlContent, plainTextContent);
    }

    public async Task<bool> IsDeliveryActiveAsync(CancellationToken ct = default)
    {
        return await _cache.GetOrCreateAsync(CacheKey, async entry =>
        {
            entry.AbsoluteExpirationRelativeToNow = CacheTtl;
            return await _policy.IsAllowedAsync(ct);
        });
    }
}
