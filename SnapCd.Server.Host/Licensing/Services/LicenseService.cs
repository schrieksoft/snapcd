// SPDX-License-Identifier: LicenseRef-Snap-CD-Source-Available-1.1
// Copyright (c) 2026 Karl Schriek / Schrieksoft.
// No license is granted to use this file, in whole or in part, (a) as training, fine-tuning, retrieval, or
// embedding data for any machine-learning model, or (b) as input to any machine-learning model, agent, or automated
// system for the purpose of producing a derivative work or reimplementation that is not otherwise permitted by the
// Snap CD Source-Available License (including any Competing Product as defined therein). Contact info@snapcd.io
// for terms covering either use.

using System.Diagnostics;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Cryptography;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using SnapCd.Server.Core.Database;
using SnapCd.Server.Core.Entities.Definition;
using SnapCd.Server.Core.Licensing.Models;
using SnapCd.Server.Core.Licensing.Services;
using SnapCd.Server.Core.Settings;

namespace SnapCd.Server.Host.Licensing.Services;

public class LicenseService(
    IDbContextFactory<SnapCdDbContext> dbContextFactory,
    IMemoryCache memoryCache,
    IRemoteLicenseClient remoteLicenseClient,
    ILicensePublicKeyService publicKeyService,
    IOptions<DebuggingOptions> debuggingOptions,
    ILogger<LicenseService> logger) : ILicenseInfoProvider
{
    private static readonly TimeSpan CacheDuration = TimeSpan.FromHours(8);
    private const string CacheKeyPrefix = "license_info_";

    public async Task<LicenseInfo> GetLicenseInfoAsync(Guid organizationId)
    {
        if (debuggingOptions.Value.ForceEnterpriseLicenseWhenDebuggerAttached && Debugger.IsAttached)
        {
            return new LicenseInfo
            {
                Tier = Tier.Enterprise,
                IsValid = true,
                MaxModules = null,
                ExpiryDate = DateTime.UtcNow.AddYears(1),
                LicensePeriodEnd = DateTime.UtcNow.AddYears(1),
                SubscriptionId = Guid.Empty
            };
        }

        var cacheKey = $"{CacheKeyPrefix}{organizationId}";

        if (memoryCache.TryGetValue(cacheKey, out LicenseInfo? cached) && cached != null)
        {
            return cached;
        }

        await using var dbContext = await dbContextFactory.CreateDbContextAsync();
        var licenseToken = await dbContext.Set<SelfHostedOrganizationLicense>()
            .Where(l => l.OrganizationId == organizationId)
            .Select(l => l.LicenseToken)
            .FirstOrDefaultAsync();

        var info = await ValidateLicenseTokenAsync(licenseToken, organizationId);

        memoryCache.Set(cacheKey, info, CacheDuration);

        return info;
    }

    public async Task<LicenseInfo> ValidateAndSaveLicenseKeyAsync(Guid organizationId, string licenseToken)
    {
        var info = await ValidateLicenseTokenAsync(licenseToken, organizationId);

        if (!info.IsValid)
        {
            return info;
        }

        await using var dbContext = await dbContextFactory.CreateDbContextAsync();
        var orgExists = await dbContext.Organizations.AnyAsync(o => o.Id == organizationId);

        if (!orgExists)
        {
            return LicenseInfo.Unlicensed("Organization not found.");
        }

        await UpsertLicenseAsync(dbContext, organizationId, license =>
        {
            license.LicenseToken = licenseToken;
            license.SelfHostedSubscriptionId = info.SubscriptionId;
        });

        EvictCache(organizationId);
        memoryCache.Set($"{CacheKeyPrefix}{organizationId}", info, CacheDuration);

        return info;
    }

    public async Task<LicenseInfo> SaveIssuedLicenseAsync(Guid organizationId, string opaqueKey, string jwt)
    {
        var info = await ValidateLicenseTokenAsync(jwt, organizationId);
        if (!info.IsValid) return info;

        await using var dbContext = await dbContextFactory.CreateDbContextAsync();
        var orgExists = await dbContext.Organizations.AnyAsync(o => o.Id == organizationId);

        if (!orgExists)
        {
            return LicenseInfo.Unlicensed("Organization not found.");
        }

        await UpsertLicenseAsync(dbContext, organizationId, license =>
        {
            license.LicenseToken = jwt;
            license.SelfHostedLicenseKey = opaqueKey;
            license.SelfHostedSubscriptionId = info.SubscriptionId;
        });

        EvictCache(organizationId);
        memoryCache.Set($"{CacheKeyPrefix}{organizationId}", info, CacheDuration);

        return info;
    }

    public async Task<LicenseInfo> RefreshFromSaaSAsync(Guid organizationId)
    {
        await using var dbContext = await dbContextFactory.CreateDbContextAsync();
        var row = await dbContext.Set<SelfHostedOrganizationLicense>()
            .FirstOrDefaultAsync(l => l.OrganizationId == organizationId);

        if (row is null || string.IsNullOrEmpty(row.SelfHostedLicenseKey))
        {
            return LicenseInfo.Unlicensed("No license key on file to refresh.");
        }

        var refreshed = await remoteLicenseClient.RefreshAsync(row.SelfHostedLicenseKey, row.LicenseToken);
        if (refreshed is null || string.IsNullOrWhiteSpace(refreshed.Token))
        {
            return LicenseInfo.Unlicensed("SaaS did not return a refreshed token.");
        }

        return await ValidateAndSaveLicenseKeyAsync(organizationId, refreshed.Token);
    }

    public async Task RemoveLicenseKeyAsync(Guid organizationId)
    {
        await using var dbContext = await dbContextFactory.CreateDbContextAsync();
        var license = await dbContext.Set<SelfHostedOrganizationLicense>()
            .FirstOrDefaultAsync(l => l.OrganizationId == organizationId);

        if (license != null)
        {
            dbContext.Set<SelfHostedOrganizationLicense>().Remove(license);
            await dbContext.SaveChangesAsync();
        }

        EvictCache(organizationId);
    }

    private static async Task UpsertLicenseAsync(
        SnapCdDbContext dbContext,
        Guid organizationId,
        Action<SelfHostedOrganizationLicense> mutate)
    {
        var set = dbContext.Set<SelfHostedOrganizationLicense>();
        var license = await set.FirstOrDefaultAsync(l => l.OrganizationId == organizationId);
        var isNew = license is null;
        license ??= new SelfHostedOrganizationLicense { OrganizationId = organizationId };

        mutate(license);

        if (isNew) set.Add(license);
        await dbContext.SaveChangesAsync();
    }

    private void EvictCache(Guid organizationId)
    {
        memoryCache.Remove($"{CacheKeyPrefix}{organizationId}");
    }

    private async Task<LicenseInfo> ValidateLicenseTokenAsync(string? licenseToken, Guid organizationId)
    {
        if (string.IsNullOrWhiteSpace(licenseToken))
        {
            return LicenseInfo.Unlicensed();
        }

        var pem = await publicKeyService.GetAsync();
        var result = TryValidate(licenseToken, pem, out var signatureFailure);
        if (result != null) return result;

        if (signatureFailure)
        {
            logger.LogInformation("License signature validation failed; refreshing public key from snapcd.io");
            var refreshedPem = await publicKeyService.RefreshFromRemoteAsync();
            if (!string.IsNullOrWhiteSpace(refreshedPem)
                && !string.Equals(refreshedPem, pem, StringComparison.Ordinal))
            {
                var retry = TryValidate(licenseToken, refreshedPem, out _);
                if (retry != null) return retry;
            }
        }

        return LicenseInfo.Unlicensed("License key is invalid.");
    }

    private LicenseInfo? TryValidate(string licenseToken, string? pem, out bool signatureFailure)
    {
        signatureFailure = false;

        if (string.IsNullOrWhiteSpace(pem))
        {
            logger.LogWarning("License verification public key is not available");
            return LicenseInfo.Unlicensed("License verification public key is not available.");
        }

        try
        {
            var rsa = RSA.Create();
            rsa.ImportFromPem(pem);
            var rsaKey = new RsaSecurityKey(rsa);

            var validationParameters = new TokenValidationParameters
            {
                ValidateIssuerSigningKey = true,
                IssuerSigningKey = rsaKey,
                ValidateIssuer = false,
                ValidateAudience = false,
                ValidateLifetime = true,
                ClockSkew = TimeSpan.FromMinutes(5)
            };

            var handler = new JwtSecurityTokenHandler();
            handler.ValidateToken(licenseToken, validationParameters, out var validatedToken);

            var jwt = (JwtSecurityToken)validatedToken;

            var subClaim = jwt.Claims.FirstOrDefault(c => c.Type == "sub")?.Value;
            if (!Guid.TryParse(subClaim, out var subscriptionId))
            {
                return LicenseInfo.Unlicensed("License key is missing a valid subject claim.");
            }

            var tierClaim = jwt.Claims.FirstOrDefault(c => c.Type == "tier")?.Value;
            var tier = TierExtensions.FromClaimValue(tierClaim);
            if (tier is null)
            {
                return LicenseInfo.Unlicensed("License key is missing a valid tier claim.");
            }

            var maxModulesClaim = jwt.Claims.FirstOrDefault(c => c.Type == "max_modules")?.Value;
            int? maxModules = null;
            if (int.TryParse(maxModulesClaim, out var parsedModules))
            {
                maxModules = parsedModules;
            }

            var licensePeriodEndClaim = jwt.Claims.FirstOrDefault(c => c.Type == "license_period_end")?.Value;
            DateTime? licensePeriodEnd = null;
            if (long.TryParse(licensePeriodEndClaim, out var unixSeconds))
            {
                licensePeriodEnd = DateTimeOffset.FromUnixTimeSeconds(unixSeconds).UtcDateTime;
            }

            return new LicenseInfo
            {
                Tier = tier.Value,
                IsValid = true,
                MaxModules = maxModules,
                ExpiryDate = jwt.ValidTo,
                LicensePeriodEnd = licensePeriodEnd,
                SubscriptionId = subscriptionId
            };
        }
        catch (SecurityTokenExpiredException)
        {
            return LicenseInfo.Unlicensed("License key has expired.");
        }
        catch (SecurityTokenSignatureKeyNotFoundException)
        {
            signatureFailure = true;
            return null;
        }
        catch (SecurityTokenInvalidSignatureException)
        {
            signatureFailure = true;
            return null;
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "License key validation failed");
            return LicenseInfo.Unlicensed("License key is invalid.");
        }
    }
}
