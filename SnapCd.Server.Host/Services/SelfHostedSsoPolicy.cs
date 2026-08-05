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
                logger?.LogInformation(
                    "No organization resolved; external login providers will be disabled.");
                return false;
            }

            var licenseService = scope.ServiceProvider.GetRequiredService<LicenseService>();
            var licenseInfo = await licenseService.GetLicenseInfoAsync(orgId.Value);
            return licenseInfo.Includes(Feature.Sso);
        }
        catch (Exception ex)
        {
            // DB not available (e.g. design-time EF tooling) — default to SSO disabled.
            // Anything else here silently disables every external login provider for the
            // lifetime of the process, so it must not vanish without a trace.
            logger?.LogError(ex,
                "SSO gating check failed; external login providers will be disabled.");
            return false;
        }
    }
}
