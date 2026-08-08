// SPDX-License-Identifier: LicenseRef-Snap-CD-Source-Available-1.1
// Copyright (c) 2026 Karl Schriek / Schrieksoft.
// No license is granted to use this file, in whole or in part, (a) as training, fine-tuning, retrieval, or
// embedding data for any machine-learning model, or (b) as input to any machine-learning model, agent, or automated
// system for the purpose of producing a derivative work or reimplementation that is not otherwise permitted by the
// Snap CD Source-Available License (including any Competing Product as defined therein). Contact info@snapcd.io
// for terms covering either use.

using SnapCd.Server.Core.Services.MaintenanceMode;

namespace SnapCd.Server.Core.Middleware;

/// <summary>
/// Courtesy layer, not the enforcement point: gives API clients one clear 503 with Retry-After
/// during a maintenance window instead of a scatter of per-write refusals. Only API routes are
/// refused — the UI stays reachable (its writes hit the repository gate), the hubs and the
/// state backend stay open for running jobs, and health checks keep answering.
/// </summary>
public class MaintenanceModeMiddleware
{
    private readonly RequestDelegate _next;
    private readonly IMaintenanceModeService _maintenanceMode;

    public MaintenanceModeMiddleware(RequestDelegate next, IMaintenanceModeService maintenanceMode)
    {
        _next = next;
        _maintenanceMode = maintenanceMode;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        if (ShouldRefuse(context.Request.Path) && await _maintenanceMode.IsActiveAsync())
        {
            context.Response.StatusCode = StatusCodes.Status503ServiceUnavailable;
            context.Response.Headers.RetryAfter = "120";
            await context.Response.WriteAsync("A maintenance window is open. Retry after it closes.");
            return;
        }

        await _next(context);
    }

    public static bool ShouldRefuse(PathString path)
    {
        if (!path.StartsWithSegments("/api")) return false;
        if (path.StartsWithSegments("/api/state")) return false;

        // Organization-scoped state backend: /api/{organizationId:guid}/state/...
        var segments = path.Value!.Split('/', StringSplitOptions.RemoveEmptyEntries);
        if (segments.Length >= 3 && Guid.TryParse(segments[1], out _) && segments[2].Equals("state", StringComparison.OrdinalIgnoreCase))
            return false;

        return true;
    }
}
