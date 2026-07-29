// SPDX-License-Identifier: LicenseRef-Snap-CD-Source-Available-1.1
// Copyright (c) 2026 Karl Schriek / Schrieksoft.
// No license is granted to use this file, in whole or in part, (a) as training, fine-tuning, retrieval, or
// embedding data for any machine-learning model, or (b) as input to any machine-learning model, agent, or automated
// system for the purpose of producing a derivative work or reimplementation that is not otherwise permitted by the
// Snap CD Source-Available License (including any Competing Product as defined therein). Contact info@snapcd.io
// for terms covering either use.

using System.Text.Json.Nodes;
using Microsoft.AspNetCore.OpenApi;
using Microsoft.Extensions.Logging;
using Microsoft.OpenApi;

namespace SnapCd.Server.Core.Startup;

/// <summary>
/// Emits an x-snapcd-schema-doc-coverage extension on the document. Every object schema must
/// carry a description (the DTO's class XML summary) and every inline property a description
/// (the property's XML summary); $ref properties are exempt — the referenced schema documents
/// itself. Gaps fail the headless generator (and with it the pre-commit artifact check); the
/// live server only logs a warning. Enum schemas are exempt: their XML summaries do not flow
/// through the ingestion pipeline, so there is no description channel to gate.
/// </summary>
public class SchemaDocsCoverageDocumentTransformer(
    ILogger<SchemaDocsCoverageDocumentTransformer> logger) : IOpenApiDocumentTransformer
{
    /// <summary>Set by the OpenAPI generator so coverage gaps fail document generation.</summary>
    public static bool Strict { get; set; }

    public Task TransformAsync(OpenApiDocument document, OpenApiDocumentTransformerContext context,
        CancellationToken cancellationToken)
    {
        var documented = 0;
        var missing = new List<string>();

        foreach (var (name, schema) in document.Components?.Schemas ?? new Dictionary<string, IOpenApiSchema>())
        {
            if (schema is not OpenApiSchema concrete) continue;
            if (concrete.Properties is null || concrete.Properties.Count == 0) continue;

            if (string.IsNullOrEmpty(concrete.Description))
                missing.Add(name);
            else
                documented++;

            foreach (var (propertyName, property) in concrete.Properties)
            {
                if (property is not OpenApiSchema inline) continue; // $ref: the target schema documents itself
                if (string.IsNullOrEmpty(inline.Description))
                    missing.Add($"{name}.{propertyName}");
                else
                    documented++;
            }
        }

        missing.Sort(StringComparer.Ordinal);
        if (missing.Count > 0)
        {
            var message = "Schemas and properties without an XML doc summary:\n  " +
                          string.Join("\n  ", missing);
            if (Strict)
                throw new InvalidOperationException(message);
            logger.LogWarning("Schema doc coverage gaps: {Message}", message);
        }

        var coverageJson = new JsonObject
        {
            ["documented"] = documented,
            ["missing"] = missing.Count
        };

        document.Extensions ??= new Dictionary<string, IOpenApiExtension>();
        document.Extensions["x-snapcd-schema-doc-coverage"] = new JsonNodeExtension(coverageJson);

        return Task.CompletedTask;
    }
}
