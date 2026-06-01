// SPDX-License-Identifier: LicenseRef-Snap-CD-Source-Available-1.1
// Copyright (c) 2026 Karl Schriek / Schrieksoft.
// No license is granted to use this file, in whole or in part, (a) as training, fine-tuning, retrieval, or
// embedding data for any machine-learning model, or (b) as input to any machine-learning model, agent, or automated
// system for the purpose of producing a derivative work or reimplementation that is not otherwise permitted by the
// Snap CD Source-Available License (including any Competing Product as defined therein). Contact info@snapcd.io
// for terms covering either use.

using System.Text.Json.Nodes;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace SnapCd.Server.Core.Misc.Utils;

public static class JsonNodeUtils
{
    // Helper method to compare JsonNode objects
    public static bool JsonNodeEquals(JsonNode? left, JsonNode? right)
    {
        // If both are null, they are equal
        if (left == null && right == null)
            return true;
        // If one is null and the other isn't, they are not equal
        if (left == null || right == null)
            return false;

        // Compare their JSON string representations
        return left.ToJsonString() == right.ToJsonString();
    }

    public static string? SerializeJsonNode(JsonNode? node)
    {
        return node?.ToJsonString(); // Safely serializes JsonNode to a string
    }

    // Helper method to deserialize JsonNode
    public static JsonNode? DeserializeJsonNode(string? json)
    {
        return string.IsNullOrEmpty(json) ? JsonNode.Parse(json ?? string.Empty) : null; // Safely deserializes JSON string to JsonNode
    }


    public static ValueConverter GetJsonNodeValueConverter()
    {
        return new ValueConverter<JsonNode, string>(
            v => SerializeJsonNode(v) ?? string.Empty, // Serialize JsonNode to JSON string
            v => DeserializeJsonNode(v) ?? string.Empty // Deserialize JSON string to JsonNode
        );
    }

    public static ValueComparer GetJsonNodeValueComparer()
    {
        return new ValueComparer<JsonNode?>(
            (l, r) => JsonNodeEquals(l, r), // Comparison logic
            v => v != null ? v.GetHashCode() : 0, // Hash code logic
            v => v != null ? DeserializeJsonNode(SerializeJsonNode(v)) : null // Snapshot logic (deep clone)
        );
    }
}