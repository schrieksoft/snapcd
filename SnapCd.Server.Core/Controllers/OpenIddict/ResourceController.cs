// SPDX-License-Identifier: LicenseRef-Snap-CD-Source-Available-1.1
// Copyright (c) 2026 Karl Schriek / Schrieksoft.
// No license is granted to use this file, in whole or in part, (a) as training, fine-tuning, retrieval, or
// embedding data for any machine-learning model, or (b) as input to any machine-learning model, agent, or automated
// system for the purpose of producing a derivative work or reimplementation that is not otherwise permitted by the
// Snap CD Source-Available License (including any Competing Product as defined therein). Contact info@snapcd.io
// for terms covering either use.

using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using OpenIddict.Abstractions;
using OpenIddict.Validation.AspNetCore;
using SnapCd.Server.Core.Entities.Definition;
using static OpenIddict.Abstractions.OpenIddictConstants;

namespace SnapCd.Server.Core.Controllers.OpenIddict;

[Route("api")]
[ApiExplorerSettings(IgnoreApi = true)]
public class ResourceController : Controller
{
    private readonly UserManager<User> _userManager;

    public ResourceController(UserManager<User> userManager)
    {
        _userManager = userManager;
    }

    [Authorize(AuthenticationSchemes = OpenIddictValidationAspNetCoreDefaults.AuthenticationScheme)]
    [HttpGet("message")]
    public async Task<IActionResult> GetMessage()
    {
        var subject = User.GetClaim(Claims.Subject);

        if (subject == null)
            throw new Exception("subject is empty");

        var user = await _userManager.FindByIdAsync(subject);
        if (user is null)
        {
            var d = new Dictionary<string, string?>
            {
                [OpenIddictValidationAspNetCoreConstants.Properties.Error] = Errors.InvalidToken,
                [OpenIddictValidationAspNetCoreConstants.Properties.ErrorDescription] =
                    "The specified access token is bound to an account that no longer exists."
            };

            return Challenge(
                authenticationSchemes: OpenIddictValidationAspNetCoreDefaults.AuthenticationScheme,
                properties: new AuthenticationProperties(d)
            );
        }

        return Content($"{user.UserName} has been successfully authenticated.");
    }
}