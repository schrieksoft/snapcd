// SPDX-License-Identifier: LicenseRef-Snap-CD-Source-Available-1.1
// Copyright (c) 2026 Karl Schriek / Schrieksoft.
// No license is granted to use this file, in whole or in part, (a) as training, fine-tuning, retrieval, or
// embedding data for any machine-learning model, or (b) as input to any machine-learning model, agent, or automated
// system for the purpose of producing a derivative work or reimplementation that is not otherwise permitted by the
// Snap CD Source-Available License (including any Competing Product as defined therein). Contact info@snapcd.io
// for terms covering either use.

using SnapCd.Server.Core.Services.Utils.RunInputs;
using Xunit;

namespace SnapCd.Server.Core.Tests.Tests.Misc;

/// <summary>
/// What a job's declaration amounts to, as one value. The declaration is read without an order, so
/// the same declaration can arrive as differently ordered JSON and must still hash the same.
/// </summary>
public class DeclarationHashTests
{
    [Fact]
    public void Array_Order_Does_Not_Count()
    {
        var one = DeclarationHash.Compose("""{"flags":[{"name":"a"},{"name":"b"}]}""");
        var other = DeclarationHash.Compose("""{"flags":[{"name":"b"},{"name":"a"}]}""");

        Assert.Equal(one, other);
    }

    [Fact]
    public void Property_Order_Does_Not_Count()
    {
        var one = DeclarationHash.Compose("""{"a":1,"b":2}""");
        var other = DeclarationHash.Compose("""{"b":2,"a":1}""");

        Assert.Equal(one, other);
    }

    [Fact]
    public void Nested_Order_Does_Not_Count()
    {
        var one = DeclarationHash.Compose("""{"outer":[{"inner":["x","y"],"z":1}]}""");
        var other = DeclarationHash.Compose("""{"outer":[{"z":1,"inner":["y","x"]}]}""");

        Assert.Equal(one, other);
    }

    [Fact]
    public void A_Changed_Value_Moves_The_Hash()
    {
        var before = DeclarationHash.Compose("""{"engine":"tofu"}""");
        var after = DeclarationHash.Compose("""{"engine":"terraform"}""");

        Assert.NotEqual(before, after);
    }

    [Fact]
    public void An_Added_Element_Moves_The_Hash()
    {
        var before = DeclarationHash.Compose("""{"flags":[{"name":"a"}]}""");
        var after = DeclarationHash.Compose("""{"flags":[{"name":"a"},{"name":"b"}]}""");

        Assert.NotEqual(before, after);
    }

    /// <summary>
    /// A duplicate is a difference: two of a flag is not the same declaration as one, so sorting
    /// must not collapse them.
    /// </summary>
    [Fact]
    public void A_Duplicated_Element_Moves_The_Hash()
    {
        var once = DeclarationHash.Compose("""{"flags":["a"]}""");
        var twice = DeclarationHash.Compose("""{"flags":["a","a"]}""");

        Assert.NotEqual(once, twice);
    }

    [Fact]
    public void A_Removed_Property_Moves_The_Hash()
    {
        var before = DeclarationHash.Compose("""{"a":1,"b":2}""");
        var after = DeclarationHash.Compose("""{"a":1}""");

        Assert.NotEqual(before, after);
    }

    /// <summary>Whitespace is not a difference in the declaration.</summary>
    [Fact]
    public void Formatting_Does_Not_Count()
    {
        var compact = DeclarationHash.Compose("""{"a":1,"b":[2,3]}""");
        var spaced = DeclarationHash.Compose("{\n  \"a\": 1,\n  \"b\": [ 2, 3 ]\n}");

        Assert.Equal(compact, spaced);
    }

    /// <summary>Something that is not JSON stands for itself rather than throwing.</summary>
    [Fact]
    public void Unparseable_Text_Still_Hashes()
    {
        var hash = DeclarationHash.Compose("not json at all");

        Assert.False(string.IsNullOrEmpty(hash));
        Assert.Equal(hash, DeclarationHash.Compose("not json at all"));
    }

    [Fact]
    public void Null_And_Missing_Are_Not_The_Same()
    {
        var present = DeclarationHash.Compose("""{"a":null}""");
        var absent = DeclarationHash.Compose("{}");

        Assert.NotEqual(present, absent);
    }
}
