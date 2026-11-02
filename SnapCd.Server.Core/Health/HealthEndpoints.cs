// SPDX-License-Identifier: LicenseRef-Snap-CD-Source-Available-1.1
// Copyright (c) 2026 Karl Schriek / Schrieksoft.
// No license is granted to use this file, in whole or in part, (a) as training, fine-tuning, retrieval, or
// embedding data for any machine-learning model, or (b) as input to any machine-learning model, agent, or automated
// system for the purpose of producing a derivative work or reimplementation that is not otherwise permitted by the
// Snap CD Source-Available License (including any Competing Product as defined therein). Contact info@snapcd.io
// for terms covering either use.

using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace SnapCd.Server.Core.Health;

public static class HealthEndpoints
{
    /// <summary>Liveness runs no checks; readiness, and <c>/health</c>, run them once the bus has started.</summary>
    public static IEndpointRouteBuilder MapSnapCdHealthChecks(this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapHealthChecks("/health/live", new HealthCheckOptions { Predicate = _ => false });
        endpoints.MapGet("/health/ready", Ready);
        endpoints.MapGet("/health", Ready);
        return endpoints;
    }

    public static async Task<IResult> Ready(BusStartupState busStartup, HealthCheckService healthChecks, CancellationToken cancellationToken)
    {
        // The health check service logs every unhealthy check at Error; a bus that is still starting is not one.
        if (!busStartup.HasStarted)
            return Results.Text("Starting", "text/plain", statusCode: StatusCodes.Status503ServiceUnavailable);

        var report = await healthChecks.CheckHealthAsync(cancellationToken);
        var statusCode = report.Status == HealthStatus.Unhealthy
            ? StatusCodes.Status503ServiceUnavailable
            : StatusCodes.Status200OK;
        return Results.Text(report.Status.ToString(), "text/plain", statusCode: statusCode);
    }
}
