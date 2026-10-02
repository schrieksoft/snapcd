// SPDX-License-Identifier: LicenseRef-Snap-CD-Source-Available-1.1
// Copyright (c) 2026 Karl Schriek / Schrieksoft.
// No license is granted to use this file, in whole or in part, (a) as training, fine-tuning, retrieval, or
// embedding data for any machine-learning model, or (b) as input to any machine-learning model, agent, or automated
// system for the purpose of producing a derivative work or reimplementation that is not otherwise permitted by the
// Snap CD Source-Available License (including any Competing Product as defined therein). Contact info@snapcd.io
// for terms covering either use.

using SnapCd.Server.Core.Licensing.Models;
using SnapCd.Server.Core.Licensing.Services;
using SnapCd.Server.Host.Licensing.Services;

namespace SnapCd.Server.Host.Services;

public class SelfHostedSsoPolicy : ISsoPolicy
{
    public async Task<bool> ShouldEnableSsoAsync(IServiceProvider serviceProvider)
    {
        var logger = serviceProvider.GetService<ILoggerFactory>()?.CreateLogger<SelfHostedSsoPolicy>();

        try
        {
            using var scope = serviceProvider.CreateScope();

            var orgIdProvider = scope.ServiceProvider.GetRequiredService<SelfHostedOrganizationIdProvider>();
            var orgId = await orgIdProvider.GetOrganizationIdAsync();
            if (orgId is null)
            {
                logger?.LogWarning(
                    "The external login providers configured under OpenIdConnect:ExternalLoginProviders "
                    + "are disabled: no organization could be read from the database, so the licence "
                    + "that enables them could not be checked. On a first start this is expected - "
                    + "finish setting the server up, enter the licence key, and restart.");
                return false;
            }

            var licenseService = scope.ServiceProvider.GetRequiredService<LicenseService>();
            var licenseInfo = await licenseService.GetLicenseInfoAsync(orgId.Value);

            if (!licenseInfo.Includes(Feature.Sso))
            {
                logger?.LogWarning(
                    "The external login providers configured under OpenIdConnect:ExternalLoginProviders "
                    + "are disabled: the licence on this organization does not include SSO. Enter a "
                    + "licence that does and restart.");
                return false;
            }

            return true;
        }
        catch (Exception ex)
        {
            // Disabling every configured external login provider for the lifetime of the process is
            // not something to do quietly, whatever the cause.
            logger?.LogWarning(ex,
                "The external login providers configured under OpenIdConnect:ExternalLoginProviders "
                + "are disabled: the licence that enables them could not be checked. If the database "
                + "is not reachable yet, restart once it is.");
            return false;
        }
    }
}
