// SPDX-License-Identifier: LicenseRef-Snap-CD-Source-Available-1.1
// Copyright (c) 2026 Karl Schriek / Schrieksoft.
// No license is granted to use this file, in whole or in part, (a) as training, fine-tuning, retrieval, or
// embedding data for any machine-learning model, or (b) as input to any machine-learning model, agent, or automated
// system for the purpose of producing a derivative work or reimplementation that is not otherwise permitted by the
// Snap CD Source-Available License (including any Competing Product as defined therein). Contact info@snapcd.io
// for terms covering either use.

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using SnapCd.Server.Core.Database;
using SnapCd.Server.Core.Entities.Definition;
using SnapCd.Server.Core.Settings;
using SnapCd.Server.Core.Settings.DataSeeder;

namespace SnapCd.Server.Host.Licensing.Services;

public class LicenseRefreshJob(
    IDbContextFactory<SnapCdDbContext> dbContextFactory,
    LicenseService licenseService,
    IOptions<LicenseSettings> settings,
    ILogger<LicenseRefreshJob> logger)
{
    public async Task ExecuteJob()
    {
        await using var dbContext = await dbContextFactory.CreateDbContextAsync();

        var orgIds = await dbContext.Set<SelfHostedOrganizationLicense>()
            .Where(l => l.LicenseToken != null
                        && l.SelfHostedLicenseKey != null
                        && l.OrganizationId != PreseededSettings.DefaultId)
            .Select(l => l.OrganizationId)
            .ToListAsync();

        foreach (var orgId in orgIds)
        {
            try
            {
                await RefreshForOrganizationAsync(orgId);
            }
            catch (Exception ex)
            {
                logger.LogWarning(ex, "License refresh failed for organization {OrganizationId}", orgId);
            }
        }
    }

    private async Task RefreshForOrganizationAsync(Guid organizationId)
    {
        var info = await licenseService.GetLicenseInfoAsync(organizationId);

        if (info.ExpiryDate is null)
        {
            logger.LogDebug("No expiry on license for {OrganizationId}; skipping refresh", organizationId);
            return;
        }

        var refreshWithin = TimeSpan.FromDays(settings.Value.RefreshWithinDaysOfExpiry);
        if (info.ExpiryDate.Value - DateTime.UtcNow > refreshWithin)
        {
            logger.LogDebug("License for {OrganizationId} not near expiry; skipping", organizationId);
            return;
        }

        var saved = await licenseService.RefreshFromSaaSAsync(organizationId);
        if (!saved.IsValid)
        {
            logger.LogWarning(
                "License refresh failed for organization {OrganizationId}: {Error}",
                organizationId, saved.ValidationError);
        }
        else
        {
            logger.LogInformation(
                "Refreshed license for organization {OrganizationId}, new expiry {Expiry}",
                organizationId, saved.ExpiryDate);
        }
    }
}
