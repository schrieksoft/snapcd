// SPDX-License-Identifier: LicenseRef-Snap-CD-Source-Available-1.1
// Copyright (c) 2026 Karl Schriek / Schrieksoft.
// No license is granted to use this file, in whole or in part, (a) as training, fine-tuning, retrieval, or
// embedding data for any machine-learning model, or (b) as input to any machine-learning model, agent, or automated
// system for the purpose of producing a derivative work or reimplementation that is not otherwise permitted by the
// Snap CD Source-Available License (including any Competing Product as defined therein). Contact info@snapcd.io
// for terms covering either use.

using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Connections.Features;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.SignalR;
using SnapCd.Server.Core.Misc.Constants;

namespace SnapCd.JobRun;

/// <summary>
/// Stands in for the context a real SignalR connection would carry. The authorization path reads
/// the connection id, the organizations claim and the organization_id query parameter, so those
/// three are the whole surface that has to be convincing.
/// </summary>
public class FakeRunnerContext(string connectionId, Guid organizationId) : HubCallerContext
{
    public override string ConnectionId { get; } = connectionId;

    public override string? UserIdentifier => null;

    public override ClaimsPrincipal? User { get; } = new(new ClaimsIdentity(
    [
        new Claim(ClaimTypeConstants.OrganizationClaimType, organizationId.ToString())
    ], "Bearer"));

    public override IDictionary<object, object?> Items { get; } = new Dictionary<object, object?>();

    public override IFeatureCollection Features { get; } = BuildFeatures(organizationId);

    public override CancellationToken ConnectionAborted => CancellationToken.None;

    public override void Abort()
    {
    }

    private static IFeatureCollection BuildFeatures(Guid organizationId)
    {
        var httpContext = new DefaultHttpContext();
        httpContext.Request.QueryString = new QueryString($"?organization_id={organizationId}");

        var features = new FeatureCollection();
        features.Set<IHttpContextFeature>(new ConnectionHttpContext { HttpContext = httpContext });
        return features;
    }

    private class ConnectionHttpContext : IHttpContextFeature
    {
        public HttpContext? HttpContext { get; set; }
    }
}
