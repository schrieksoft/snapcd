// SPDX-License-Identifier: LicenseRef-Snap-CD-Source-Available-1.1
// Copyright (c) 2026 Karl Schriek / Schrieksoft.
// No license is granted to use this file, in whole or in part, (a) as training, fine-tuning, retrieval, or
// embedding data for any machine-learning model, or (b) as input to any machine-learning model, agent, or automated
// system for the purpose of producing a derivative work or reimplementation that is not otherwise permitted by the
// Snap CD Source-Available License (including any Competing Product as defined therein). Contact info@snapcd.io
// for terms covering either use.

using System.Text.Json.Nodes;
using Microsoft.AspNetCore.OpenApi;
using Microsoft.OpenApi;
using SnapCd.Server.Core.Services.OrganizationContext;

namespace SnapCd.Server.Core.Startup;

/// <summary>
/// The OpenAPI document is generated per request, so the caller's current
/// organization (org cookie) can be stamped as the example on every
/// {organizationId} parameter — Scalar prefills path parameters from the example.
/// </summary>
public class CurrentOrganizationOperationTransformer : IOpenApiOperationTransformer
{
    private readonly IHttpContextAccessor _httpContextAccessor;

    public CurrentOrganizationOperationTransformer(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor;
    }

    public Task TransformAsync(OpenApiOperation operation, OpenApiOperationTransformerContext context,
        CancellationToken cancellationToken)
    {
        var cookie = _httpContextAccessor.HttpContext?.Request.Cookies[OrganizationContext.CookieName];
        if (!Guid.TryParse(cookie, out var organizationId) || operation.Parameters is null)
            return Task.CompletedTask;

        foreach (var parameter in operation.Parameters)
        {
            if (string.Equals(parameter.Name, "organizationId", StringComparison.OrdinalIgnoreCase)
                && parameter is OpenApiParameter concrete)
            {
                concrete.Example = JsonValue.Create(organizationId.ToString());
            }
        }

        return Task.CompletedTask;
    }
}
