// SPDX-License-Identifier: LicenseRef-Snap-CD-Source-Available-1.1
// Copyright (c) 2026 Karl Schriek / Schrieksoft.
// No license is granted to use this file, in whole or in part, (a) as training, fine-tuning, retrieval, or
// embedding data for any machine-learning model, or (b) as input to any machine-learning model, agent, or automated
// system for the purpose of producing a derivative work or reimplementation that is not otherwise permitted by the
// Snap CD Source-Available License (including any Competing Product as defined therein). Contact info@snapcd.io
// for terms covering either use.

using System.Collections.Concurrent;
using System.Reflection;
using System.Xml.Linq;
using Microsoft.AspNetCore.OpenApi;
using Microsoft.OpenApi;
using SnapCd.Contracts;

namespace SnapCd.Server.Core.Startup;

/// <summary>
/// Fills schema descriptions the built-in XML-doc ingestion misses: property shapes it skips
/// (e.g. nullable enum refs rendered as oneOf) and enum type summaries. Resolves each component
/// schema name to its CLR type, reads the assembly's XML doc file directly, and sets any still-empty
/// description from the member's summary. Runs before the coverage gate.
/// </summary>
public class SchemaXmlDocsDocumentTransformer : IOpenApiDocumentTransformer
{
    private static readonly ConcurrentDictionary<Assembly, Dictionary<string, string>> XmlDocsByAssembly = new();

    private static readonly Assembly[] CandidateAssemblies =
    [
        typeof(EndpointDocConvention).Assembly,               // SnapCd.Contracts
        typeof(SchemaXmlDocsDocumentTransformer).Assembly     // SnapCd.Server.Core
    ];

    public Task TransformAsync(OpenApiDocument document, OpenApiDocumentTransformerContext context,
        CancellationToken cancellationToken)
    {
        foreach (var (name, schema) in document.Components?.Schemas ?? new Dictionary<string, IOpenApiSchema>())
        {
            if (schema is not OpenApiSchema concrete) continue;

            var type = ResolveType(name);
            if (type is null) continue;

            if (string.IsNullOrEmpty(concrete.Description))
                concrete.Description = Summary($"T:{type.FullName}", type.Assembly);

            if (concrete.Properties is null) continue;
            foreach (var (propertyName, property) in concrete.Properties)
            {
                if (property is not OpenApiSchema inline || !string.IsNullOrEmpty(inline.Description))
                    continue;

                var clrProperty = FindProperty(type, propertyName);
                if (clrProperty?.DeclaringType is null) continue;

                inline.Description = Summary(
                    $"P:{clrProperty.DeclaringType.FullName}.{clrProperty.Name}",
                    clrProperty.DeclaringType.Assembly);
            }
        }

        return Task.CompletedTask;
    }

    private static Type? ResolveType(string schemaName)
    {
        foreach (var assembly in CandidateAssemblies)
        {
            var match = assembly.GetTypes().FirstOrDefault(t => t.Name == schemaName);
            if (match is not null) return match;
        }

        return null;
    }

    private static PropertyInfo? FindProperty(Type type, string jsonName)
    {
        return type.GetProperties(BindingFlags.Public | BindingFlags.Instance)
            .FirstOrDefault(p => string.Equals(p.Name, jsonName, StringComparison.OrdinalIgnoreCase));
    }

    private static string? Summary(string docId, Assembly assembly)
    {
        var docs = XmlDocsByAssembly.GetOrAdd(assembly, Load);
        return docs.TryGetValue(docId, out var summary) ? summary : null;
    }

    private static Dictionary<string, string> Load(Assembly assembly)
    {
        var docs = new Dictionary<string, string>(StringComparer.Ordinal);
        var path = Path.ChangeExtension(assembly.Location, ".xml");
        if (!File.Exists(path)) return docs;

        var xml = XDocument.Load(path);
        foreach (var member in xml.Descendants("member"))
        {
            var docId = member.Attribute("name")?.Value;
            var summary = member.Element("summary")?.Value;
            if (docId is null || string.IsNullOrWhiteSpace(summary)) continue;

            var collapsed = string.Join(" ",
                summary.Split('\n', StringSplitOptions.RemoveEmptyEntries).Select(l => l.Trim())).Trim();
            docs[docId] = collapsed;
        }

        return docs;
    }
}
