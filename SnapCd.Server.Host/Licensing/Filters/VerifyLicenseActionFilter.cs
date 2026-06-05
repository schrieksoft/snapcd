// SPDX-License-Identifier: LicenseRef-Snap-CD-Source-Available-1.1
// Copyright (c) 2026 Karl Schriek / Schrieksoft.
// No license is granted to use this file, in whole or in part, (a) as training, fine-tuning, retrieval, or
// embedding data for any machine-learning model, or (b) as input to any machine-learning model, agent, or automated
// system for the purpose of producing a derivative work or reimplementation that is not otherwise permitted by the
// Snap CD Source-Available License (including any Competing Product as defined therein). Contact info@snapcd.io
// for terms covering either use.

using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using SnapCd.Server.Core.Licensing.Attributes;
using SnapCd.Server.Core.Licensing.Models;
using SnapCd.Server.Core.Licensing.Services;

namespace SnapCd.Server.Host.Licensing.Filters;

public class VerifyLicenseActionFilter(
    ILicenseInfoProvider licenseInfoProvider,
    ILicenseVerificationPolicy licenseVerificationPolicy) : IAsyncActionFilter
{
    public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
    {
        var verifyAttr = context.ActionDescriptor.EndpointMetadata
            .OfType<VerifyLicense>()
            .FirstOrDefault();

        if (verifyAttr == null)
        {
            await next();
            return;
        }

        if (licenseVerificationPolicy.ShouldSkipVerification)
        {
            await next();
            return;
        }

        if (!context.ActionArguments.TryGetValue("organizationId", out var orgIdObj) ||
            orgIdObj is not Guid organizationId)
        {
            context.Result = new ObjectResult("Organization ID is required for license verification.")
                { StatusCode = StatusCodes.Status400BadRequest };
            return;
        }

        var licenseInfo = await licenseInfoProvider.GetLicenseInfoAsync(organizationId);

        if (!licenseInfo.Includes(verifyAttr.Feature))
        {
            context.Result = new ObjectResult(
                $"This feature ({verifyAttr.Feature}) is not included in your current tier.")
                { StatusCode = StatusCodes.Status403Forbidden };
            return;
        }

        await next();
    }
}
