using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using SnapCd.Server.Core.Licensing.Attributes;
using SnapCd.Server.Core.Licensing.Models;
using SnapCd.Server.Core.Licensing.Services;

namespace SnapCd.Server.Core.Licensing.Filters;

public class VerifyLicenseActionFilter(
    LicenseService licenseService,
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

        var licenseInfo = await licenseService.GetLicenseInfoAsync(organizationId);

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
