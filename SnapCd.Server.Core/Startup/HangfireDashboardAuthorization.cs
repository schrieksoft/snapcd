// SPDX-License-Identifier: LicenseRef-Snap-CD-Source-Available-1.1
// Copyright (c) 2026 Karl Schriek / Schrieksoft.
// No license is granted to use this file, in whole or in part, (a) as training, fine-tuning, retrieval, or
// embedding data for any machine-learning model, or (b) as input to any machine-learning model, agent, or automated
// system for the purpose of producing a derivative work or reimplementation that is not otherwise permitted by the
// Snap CD Source-Available License (including any Competing Product as defined therein). Contact info@snapcd.io
// for terms covering either use.

using Hangfire.Annotations;
using Hangfire.Dashboard;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using SnapCd.Server.Core.Services.Nonsecured.Admin;

namespace SnapCd.Server.Core.Startup;

/// <summary>
/// Lets a system administrator see the background jobs dashboard, and nobody else.
///
/// Hangfire's own default allows local requests only, which means the dashboard is unreachable on
/// any real deployment. This applies the check the other administration pages use instead.
/// </summary>
public class HangfireDashboardAuthorization : IDashboardAsyncAuthorizationFilter
{
    public Task<bool> AuthorizeAsync([NotNull] DashboardContext context) =>
        IsAllowed(context.GetHttpContext());

    /// <summary>
    /// Whether this request may see the dashboard. Separate from the filter so the decision can be
    /// tested without an ASP.NET dashboard context, which Hangfire does not make constructible.
    /// </summary>
    public static async Task<bool> IsAllowed(HttpContext? httpContext)
    {
        if (httpContext?.User.Identity?.IsAuthenticated != true) return false;

        using var systemRoles = httpContext.RequestServices
            .GetRequiredService<SystemRoleServiceFactory>()
            .Create();

        return await systemRoles.CurrentUserIsSystemAdministratorAsync();
    }
}
