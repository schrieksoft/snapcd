// SPDX-License-Identifier: LicenseRef-Snap-CD-Source-Available-1.1
// Copyright (c) 2026 Karl Schriek / Schrieksoft.
// No license is granted to use this file, in whole or in part, (a) as training, fine-tuning, retrieval, or
// embedding data for any machine-learning model, or (b) as input to any machine-learning model, agent, or automated
// system for the purpose of producing a derivative work or reimplementation that is not otherwise permitted by the
// Snap CD Source-Available License (including any Competing Product as defined therein). Contact info@snapcd.io
// for terms covering either use.


using SnapCd.Runner.Services;
using Xunit;

namespace SnapCd.Runner.Tests;

/// <summary>
/// The state commands, one address at a time. Addresses come from a module's own state rather than
/// being typed, but they still pass through a shell, so each is one quoted argument.
/// </summary>
public class StateMoveCommandTests
{
    [Fact]
    public void Mv_Names_Both_Ends()
    {
        var command = TerraformEngine.StateMoveCommand(
            "tofu", StateMoveOperation.Move, "random_pet.old", "random_pet.new");

        Assert.Equal("tofu state mv 'random_pet.old' 'random_pet.new'", command);
    }

    [Fact]
    public void Import_Names_The_Address_And_The_Resource()
    {
        var command = TerraformEngine.StateMoveCommand(
            "tofu", StateMoveOperation.Import, "aws_s3_bucket.main", "my-bucket");

        Assert.Equal("tofu import 'aws_s3_bucket.main' 'my-bucket'", command);
    }

    [Fact]
    public void Remove_Names_Only_The_Address()
    {
        var command = TerraformEngine.StateMoveCommand(
            "tofu", StateMoveOperation.Remove, "random_pet.gone", null);

        Assert.Equal("tofu state rm 'random_pet.gone'", command);
    }

    /// <summary>An indexed address reaches the engine as written rather than being split by the shell.</summary>
    [Fact]
    public void An_Indexed_Address_Survives_Quoting()
    {
        var command = TerraformEngine.StateMoveCommand(
            "tofu", StateMoveOperation.Remove, "module.network[\"eu-west-1\"].aws_vpc.this", null);

        Assert.Equal("tofu state rm 'module.network[\"eu-west-1\"].aws_vpc.this'", command);
    }

    /// <summary>
    /// Shell metacharacters stay inside the quotes, so an address is only ever an argument. Single
    /// quotes are the innermost quoting the script carries, which is what makes this hold.
    /// </summary>
    [Theory]
    [InlineData("a; rm -rf /")]
    [InlineData("a$(id -u)b")]
    [InlineData("a`id`b")]
    [InlineData("a b")]
    public void Metacharacters_Stay_Inside_The_Quotes(string address)
    {
        var command = TerraformEngine.StateMoveCommand("tofu", StateMoveOperation.Remove, address, null);

        Assert.Equal($"tofu state rm '{address}'", command);
    }

    /// <summary>A quote closes and reopens the quoting rather than escaping out of it.</summary>
    [Fact]
    public void A_Quote_In_An_Address_Cannot_Break_Out()
    {
        var command = TerraformEngine.StateMoveCommand("tofu", StateMoveOperation.Remove, "a'b", null);

        Assert.Equal("tofu state rm 'a'\\''b'", command);
    }
}
