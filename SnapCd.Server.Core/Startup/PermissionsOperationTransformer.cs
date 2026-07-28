// SPDX-License-Identifier: LicenseRef-Snap-CD-Source-Available-1.1
// Copyright (c) 2026 Karl Schriek / Schrieksoft.
// No license is granted to use this file, in whole or in part, (a) as training, fine-tuning, retrieval, or
// embedding data for any machine-learning model, or (b) as input to any machine-learning model, agent, or automated
// system for the purpose of producing a derivative work or reimplementation that is not otherwise permitted by the
// Snap CD Source-Available License (including any Competing Product as defined therein). Contact info@snapcd.io
// for terms covering either use.

using System.Text;
using System.Text.Json.Nodes;
using Microsoft.AspNetCore.Mvc.Controllers;
using Microsoft.AspNetCore.OpenApi;
using Microsoft.OpenApi;

namespace SnapCd.Server.Core.Startup;

/// <summary>
/// Documents each operation's required permissions from the secured repository that
/// enforces them (see <see cref="PermissionDocExtractor"/>): a markdown block in the
/// operation description for readers, and an x-snapcd-permissions extension for
/// tooling (/RoleCapabilities, the docs pipeline).
/// </summary>
public class PermissionsOperationTransformer : IOpenApiOperationTransformer
{
    public Task TransformAsync(OpenApiOperation operation, OpenApiOperationTransformerContext context,
        CancellationToken cancellationToken)
    {
        if (context.Description.ActionDescriptor is not ControllerActionDescriptor action)
            return Task.CompletedTask;

        var doc = PermissionDocExtractor.Extract(action);
        if (doc is null) return Task.CompletedTask;

        operation.Description = string.IsNullOrEmpty(operation.Description)
            ? BuildMarkdown(doc)
            : $"{operation.Description}\n\n{BuildMarkdown(doc)}";

        operation.Extensions ??= new Dictionary<string, IOpenApiExtension>();
        operation.Extensions["x-snapcd-permissions"] = new JsonNodeExtension(BuildJson(doc));

        return Task.CompletedTask;
    }

    private static IEnumerable<string> QualifiedRoles(PermissionDoc doc)
    {
        // "Organization.Owner", "Stack.Contributor", ... — dimension order and
        // role sorting come from the extractor, so the output is deterministic.
        return doc.RolesByDimension.SelectMany(kv =>
        {
            var label = Capitalize(kv.Key);
            return kv.Value.Select(role => $"{label}.{role}");
        });
    }

    private static string BuildMarkdown(PermissionDoc doc)
    {
        var builder = new StringBuilder();
        builder.AppendLine("##### Required permissions");
        builder.AppendLine();

        if (doc.RolesByDimension.Count > 0)
        {
            builder.AppendLine("Any of:");
            foreach (var role in QualifiedRoles(doc))
                builder.AppendLine($"- `{role}`");

            foreach (var dimension in doc.ReverseInheritedDimensions)
                builder.AppendLine($"- *any* role on a contained {Capitalize(dimension)} (`{Capitalize(dimension)}.*`)");

            if (!string.IsNullOrEmpty(doc.Notes))
            {
                builder.AppendLine();
                builder.AppendLine(doc.Notes);
            }
        }
        else
        {
            builder.AppendLine(doc.Notes);
        }

        return builder.ToString().TrimEnd();
    }

    private static JsonObject BuildJson(PermissionDoc doc)
    {
        var json = new JsonObject();

        if (doc.Verb is { } verb)
            json["verb"] = verb.ToString();

        if (doc.RolesByDimension.Count > 0)
            json["anyOf"] = new JsonArray(QualifiedRoles(doc).Select(r => (JsonNode)r).ToArray());

        if (!string.IsNullOrEmpty(doc.Notes))
            json["notes"] = doc.Notes;

        if (doc.ReverseInheritedDimensions.Count > 0)
            json["reverseInheritedAnyOf"] = new JsonArray(
                doc.ReverseInheritedDimensions.Select(d => (JsonNode)$"{Capitalize(d)}.*").ToArray());

        return json;
    }

    private static string Capitalize(string dimension)
    {
        return char.ToUpperInvariant(dimension[0]) + dimension[1..];
    }
}
