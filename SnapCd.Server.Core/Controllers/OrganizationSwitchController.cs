// SPDX-License-Identifier: LicenseRef-Snap-CD-Source-Available-1.1
// Copyright (c) 2026 Karl Schriek / Schrieksoft.
// No license is granted to use this file, in whole or in part, (a) as training, fine-tuning, retrieval, or
// embedding data for any machine-learning model, or (b) as input to any machine-learning model, agent, or automated
// system for the purpose of producing a derivative work or reimplementation that is not otherwise permitted by the
// Snap CD Source-Available License (including any Competing Product as defined therein). Contact info@snapcd.io
// for terms covering either use.

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using SnapCd.Server.Core.Entities.Definition;
using SnapCd.Server.Core.Misc.Attributes;
using SnapCd.Server.Core.Services.OrganizationContext;

namespace SnapCd.Server.Core.Controllers;

[ApiController]
[Route("api/organization")]
[Authorize]
[PermissionSource(Skip = true,
    Notes = "Session helper: requires only membership of the target organization.")]
public class OrganizationSwitchController : ControllerBase
{
    private readonly UserManager<User> _userManager;
    private readonly OrganizationMembershipCacheService _membershipCache;
    private readonly ILogger<OrganizationSwitchController> _logger;

    public OrganizationSwitchController(
        UserManager<User> userManager,
        OrganizationMembershipCacheService membershipCache,
        ILogger<OrganizationSwitchController> logger)
    {
        _userManager = userManager;
        _membershipCache = membershipCache;
        _logger = logger;
    }

    [HttpGet("switch/{organizationId:guid}")]
    public async Task<IActionResult> SwitchOrganization(Guid organizationId, [FromQuery] string? returnUrl = null)
    {
        var user = await _userManager.GetUserAsync(User);
        if (user == null)
        {
            return Redirect("/Account/Login");
        }

        // Validate user has access to the organization
        var hasAccess = await _membershipCache.IsActiveMemberAsync(user.Id, organizationId);

        if (!hasAccess)
        {
            _logger.LogWarning("User {UserId} attempted to switch to organization {OrganizationId} without access",
                user.Id, organizationId);
            return Redirect("/Organizations");
        }

        // Set the cookie
        OrganizationContext.SetOrganizationCookie(Response, organizationId);

        _logger.LogInformation("User {UserId} switched to organization {OrganizationId}",
            user.Id, organizationId);

        // Pairs with the organization gate's Debug line: this is what was sent, that is what
        // came back. The attributes are the difference when a cookie is written but not stored.
        _logger.LogDebug("Set organization cookie: {SetCookie}",
            string.Join(" | ", Response.Headers.SetCookie.ToArray()));

        // Redirect to the destination
        var redirectTo = Url.IsLocalUrl(returnUrl) ? returnUrl! : "/Dashboard";

        return Redirect(redirectTo);
    }

    [HttpPost("clear")]
    public async Task<IActionResult> ClearOrganization()
    {
        var user = await _userManager.GetUserAsync(User);
        if (user == null)
        {
            return Unauthorized();
        }

        OrganizationContext.ClearOrganizationCookie(Response);

        _logger.LogInformation("User {UserId} cleared organization context", user.Id);

        return Ok(new { redirectUrl = "/Organizations" });
    }
}
