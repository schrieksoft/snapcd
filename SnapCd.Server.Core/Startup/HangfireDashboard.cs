// SPDX-License-Identifier: LicenseRef-Snap-CD-Source-Available-1.1
// Copyright (c) 2026 Karl Schriek / Schrieksoft.
// No license is granted to use this file, in whole or in part, (a) as training, fine-tuning, retrieval, or
// embedding data for any machine-learning model, or (b) as input to any machine-learning model, agent, or automated
// system for the purpose of producing a derivative work or reimplementation that is not otherwise permitted by the
// Snap CD Source-Available License (including any Competing Product as defined therein). Contact info@snapcd.io
// for terms covering either use.

using System.Reflection;
using Hangfire;
using Hangfire.Dashboard;
using Microsoft.AspNetCore.Builder;

namespace SnapCd.Server.Core.Startup;

/// <summary>
/// Mounts the background jobs dashboard: what each job has done, what is queued, and a way to run
/// one by hand.
///
/// Hangfire keeps all of this already, so the work here is making it reachable by the right people
/// and making it look like the rest of the product.
/// </summary>
public static class HangfireDashboard
{
    /// <summary>Where the dashboard is served from, and what the admin section links to.</summary>
    public const string Path = "/AdminCenter/BackgroundJobs";

    public static void UseSnapCdHangfireDashboard(this IApplicationBuilder app)
    {
        DashboardRoutes.AddStylesheet(
            typeof(HangfireDashboard).Assembly,
            "SnapCd.Server.Core.Startup.Hangfire.hangfire-snapcd.css");

        app.UseHangfireDashboard(Path, new DashboardOptions
        {
            DashboardTitle = "Background Jobs",

            // Hangfire's default allows local requests only, so without this the dashboard is
            // unreachable on a real deployment.
            AsyncAuthorization = [new HangfireDashboardAuthorization()],

            // Where the "back to site" link goes.
            AppPath = "/AdminCenter"
        });
    }
}
