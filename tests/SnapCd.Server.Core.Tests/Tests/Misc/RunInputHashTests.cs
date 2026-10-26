// SPDX-License-Identifier: LicenseRef-Snap-CD-Source-Available-1.1
// Copyright (c) 2026 Karl Schriek / Schrieksoft.
// No license is granted to use this file, in whole or in part, (a) as training, fine-tuning, retrieval, or
// embedding data for any machine-learning model, or (b) as input to any machine-learning model, agent, or automated
// system for the purpose of producing a derivative work or reimplementation that is not otherwise permitted by the
// Snap CD Source-Available License (including any Competing Product as defined therein). Contact info@snapcd.io
// for terms covering either use.

using SnapCd.Contracts;
using SnapCd.Server.Core.Dtos;
using SnapCd.Server.Core.Services.RunInputs;
using Xunit;

namespace SnapCd.Server.Core.Tests.Tests.Misc;

/// <summary>
/// What a run was given, as one value. A result computed from these inputs is current while this is
/// unchanged, so the hash has to move on every difference that reaches the run and on nothing else.
/// </summary>
public class RunInputHashTests
{
    private static ModuleResolvedInput Literal(string name, string value) => new()
    {
        Name = name,
        ResolvedValue = value,
        Source = ModuleInputSource.Literal
    };

    private static ModuleResolvedInput Secret(string name, string value, string? version) => new()
    {
        Name = name,
        ResolvedValue = value,
        Source = ModuleInputSource.ModuleSecret,
        SecretVersion = version
    };

    [Fact]
    public void The_Same_Inputs_Hash_The_Same()
    {
        var first = RunInputHash.Compose([Literal("a", "1"), Literal("b", "2")]);
        var second = RunInputHash.Compose([Literal("a", "1"), Literal("b", "2")]);

        Assert.Equal(first, second);
    }

    /// <summary>A dictionary's order is not guaranteed, so the order it comes back in cannot count.</summary>
    [Fact]
    public void Order_Does_Not_Count()
    {
        var forwards = RunInputHash.Compose([Literal("a", "1"), Literal("b", "2")]);
        var backwards = RunInputHash.Compose([Literal("b", "2"), Literal("a", "1")]);

        Assert.Equal(forwards, backwards);
    }

    [Fact]
    public void A_Changed_Value_Moves_The_Hash()
    {
        var before = RunInputHash.Compose([Literal("a", "1")]);
        var after = RunInputHash.Compose([Literal("a", "2")]);

        Assert.NotEqual(before, after);
    }

    [Fact]
    public void An_Added_Input_Moves_The_Hash()
    {
        var before = RunInputHash.Compose([Literal("a", "1")]);
        var after = RunInputHash.Compose([Literal("a", "1"), Literal("b", "2")]);

        Assert.NotEqual(before, after);
    }

    /// <summary>
    /// A value arriving under a different name is a different input, even where the value is the
    /// same, because what the run is given is the pair.
    /// </summary>
    [Fact]
    public void A_Renamed_Input_Moves_The_Hash()
    {
        var before = RunInputHash.Compose([Literal("a", "1")]);
        var after = RunInputHash.Compose([Literal("b", "1")]);

        Assert.NotEqual(before, after);
    }

    /// <summary>
    /// The composition and the input names are both knowable, so a digest over a short secret could
    /// be found by guessing. A secret stands for itself by version instead.
    /// </summary>
    [Fact]
    public void A_Secrets_Value_Is_Not_In_The_Hash()
    {
        var one = RunInputHash.Compose([Secret("token", "hunter2", "v1")]);
        var another = RunInputHash.Compose([Secret("token", "something-else", "v1")]);

        Assert.Equal(one, another);
    }

    [Fact]
    public void A_Rotated_Secret_Moves_The_Hash()
    {
        var before = RunInputHash.Compose([Secret("token", "hunter2", "v1")]);
        var after = RunInputHash.Compose([Secret("token", "hunter2", "v2")]);

        Assert.NotEqual(before, after);
    }

    /// <summary>
    /// A secret whose version could not be read stands for nothing, so two of them hash alike and a
    /// rotation between them goes unseen. The caller reads that as unknown rather than as unchanged.
    /// </summary>
    [Fact]
    public void A_Secret_Without_A_Version_Contributes_Nothing()
    {
        var one = RunInputHash.Compose([Secret("token", "hunter2", null)]);
        var another = RunInputHash.Compose([Secret("token", "different", null)]);

        Assert.Equal(one, another);
    }

    [Fact]
    public void The_Same_Value_From_A_Different_Source_Moves_The_Hash()
    {
        var literal = RunInputHash.Compose([Literal("a", "1")]);
        var fromOutput = RunInputHash.Compose([
            new ModuleResolvedInput { Name = "a", ResolvedValue = "1", Source = ModuleInputSource.ModuleOutput }
        ]);

        Assert.NotEqual(literal, fromOutput);
    }

    [Fact]
    public void No_Inputs_Still_Hashes()
    {
        Assert.False(string.IsNullOrEmpty(RunInputHash.Compose([])));
    }
}
