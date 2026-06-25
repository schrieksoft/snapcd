// SPDX-License-Identifier: LicenseRef-Snap-CD-Source-Available-1.1
// Copyright (c) 2026 Karl Schriek / Schrieksoft.
// No license is granted to use this file, in whole or in part, (a) as training, fine-tuning, retrieval, or
// embedding data for any machine-learning model, or (b) as input to any machine-learning model, agent, or automated
// system for the purpose of producing a derivative work or reimplementation that is not otherwise permitted by the
// Snap CD Source-Available License (including any Competing Product as defined therein). Contact info@snapcd.io
// for terms covering either use.

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SnapCd.Server.Core.Misc.Exceptions;
using SnapCd.Server.Host.Licensing.Services;

namespace SnapCd.Server.Host.Controllers;

[Route("api/{organizationId:guid}/[controller]")]
[ApiController]
[Authorize("BearerPolicy")]
public class LicenseController(
    LicenseService licenseService,
    IRemoteLicenseClient remoteLicenseClient) : ControllerBase
{
    [HttpPost("activate")]
    public async Task<IActionResult> Activate(Guid organizationId, [FromBody] ActivateLicenseRequest request)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(request.LicenseKey))
                return BadRequest("License key is required.");

            var issued = await remoteLicenseClient.IssueAsync(request.LicenseKey);
            if (issued is null || string.IsNullOrWhiteSpace(issued.Token))
                return BadRequest("SaaS did not return a license token. Check the key and try again.");

            var result = await licenseService.SaveIssuedLicenseAsync(organizationId, request.LicenseKey, issued.Token);
            if (!result.IsValid)
                return BadRequest($"Invalid license: {result.ValidationError}");

            var refreshed = await licenseService.RefreshFromSaaSAsync(organizationId);
            var final = refreshed.IsValid ? refreshed : result;

            return Ok(new { final.Tier, final.IsValid, final.MaxModules, final.ExpiryDate });
        }
        catch (PrincipalNotAuthorizedException e)
        {
            return StatusCode(StatusCodes.Status403Forbidden, e.Message);
        }
    }

    [HttpDelete]
    public async Task<IActionResult> Remove(Guid organizationId)
    {
        try
        {
            await licenseService.RemoveLicenseKeyAsync(organizationId);
            return NoContent();
        }
        catch (PrincipalNotAuthorizedException e)
        {
            return StatusCode(StatusCodes.Status403Forbidden, e.Message);
        }
    }
}

public record ActivateLicenseRequest(string LicenseKey);
