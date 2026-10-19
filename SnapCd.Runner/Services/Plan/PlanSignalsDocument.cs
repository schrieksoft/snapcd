// SPDX-License-Identifier: LicenseRef-Snap-CD-Source-Available-1.1
// Copyright (c) 2026 Karl Schriek / Schrieksoft.
// No license is granted to use this file, in whole or in part, (a) as training, fine-tuning, retrieval, or
// embedding data for any machine-learning model, or (b) as input to any machine-learning model, agent, or automated
// system for the purpose of producing a derivative work or reimplementation that is not otherwise permitted by the
// Snap CD Source-Available License (including any Competing Product as defined therein). Contact info@snapcd.io
// for terms covering either use.

using System.Text.Json.Nodes;
using SnapCd.Contracts;

namespace SnapCd.Runner.Services.Plan;

/// <summary>
/// What a plan says about its own outputs, in the format a downstream root consumes.
///
/// The same document planwalker emits, so one source of signals is interchangeable with the other:
/// a plan step's outputs and a probe's outputs are read by the same code on the server. Values are
/// base64 of the bytes the plan held and are never decoded here, which is what lets an unknown stay
/// unknown rather than becoming a guess at a value nothing knows yet.
/// </summary>
public static class PlanSignalsDocument
{
    private const string FormatVersion = "1";

    /// <summary>
    /// The document for these output changes, or null when none of them carried an encoded value.
    /// Null means "no signals to report" rather than "nothing changed": an engine whose plan has
    /// no encoded outputs cannot propagate, which is a different answer from having no consumers.
    /// </summary>
    public static string? Build(IEnumerable<PlanOutputChange> outputChanges)
    {
        var outputs = new JsonObject();

        foreach (var change in outputChanges)
        {
            if (change.EncodedType == null) continue;

            var value = new JsonObject
            {
                ["action"] = ActionName(change),
                ["type"] = Convert.ToBase64String(TypePayload(change.EncodedType))
            };

            if (change.EncodedBefore != null)
                value["before"] = Convert.ToBase64String(change.EncodedBefore);

            if (change.EncodedAfter != null)
                value["after"] = Convert.ToBase64String(change.EncodedAfter);

            if (change.Sensitive)
                value["sensitive"] = true;

            outputs[change.Name] = value;
        }

        if (outputs.Count == 0) return null;

        return new JsonObject
        {
            ["format_version"] = FormatVersion,
            ["outputs"] = outputs
        }.ToJsonString();
    }

    /// <summary>
    /// The type without its msgpack binary header, which is how a signals document carries it: the
    /// header describes the plan file's framing rather than the type.
    /// </summary>
    private static byte[] TypePayload(byte[] encodedType)
    {
        return encodedType switch
        {
            [0xc4, _, .. var rest] => rest,
            [0xc5, _, _, .. var rest] => rest,
            _ => encodedType
        };
    }

    /// <summary>
    /// What the plan says happens, with one substitution: an output whose new value the plan could
    /// not predict is reported as unknown rather than by the action that produced it, because that
    /// is the distinction a consumer acts on.
    /// </summary>
    private static string ActionName(PlanOutputChange change)
    {
        if (IsUnknown(change.EncodedAfter)) return "unknown";

        return change.Action switch
        {
            PlanAction.Create => "create",
            PlanAction.Update => "update",
            PlanAction.Delete => "delete",
            // A replaced output is a changed value from a consumer's side; what was replaced is
            // the resource behind it, which is not a distinction the consumer can act on.
            PlanAction.Replace => "update",
            _ => "no-op"
        };
    }

    /// <summary>msgpack fixext1 with type 0, which is cty's marker for an unknown value.</summary>
    private static bool IsUnknown(byte[]? encodedValue) =>
        encodedValue is [0xd4, 0x00, ..];
}
