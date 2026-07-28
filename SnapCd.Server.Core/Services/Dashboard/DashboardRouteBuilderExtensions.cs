// SPDX-License-Identifier: LicenseRef-Snap-CD-Source-Available-1.1
// Copyright (c) 2026 Karl Schriek / Schrieksoft.
// No license is granted to use this file, in whole or in part, (a) as training, fine-tuning, retrieval, or
// embedding data for any machine-learning model, or (b) as input to any machine-learning model, agent, or automated
// system for the purpose of producing a derivative work or reimplementation that is not otherwise permitted by the
// Snap CD Source-Available License (including any Competing Product as defined therein). Contact info@snapcd.io
// for terms covering either use.

using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Http.Extensions;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Primitives;
using SnapCd.Server.Core.Entities.Definition;

namespace SnapCd.Server.Core.Services.Dashboard;

public static class DashboardRouteBuilderExtensions
{
    // These endpoints are required by the Identity Razor components defined in the /Components/User/Pages directory of this project.

    public static IEndpointConventionBuilder MapAdditionalFormEndpoints(this IEndpointRouteBuilder endpoints)
    {
        ArgumentNullException.ThrowIfNull(endpoints);

        var themeGroup = endpoints.MapGroup("/Theme");

        themeGroup.MapPost("/ToggleDarkMode", (
            HttpContext context) =>
        {
            var newTheme = ThemeCookie.ColorMode(!ThemeCookie.IsDark(context));

            context.Response.Cookies.Append(ThemeCookie.Name, newTheme, new CookieOptions
            {
                Expires = DateTime.UtcNow.AddYears(1),
                HttpOnly = false,
                IsEssential = true,
                SameSite = SameSiteMode.Lax,
                Secure = context.Request.IsHttps
            });

            var referer = context.Request.Headers.Referer.ToString();
            return Results.Redirect(string.IsNullOrEmpty(referer) ? "/" : referer);
        }).ExcludeFromDescription();

        var consentGroup = endpoints.MapGroup("/Consent");

        consentGroup.MapPost("/Accept", (
            HttpContext context,
            [FromForm] string level) =>
        {
            var validLevels = new[] { "all", "essential", "rejected" };
            var consentLevel = validLevels.Contains(level) ? level : "essential";

            context.Response.Cookies.Append("cookie_consent", consentLevel, new CookieOptions
            {
                Expires = DateTime.UtcNow.AddYears(1),
                HttpOnly = false,
                IsEssential = true,
                SameSite = SameSiteMode.Lax,
                Secure = context.Request.IsHttps
            });

            var referer = context.Request.Headers.Referer.ToString();
            return Results.Redirect(string.IsNullOrEmpty(referer) ? "/" : referer);
        }).ExcludeFromDescription();

        return consentGroup;
    }


    public static IEndpointConventionBuilder MapAdditionalIdentityEndpoints(this IEndpointRouteBuilder endpoints)
    {
        ArgumentNullException.ThrowIfNull(endpoints);

        var accountGroup = endpoints.MapGroup("/Account");

        accountGroup.MapPost("/PerformExternalLogin", (
            HttpContext context,
            [FromServices] SignInManager<User> signInManager,
            [FromForm] string provider,
            [FromForm] string returnUrl,
            [FromForm] string? token,
            [FromForm] bool isRegistration = false) =>
        {
            var queryParams = new List<KeyValuePair<string, StringValues>>
            {
                new("ReturnUrl", returnUrl)
            };

            if (!string.IsNullOrEmpty(token)) queryParams.Add(new KeyValuePair<string, StringValues>("Token", token));

            string redirectPath;
            string actionValue;

            if (isRegistration && !string.IsNullOrEmpty(token))
            {
                // Invitation-flow registration via external login — dedicated page that
                // attaches the login to the pre-created invited User and finalizes the invitation.
                redirectPath = "/Account/CompleteInvitationExternalLogin";
                actionValue = ExternalLoginConstants.RegisterCallbackAction;
            }
            else if (isRegistration)
            {
                redirectPath = "/Account/RegisterExternalLogin";
                actionValue = ExternalLoginConstants.RegisterCallbackAction;
            }
            else
            {
                redirectPath = "/Account/ExternalLogin";
                actionValue = ExternalLoginConstants.LoginCallbackAction;
            }

            queryParams.Add(new KeyValuePair<string, StringValues>("Action", actionValue));

            var redirectUrl = UriHelper.BuildRelative(
                context.Request.PathBase,
                redirectPath,
                QueryString.Create(queryParams));

            var properties = signInManager.ConfigureExternalAuthenticationProperties(provider, redirectUrl);
            return TypedResults.Challenge(properties, [provider]);
        }).ExcludeFromDescription();

        accountGroup.MapPost("/Logout", async (
            ClaimsPrincipal _,
            SignInManager<User> signInManager,
            [FromForm] string? returnUrl) =>
        {
            await signInManager.SignOutAsync();

            // returnUrl comes off the posted form, so it is untrusted: only honour it
            // when it is a rooted path with no authority. A value starting with "//"
            // (or "/\") is protocol-relative and would redirect off-site, and blindly
            // interpolating it into "~/{returnUrl}" produces exactly that.
            var target = "~/";
            if (!string.IsNullOrEmpty(returnUrl)
                && returnUrl.StartsWith('/')
                && !returnUrl.StartsWith("//", StringComparison.Ordinal)
                && !returnUrl.StartsWith("/\\", StringComparison.Ordinal))
            {
                target = $"~{returnUrl}";
            }

            return TypedResults.LocalRedirect(target);
        }).ExcludeFromDescription();

        var manageGroup = accountGroup.MapGroup("/Manage").RequireAuthorization();

        manageGroup.MapPost("/LinkExternalLogin", async (
            HttpContext context,
            [FromServices] SignInManager<User> signInManager,
            [FromForm] string provider) =>
        {
            // Clear the existing external cookie to ensure a clean login process
            await context.SignOutAsync(IdentityConstants.ExternalScheme);

            var redirectUrl = UriHelper.BuildRelative(
                context.Request.PathBase,
                "/Account/AssociateExternalLogin",
                QueryString.Create("Action", ExternalLoginConstants.LinkLoginCallbackAction));

            var properties = signInManager.ConfigureExternalAuthenticationProperties(provider, redirectUrl, signInManager.UserManager.GetUserId(context.User));
            return TypedResults.Challenge(properties, [provider]);
        }).ExcludeFromDescription();


        return accountGroup;
    }
}