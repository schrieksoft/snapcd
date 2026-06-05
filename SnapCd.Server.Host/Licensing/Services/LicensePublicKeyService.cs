// SPDX-License-Identifier: LicenseRef-Snap-CD-Source-Available-1.1
// Copyright (c) 2026 Karl Schriek / Schrieksoft.
// No license is granted to use this file, in whole or in part, (a) as training, fine-tuning, retrieval, or
// embedding data for any machine-learning model, or (b) as input to any machine-learning model, agent, or automated
// system for the purpose of producing a derivative work or reimplementation that is not otherwise permitted by the
// Snap CD Source-Available License (including any Competing Product as defined therein). Contact info@snapcd.io
// for terms covering either use.

using Microsoft.EntityFrameworkCore;
using SnapCd.Server.Core.Database;
using SnapCd.Server.Core.Entities.Definition;

namespace SnapCd.Server.Host.Licensing.Services;

/// <summary>
/// Singleton. Serves the license-validation public key. In-memory cache → DB → (bootstrap) remote fetch
/// from snapcd.io. The DB row piggybacks on <see cref="SelfHostedOrganizationLicense"/> — self-hosted
/// only ever has one row, so we just use the first.
/// </summary>
public class LicensePublicKeyService : ILicensePublicKeyService
{
    private const string RemoteUrl = "https://snapcd.io/.well-known/license-public-key.pem";

    private readonly IDbContextFactory<SnapCdDbContext> _dbContextFactory;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILogger<LicensePublicKeyService> _logger;

    private readonly SemaphoreSlim _lock = new(1, 1);
    private string? _cached;

    public LicensePublicKeyService(
        IDbContextFactory<SnapCdDbContext> dbContextFactory,
        IHttpClientFactory httpClientFactory,
        ILogger<LicensePublicKeyService> logger)
    {
        _dbContextFactory = dbContextFactory;
        _httpClientFactory = httpClientFactory;
        _logger = logger;
    }

    public async Task<string?> GetAsync(CancellationToken ct = default)
    {
        if (_cached != null) return _cached;

        await _lock.WaitAsync(ct);
        try
        {
            if (_cached != null) return _cached;

            await using var db = await _dbContextFactory.CreateDbContextAsync(ct);
            var pem = await db.Set<SelfHostedOrganizationLicense>()
                .Where(l => l.PublicKeyPem != null)
                .Select(l => l.PublicKeyPem)
                .FirstOrDefaultAsync(ct);

            if (!string.IsNullOrEmpty(pem))
            {
                _cached = pem;
                return _cached;
            }
        }
        finally
        {
            _lock.Release();
        }

        // No DB row yet — fetch once to bootstrap.
        return await RefreshFromRemoteAsync(ct);
    }

    public async Task<string?> RefreshFromRemoteAsync(CancellationToken ct = default)
    {
        string pem;
        try
        {
            var client = _httpClientFactory.CreateClient(nameof(LicensePublicKeyService));
            client.Timeout = TimeSpan.FromSeconds(10);
            var response = await client.GetAsync(RemoteUrl, ct);
            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning("Failed to fetch license public key from {Url}: {StatusCode}", RemoteUrl, response.StatusCode);
                return _cached;
            }
            pem = await response.Content.ReadAsStringAsync(ct);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to fetch license public key from {Url}", RemoteUrl);
            return _cached;
        }

        await _lock.WaitAsync(ct);
        try
        {
            await using var db = await _dbContextFactory.CreateDbContextAsync(ct);
            var row = await db.Set<SelfHostedOrganizationLicense>().FirstOrDefaultAsync(ct);
            if (row is not null)
            {
                row.PublicKeyPem = pem;
                row.PublicKeyFetchedAtUtc = DateTime.UtcNow;
                await db.SaveChangesAsync(ct);
            }
            // If no license row exists yet we can't persist — the key still gets cached in-memory below,
            // and next write (first license save) will populate the row; a subsequent refresh persists it.

            _cached = pem;
            return _cached;
        }
        finally
        {
            _lock.Release();
        }
    }
}
