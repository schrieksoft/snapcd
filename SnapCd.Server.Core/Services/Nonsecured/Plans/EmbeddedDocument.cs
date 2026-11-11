// SPDX-License-Identifier: LicenseRef-Snap-CD-Source-Available-1.1
// Copyright (c) 2026 Karl Schriek / Schrieksoft.
// No license is granted to use this file, in whole or in part, (a) as training, fine-tuning, retrieval, or
// embedding data for any machine-learning model, or (b) as input to any machine-learning model, agent, or automated
// system for the purpose of producing a derivative work or reimplementation that is not otherwise permitted by the
// Snap CD Source-Available License (including any Competing Product as defined therein). Contact info@snapcd.io
// for terms covering either use.

using System.Text.Json;
using YamlDotNet.Core;
using YamlDotNet.Core.Events;
using YamlDotNet.Serialization;

namespace SnapCd.Server.Core.Services.Nonsecured.Plans;

/// <summary>
/// Recognises a string that holds a whole document. A Helm values blob, a Kubernetes manifest and
/// a JSON policy all arrive as one string, and the engine compares them as text. Parsing one lets
/// the walk continue inside it.
/// </summary>
public static class EmbeddedDocument
{
    private static readonly IDeserializer Yaml = new DeserializerBuilder().Build();

    public static bool TryParse(string text, out JsonElement? document, out string? format)
    {
        document = null;
        format = null;

        var trimmed = text.Trim();
        if (trimmed.Length == 0) return false;

        // A plain sentence is not a document, and YAML will parse one as a scalar or throw. The
        // test for a document is what it parses to, which the deserializer below decides: a map or
        // a list is a document, anything else is a value and is compared as the string it is.

        if (trimmed[0] is '{' or '[')
        {
            try
            {
                document = JsonDocument.Parse(trimmed).RootElement.Clone();
                format = "json";
                return true;
            }
            catch (JsonException)
            {
                return false;
            }
        }

        try
        {
            // A stream, because a manifest file commonly holds several documents separated by ---.
            // One document is unwrapped; several become an array, so the walk descends into each.
            var documents = new List<object?>();
            using (var reader = new StringReader(trimmed))
            {
                var parser = new Parser(reader);
                parser.Consume<StreamStart>();

                while (parser.Accept<DocumentStart>(out _))
                    documents.Add(Yaml.Deserialize<object?>(parser));
            }

            if (documents.Count == 0) return false;

            if (documents.Count == 1)
            {
                var graph = documents[0];

                // A scalar is not a document: parsing it gains nothing over comparing the string.
                if (graph is null or string or bool or int or long or double) return false;

                document = JsonSerializer.SerializeToDocument(graph).RootElement.Clone();
                format = "yaml";
                return true;
            }

            document = JsonSerializer.SerializeToDocument(documents).RootElement.Clone();
            format = "yaml-stream";
            return true;
        }
        catch (Exception)
        {
            return false;
        }
    }
}
