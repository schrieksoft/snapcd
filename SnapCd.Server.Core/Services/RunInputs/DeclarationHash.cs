// SPDX-License-Identifier: LicenseRef-Snap-CD-Source-Available-1.1
// Copyright (c) 2026 Karl Schriek / Schrieksoft.
// No license is granted to use this file, in whole or in part, (a) as training, fine-tuning, retrieval, or
// embedding data for any machine-learning model, or (b) as input to any machine-learning model, agent, or automated
// system for the purpose of producing a derivative work or reimplementation that is not otherwise permitted by the
// Snap CD Source-Available License (including any Competing Product as defined therein). Contact info@snapcd.io
// for terms covering either use.

using System.Text.Json;
using System.Text.Json.Nodes;

namespace SnapCd.Server.Core.Services.RunInputs;

/// <summary>
/// What a job's declaration amounts to, as one value, so a result computed under it can be judged
/// still current.
///
/// The whole declaration counts, with nothing left out. Several of its parts cannot change a plan,
/// and excluding them would make the result quieter, but a part added later would then fall outside
/// the exclusion and silently stop counting. Including everything fails the other way, as a change
/// reported where none was needed, which someone sees.
///
/// The declaration is read from the database without an order, so the same declaration can arrive
/// as differently ordered JSON. It is sorted before hashing, or every job would look different from
/// the one before it.
/// </summary>
public static class DeclarationHash
{
    public static string Compose(string declaredJson) =>
        RunInputHash.Digest(Normalize(declaredJson));

    /// <summary>
    /// The same JSON written one way: object properties by name, array elements by what they
    /// serialize to. Sorting arrays by content rather than position is what makes the result
    /// independent of the order rows came back in.
    /// </summary>
    public static string Normalize(string json)
    {
        JsonNode? parsed;

        try
        {
            parsed = JsonNode.Parse(json);
        }
        catch (JsonException)
        {
            // Not JSON, so there is nothing to sort and the text stands for itself.
            return json;
        }

        return Sort(parsed)?.ToJsonString() ?? "null";
    }

    private static JsonNode? Sort(JsonNode? node)
    {
        switch (node)
        {
            case JsonObject obj:
            {
                var sorted = new JsonObject();
                foreach (var property in obj.ToList().OrderBy(p => p.Key, StringComparer.Ordinal))
                {
                    obj.Remove(property.Key);
                    sorted[property.Key] = Sort(property.Value);
                }

                return sorted;
            }

            case JsonArray array:
            {
                var elements = array.ToList();
                foreach (var element in elements) array.Remove(element);

                var sorted = new JsonArray();
                foreach (var element in elements
                             .Select(Sort)
                             .OrderBy(e => e?.ToJsonString() ?? "null", StringComparer.Ordinal))
                    sorted.Add(element);

                return sorted;
            }

            default:
                return node?.DeepClone();
        }
    }
}
