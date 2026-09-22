// SPDX-License-Identifier: LicenseRef-Snap-CD-Source-Available-1.1
// Copyright (c) 2026 Karl Schriek / Schrieksoft.
// No license is granted to use this file, in whole or in part, (a) as training, fine-tuning, retrieval, or
// embedding data for any machine-learning model, or (b) as input to any machine-learning model, agent, or automated
// system for the purpose of producing a derivative work or reimplementation that is not otherwise permitted by the
// Snap CD Source-Available License (including any Competing Product as defined therein). Contact info@snapcd.io
// for terms covering either use.
using SnapCd.Server.Core.Services.Crud.Transfers;
using SnapCd.Server.Core.Services.ResolvedConfiguration.HelperClasses;
using Xunit;

namespace SnapCd.Server.Core.Tests.Tests.Transfers;

/// <summary>
/// The key decides whether a proof still describes reality. It must change when any input a slice
/// depends on changes, and must not change for anything else - a false match reports a stale green
/// on the gate that people lock and merge against.
/// </summary>
public class TransferInputKeyTests
{
    [Fact]
    public void The_Same_Inputs_Give_The_Same_Key()
    {
        Assert.Equal(Key("abc123"), Key("abc123"));
    }

    [Fact]
    public void A_New_Commit_Changes_The_Key()
    {
        Assert.NotEqual(Key("abc123"), Key("def456"));
    }

    [Fact]
    public void A_Changed_Fragment_Changes_The_Key()
    {
        Assert.NotEqual(Key("abc123", fragment: "one"), Key("abc123", fragment: "two"));
    }

    /// <summary>A module's own inputs are part of what its plan depends on.</summary>
    [Fact]
    public void Changed_Declared_Inputs_Change_The_Key()
    {
        var a = Declared("one");
        var b = Declared("two");

        Assert.NotEqual(
            TransferInputKey.Compute("abc123", a, null, null),
            TransferInputKey.Compute("abc123", b, null, null));
    }

    /// <summary>
    /// The cross-edge case: a producer re-proves, its outputs change, and the consumer's proof is
    /// no longer valid even though its own code never moved.
    /// </summary>
    [Fact]
    public void A_Changed_Upstream_Output_Changes_The_Key()
    {
        Assert.NotEqual(
            Key("abc123", outputs: new Dictionary<string, string> { ["db_endpoint"] = "old" }),
            Key("abc123", outputs: new Dictionary<string, string> { ["db_endpoint"] = "new" }));
    }

    /// <summary>Same values, different order, same inputs.</summary>
    [Fact]
    public void Threaded_Output_Order_Does_Not_Change_The_Key()
    {
        var one = new Dictionary<string, string> { ["a"] = "1", ["b"] = "2" };
        var other = new Dictionary<string, string> { ["b"] = "2", ["a"] = "1" };

        Assert.Equal(Key("abc123", outputs: one), Key("abc123", outputs: other));
    }

    /// <summary>A slice that gains a threaded value is not the slice that never had one.</summary>
    [Fact]
    public void Gaining_A_Threaded_Output_Changes_The_Key()
    {
        Assert.NotEqual(
            Key("abc123"),
            Key("abc123", outputs: new Dictionary<string, string> { ["x"] = "" }));
    }

    private static ResolvedModule Declared(string moduleName) => new()
    {
        ModuleName = moduleName,
        NamespaceName = "ns",
        StackName = "stack",
        RunnerName = "runner",
        SourceRevision = "main",
        SourceUrl = "https://example.com/repo.git",
        SourceSubdirectory = "",
        Engine = "OpenTofu"
    };

    private static string Key(
        string revision,
        string? fragment = null,
        IReadOnlyDictionary<string, string>? outputs = null) =>
        TransferInputKey.Compute(revision, null, fragment, outputs);
}
