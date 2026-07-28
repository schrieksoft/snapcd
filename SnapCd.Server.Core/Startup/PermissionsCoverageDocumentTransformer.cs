// SPDX-License-Identifier: LicenseRef-Snap-CD-Source-Available-1.1
// Copyright (c) 2026 Karl Schriek / Schrieksoft.
// No license is granted to use this file, in whole or in part, (a) as training, fine-tuning, retrieval, or
// embedding data for any machine-learning model, or (b) as input to any machine-learning model, agent, or automated
// system for the purpose of producing a derivative work or reimplementation that is not otherwise permitted by the
// Snap CD Source-Available License (including any Competing Product as defined therein). Contact info@snapcd.io
// for terms covering either use.

using System.Text.Json.Nodes;
using Microsoft.AspNetCore.Mvc.Controllers;
using Microsoft.AspNetCore.OpenApi;
using Microsoft.Extensions.Logging;
using Microsoft.OpenApi;

namespace SnapCd.Server.Core.Startup;

/// <summary>
/// Emits an x-snapcd-permission-coverage extension on the document (documented and
/// skipped counts). Absence must be a decision, not an accident: an operation that is
/// neither documented nor explicitly skipped fails the headless generator (and with it
/// the pre-commit artifact check) until it resolves to a permission map or carries
/// [PermissionSource(Skip = true)]. The live server only logs a warning — a coverage
/// gap must never take down the running /openapi endpoint.
/// </summary>
public class PermissionsCoverageDocumentTransformer(
    ILogger<PermissionsCoverageDocumentTransformer> logger) : IOpenApiDocumentTransformer
{
    /// <summary>Set by the OpenAPI generator so coverage gaps fail document generation.</summary>
    public static bool Strict { get; set; }

    public Task TransformAsync(OpenApiDocument document, OpenApiDocumentTransformerContext context,
        CancellationToken cancellationToken)
    {
        var documented = 0;
        var skipped = 0;
        var unresolved = new List<string>();

        foreach (var group in context.DescriptionGroups)
        foreach (var description in group.Items)
        {
            if (description.ActionDescriptor is not ControllerActionDescriptor action)
                continue;

            var (_, coverage) = PermissionDocExtractor.ExtractWithCoverage(action);
            switch (coverage)
            {
                case PermissionCoverage.Documented:
                    documented++;
                    break;
                case PermissionCoverage.Skipped:
                    skipped++;
                    break;
                default:
                    unresolved.Add($"{description.HttpMethod} /{description.RelativePath}");
                    break;
            }
        }

        unresolved.Sort(StringComparer.Ordinal);
        if (unresolved.Count > 0)
        {
            var message = "Operations without permission documentation and without an explicit " +
                          "[PermissionSource(Skip = true)]:\n  " + string.Join("\n  ", unresolved);
            if (Strict)
                throw new InvalidOperationException(message);
            logger.LogWarning("Permission coverage gaps: {Message}", message);
        }

        var coverageJson = new JsonObject
        {
            ["documented"] = documented,
            ["skipped"] = skipped
        };

        document.Extensions ??= new Dictionary<string, IOpenApiExtension>();
        document.Extensions["x-snapcd-permission-coverage"] = new JsonNodeExtension(coverageJson);

        return Task.CompletedTask;
    }
}
