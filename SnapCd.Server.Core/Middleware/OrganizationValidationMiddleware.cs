// SPDX-License-Identifier: LicenseRef-Snap-CD-Source-Available-1.1
// Copyright (c) 2026 Karl Schriek / Schrieksoft.
// No license is granted to use this file, in whole or in part, (a) as training, fine-tuning, retrieval, or
// embedding data for any machine-learning model, or (b) as input to any machine-learning model, agent, or automated
// system for the purpose of producing a derivative work or reimplementation that is not otherwise permitted by the
// Snap CD Source-Available License (including any Competing Product as defined therein). Contact info@snapcd.io
// for terms covering either use.

using System.Security.Claims;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Endpoints;
using SnapCd.Server.Core.Services.Edition;
using SnapCd.Server.Core.Services.OrganizationContext;

namespace SnapCd.Server.Core.Middleware;

public class OrganizationValidationMiddleware
{
    private readonly RequestDelegate _next;

    public OrganizationValidationMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task InvokeAsync(
        HttpContext context,
        OrganizationMembershipCacheService membershipCache,
        IOrganizationCountValidator organizationCountValidator)
    {
        // Check runtime organization limit
        if (await organizationCountValidator.IsOverLimitAsync())
        {
            context.Response.StatusCode = 503;
            context.Response.ContentType = "text/plain";
            await context.Response.WriteAsync(
                "Service unavailable: organization limit exceeded. Remove extra organizations and restart.");
            return;
        }

        if (!RequiresOrganizationValidation(context))
        {
            await _next(context);
            return;
        }

        var cookieValue = context.Request.Cookies[OrganizationContext.CookieName];

        if (!Guid.TryParse(cookieValue, out var organizationId) || organizationId == Guid.Empty)
        {
            RedirectToSelectOrganization(context);
            return;
        }

        var userIdClaim = context.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (Guid.TryParse(userIdClaim, out var userId))
        {
            var isValid = await membershipCache.IsActiveMemberAsync(userId, organizationId);
            if (!isValid)
            {
                RedirectToSelectOrganization(context);
                return;
            }
        }

        await _next(context);
    }

    private static bool RequiresOrganizationValidation(HttpContext context)
    {
        if (context.User.Identity?.IsAuthenticated != true)
            return false;

        var componentType = context.GetEndpoint()?.Metadata
            .GetMetadata<ComponentTypeMetadata>()?.Type;

        if (componentType == null)
            return false;

        var layoutAttr = componentType
            .GetCustomAttributes(typeof(LayoutAttribute), true)
            .OfType<LayoutAttribute>()
            .FirstOrDefault();

        return layoutAttr?.LayoutType?.Name == "OrganizationMainLayout";
    }

    private static void RedirectToSelectOrganization(HttpContext context)
    {
        var path = context.Request.Path + context.Request.QueryString;
        var returnUrl = Uri.EscapeDataString(path);
        context.Response.Redirect($"/Account/SelectOrganization?returnUrl={returnUrl}");
    }
}
