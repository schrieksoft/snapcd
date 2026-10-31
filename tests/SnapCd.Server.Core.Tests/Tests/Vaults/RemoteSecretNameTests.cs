// SPDX-License-Identifier: LicenseRef-Snap-CD-Source-Available-1.1
// Copyright (c) 2026 Karl Schriek / Schrieksoft.
// No license is granted to use this file, in whole or in part, (a) as training, fine-tuning, retrieval, or
// embedding data for any machine-learning model, or (b) as input to any machine-learning model, agent, or automated
// system for the purpose of producing a derivative work or reimplementation that is not otherwise permitted by the
// Snap CD Source-Available License (including any Competing Product as defined therein). Contact info@snapcd.io
// for terms covering either use.

using SnapCd.Server.Core.Entities.Definition.Secrets.Scoped;
using SnapCd.Server.Core.Services.Utils.Vaults;
using Xunit;

namespace SnapCd.Server.Core.Tests.Tests.Vaults;

/// <summary>
/// The names Secret values are stored under, and the question the sweep asks of a name it did not
/// build: does this already carry its owner?
/// </summary>
public class RemoteSecretNameTests
{
    private static readonly Guid Org = Guid.Parse("11111111-1111-1111-1111-111111111111");
    private static readonly Guid Module = Guid.Parse("22222222-2222-2222-2222-222222222222");
    private static readonly Guid SecretId = Guid.Parse("33333333-3333-3333-3333-333333333333");

    private static ModuleSecret Secret(bool scoped) =>
        new() { Id = SecretId, OrganizationId = Org, ModuleId = Module, NameContainsScope = scoped };

    [Fact]
    public void A_Scoped_Name_Carries_Its_Owner()
    {
        Assert.Equal($"module--{Org}--{Module}--{SecretId}", RemoteSecretName.Scoped(Secret(true), Org));
    }

    [Fact]
    public void A_Scopeless_Name_Carries_Only_The_Secret()
    {
        Assert.Equal($"module--{Org}--{SecretId}", RemoteSecretName.Scopeless(Secret(false), Org));
    }

    /// <summary>
    /// The name follows the row, not the code version, which is what lets a half-swept deployment
    /// keep reading every Secret.
    /// </summary>
    [Fact]
    public void For_Follows_What_The_Row_Says()
    {
        Assert.Equal(RemoteSecretName.Scoped(Secret(true), Org), RemoteSecretName.For(Secret(true), Org));
        Assert.Equal(RemoteSecretName.Scopeless(Secret(false), Org), RemoteSecretName.For(Secret(false), Org));
    }

    [Fact]
    public void The_Longest_Name_Fits_Key_Vaults_Limit()
    {
        var longest = $"namespace--{Org}--{Module}--{SecretId}";

        Assert.True(longest.Length <= RemoteSecretName.MaxLength,
            $"longest name is {longest.Length} characters");
    }

    /// <summary>
    /// The hash existing output values were written with, reproduced exactly. Changing it would
    /// point every Module's outputs at names that hold nothing.
    /// </summary>
    [Fact]
    public void The_Output_Hash_Is_Unchanged()
    {
        using var sha256 = System.Security.Cryptography.SHA256.Create();
        var bytes = sha256.ComputeHash(System.Text.Encoding.UTF8.GetBytes("db_password"));
        var expected = BitConverter.ToString(bytes).Replace("-", "").ToLower()[..32];

        Assert.Equal(expected, RemoteSecretName.Hash("db_password"));
        Assert.Equal($"output--{Org}--{Module}--{expected}",
            RemoteSecretName.ForOutput(Org, Module, "db_password"));
    }

    /// <summary>
    /// A signal is named after its Module and its own id, the same shape as a Secret. Every signal
    /// is its own row, so each answer to each wave has its own value.
    /// </summary>
    [Fact]
    public void A_Signal_Is_Named_After_Its_Module_And_Itself()
    {
        var signalId = Guid.Parse("44444444-4444-4444-4444-444444444444");

        Assert.Equal($"signal--{Org}--{Module}--{signalId}",
            RemoteSecretName.ForSignal(Org, Module, signalId));
    }

    [Fact]
    public void A_Signal_Name_Fits_Key_Vaults_Limit()
    {
        var name = RemoteSecretName.ForSignal(Org, Module, Guid.NewGuid());

        Assert.Matches("^[A-Za-z0-9-]+$", name);
        Assert.True(name.Length <= RemoteSecretName.MaxLength, $"{name.Length} characters");
    }

    [Theory]
    [InlineData("module", true)]
    [InlineData("namespace", true)]
    [InlineData("stack", true)]
    public void A_Secret_Name_Is_Scoped_Only_With_The_Owner_Segment(string prefix, bool _)
    {
        Assert.False(RemoteSecretName.NameContainsScope($"{prefix}--{Org}--{SecretId}"));
        Assert.True(RemoteSecretName.NameContainsScope($"{prefix}--{Org}--{Module}--{SecretId}"));
    }

    /// <summary>These carried their owner from the outset, so they need no sweeping.</summary>
    [Theory]
    [InlineData("output--11111111-1111-1111-1111-111111111111--22222222-2222-2222-2222-222222222222--abc123")]
    [InlineData("integration--11111111-1111-1111-1111-111111111111--22222222-2222-2222-2222-222222222222")]
    public void Kinds_That_Always_Carried_Their_Owner_Report_Scoped(string name)
    {
        Assert.True(RemoteSecretName.NameContainsScope(name));
    }

    /// <summary>
    /// A signal's name ends in an output name, which is user supplied and may contain the
    /// separator. Counting segments would misread these, so the prefix decides.
    /// </summary>
    [Theory]
    [InlineData("signal--11111111-1111-1111-1111-111111111111--22222222-2222-2222-2222-222222222222--db_password")]
    [InlineData("signal--11111111-1111-1111-1111-111111111111--22222222-2222-2222-2222-222222222222--odd--name")]
    [InlineData("signal--11111111-1111-1111-1111-111111111111--22222222-2222-2222-2222-222222222222--x")]
    public void A_Signal_Name_Reports_Scoped_Whatever_Its_Output_Name_Contains(string name)
    {
        Assert.True(RemoteSecretName.NameContainsScope(name));
    }

    [Theory]
    [InlineData("")]
    [InlineData("nonsense")]
    [InlineData("unknown--11111111-1111-1111-1111-111111111111--22222222-2222-2222-2222-222222222222")]
    public void An_Unrecognised_Name_Is_Not_Claimed_As_Scoped(string name)
    {
        Assert.False(RemoteSecretName.NameContainsScope(name));
    }
}
