// SPDX-License-Identifier: LicenseRef-Snap-CD-Source-Available-1.1
// Copyright (c) 2026 Karl Schriek / Schrieksoft.
// No license is granted to use this file, in whole or in part, (a) as training, fine-tuning, retrieval, or
// embedding data for any machine-learning model, or (b) as input to any machine-learning model, agent, or automated
// system for the purpose of producing a derivative work or reimplementation that is not otherwise permitted by the
// Snap CD Source-Available License (including any Competing Product as defined therein). Contact info@snapcd.io
// for terms covering either use.

using SnapCd.Runner.Services.Plan;
using Xunit;

namespace SnapCd.Runner.Tests;

/// <summary>
/// Unwrapping a plan file's encoded output into the type and value a signals document carries.
///
/// The bytes below were taken from a real plan file: OpenTofu planning a root whose random_pet is
/// being replaced, so its id is unknown. They are what the proto's DynamicValue.Msgpack held.
/// </summary>
public class PlanOutputSignalReaderTests
{
    /// <summary>[ "string", "trusting-lab" ] as a plan file writes it.</summary>
    private static readonly byte[] KnownString =
        Convert.FromHexString("92c40822737472696e6722ac7472757374696e672d6c6162");

    /// <summary>[ "string", unknown ] - the same output once its resource is being replaced.</summary>
    private static readonly byte[] UnknownString =
        Convert.FromHexString("92c40822737472696e6722d40000");

    [Fact]
    public void A_Known_Value_Splits_Into_Its_Type_And_Its_Value()
    {
        var split = PlanOutputSignalReader.Split(KnownString);

        Assert.NotNull(split);
        Assert.Equal("c40822737472696e6722", Convert.ToHexString(split!.Value.Type).ToLowerInvariant());
        Assert.Equal("ac7472757374696e672d6c6162", Convert.ToHexString(split.Value.Value).ToLowerInvariant());
    }

    /// <summary>
    /// The case the whole mechanism exists for. fixext1 type 0 is cty's unknown, and it has to
    /// survive as those three bytes: decoding it would force a guess at a value no plan knows.
    /// </summary>
    [Fact]
    public void An_Unknown_Value_Survives_As_Fixext1()
    {
        var split = PlanOutputSignalReader.Split(UnknownString);

        Assert.NotNull(split);
        Assert.Equal("d40000", Convert.ToHexString(split!.Value.Value).ToLowerInvariant());
    }

    /// <summary>
    /// The type half is carried verbatim, so a consumer reading the signals document gets the same
    /// ctyjson bytes planwalker would have produced for the same output.
    /// </summary>
    [Fact]
    public void The_Type_Half_Is_The_Ctyjson_Planwalker_Emits()
    {
        var split = PlanOutputSignalReader.Split(UnknownString);

        // The binary header is stripped by whoever writes the document; the payload is the type.
        var type = split!.Value.Type;
        Assert.Equal(0xc4, type[0]);
        Assert.Equal("\"string\"", System.Text.Encoding.UTF8.GetString(type[2..]));
    }

    [Fact]
    public void Bytes_That_Are_Not_A_Two_Element_Array_Are_Refused()
    {
        // A bare value rather than the [type, value] pair a plan file writes.
        Assert.Null(PlanOutputSignalReader.Split(Convert.FromHexString("ac7472757374696e672d6c6162")));
    }

    [Fact]
    public void Truncated_Bytes_Are_Refused()
    {
        // The array header promises two elements and the buffer stops inside the first.
        Assert.Null(PlanOutputSignalReader.Split(Convert.FromHexString("92c408227374")));
    }

    [Fact]
    public void Empty_Bytes_Are_Refused()
    {
        Assert.Null(PlanOutputSignalReader.Split([]));
    }

    /// <summary>
    /// The document a plan step produces has to be the one planwalker would have produced for the
    /// same plan, because the server reads both with the same code. This compares against the
    /// signals planwalker actually emitted for this root: same type, same before, same after,
    /// same action.
    /// </summary>
    [Fact]
    public void The_Document_Matches_What_Planwalker_Emitted_For_The_Same_Plan()
    {
        var split = PlanOutputSignalReader.Split(UnknownString)!.Value;
        var before = PlanOutputSignalReader.Split(KnownString)!.Value;

        var document = PlanSignalsDocument.Build([
            new PlanOutputChange
            {
                Name = "a_id",
                Action = SnapCd.Contracts.PlanAction.Replace,
                EncodedType = split.Type,
                EncodedBefore = before.Value,
                EncodedAfter = split.Value
            }
        ]);

        Assert.NotNull(document);

        var output = System.Text.Json.Nodes.JsonNode.Parse(document!)!["outputs"]!["a_id"]!;

        // planwalker's own output for this root, from the diamond fixture.
        Assert.Equal("unknown", (string?)output["action"]);
        Assert.Equal("InN0cmluZyI=", (string?)output["type"]);
        Assert.Equal("rHRydXN0aW5nLWxhYg==", (string?)output["before"]);
        Assert.Equal("1AAA", (string?)output["after"]);
    }

    [Fact]
    public void An_Engine_Whose_Plan_Carries_No_Encoded_Outputs_Produces_No_Document()
    {
        var document = PlanSignalsDocument.Build([
            new PlanOutputChange { Name = "whatever", Action = SnapCd.Contracts.PlanAction.Update }
        ]);

        // Absent rather than empty: a Module that cannot propagate is a different answer from one
        // whose outputs did not change.
        Assert.Null(document);
    }

    /// <summary>
    /// A created output's single value is its new one. Reading by position alone puts it in the
    /// before slot and leaves after empty, which reports a change as having already happened and
    /// tells a consumer nothing about what is coming.
    /// </summary>
    [Fact]
    public void A_Creations_Only_Value_Is_Its_After()
    {
        var split = PlanOutputSignalReader.Split(UnknownString)!.Value;

        var document = PlanSignalsDocument.Build([
            new PlanOutputChange
            {
                Name = "a_id",
                Action = SnapCd.Contracts.PlanAction.Create,
                EncodedType = split.Type,
                EncodedAfter = split.Value
            }
        ]);

        var output = System.Text.Json.Nodes.JsonNode.Parse(document!)!["outputs"]!["a_id"]!;

        Assert.Equal("1AAA", (string?)output["after"]);
        Assert.Null(output["before"]);
        Assert.Equal("unknown", (string?)output["action"]);
    }

    [Fact]
    public void A_Created_Output_Carries_No_Before()
    {
        var split = PlanOutputSignalReader.Split(UnknownString)!.Value;

        var document = PlanSignalsDocument.Build([
            new PlanOutputChange
            {
                Name = "fresh",
                Action = SnapCd.Contracts.PlanAction.Create,
                EncodedType = split.Type,
                EncodedAfter = split.Value
            }
        ]);

        var output = System.Text.Json.Nodes.JsonNode.Parse(document!)!["outputs"]!["fresh"]!;

        Assert.Null(output["before"]);
    }

    [Fact]
    public void A_Sensitive_Output_Is_Marked()
    {
        var split = PlanOutputSignalReader.Split(UnknownString)!.Value;

        var document = PlanSignalsDocument.Build([
            new PlanOutputChange
            {
                Name = "secret",
                Action = SnapCd.Contracts.PlanAction.Update,
                EncodedType = split.Type,
                EncodedAfter = split.Value,
                Sensitive = true
            }
        ]);

        var output = System.Text.Json.Nodes.JsonNode.Parse(document!)!["outputs"]!["secret"]!;

        Assert.True((bool?)output["sensitive"]);
    }
}
