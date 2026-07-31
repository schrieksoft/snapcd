// SPDX-License-Identifier: LicenseRef-Snap-CD-Source-Available-1.1
// Copyright (c) 2026 Karl Schriek / Schrieksoft.
// No license is granted to use this file, in whole or in part, (a) as training, fine-tuning, retrieval, or
// embedding data for any machine-learning model, or (b) as input to any machine-learning model, agent, or automated
// system for the purpose of producing a derivative work or reimplementation that is not otherwise permitted by the
// Snap CD Source-Available License (including any Competing Product as defined therein). Contact info@snapcd.io
// for terms covering either use.

using SnapCd.Server.Core.Entities.Definition;
using SnapCd.Server.Core.Services;

namespace SnapCd.Server.Core.Tests.Tests.Services;

public class TriggerPathClosureTests
{
    [Theory]
    [InlineData("shared/network", "shared/network")]
    [InlineData("shared/network/", "shared/network")]
    [InlineData("/shared/network", "shared/network")]
    [InlineData(" shared/network ", "shared/network")]
    [InlineData("", ".")]
    [InlineData("/", ".")]
    public void NormalizePath_Normalizes(string input, string expected)
    {
        Assert.Equal(expected, TriggerPathClosure.NormalizePath(input));
    }

    [Fact]
    public void Compose_Is_Order_Independent()
    {
        var hashes = new Dictionary<string, string>
        {
            ["a"] = "hash-a",
            ["b"] = "hash-b"
        };

        Assert.Equal(
            TriggerPathClosure.Compose(new[] { "a", "b" }, hashes),
            TriggerPathClosure.Compose(new[] { "b", "a" }, hashes));
    }

    [Fact]
    public void Compose_Moves_When_A_Hash_Moves()
    {
        var before = TriggerPathClosure.Compose(new[] { "a", "b" }, new Dictionary<string, string> { ["a"] = "1", ["b"] = "2" });
        var after = TriggerPathClosure.Compose(new[] { "a", "b" }, new Dictionary<string, string> { ["a"] = "1", ["b"] = "3" });

        Assert.NotEqual(before, after);
    }

    [Fact]
    public void Compose_Moves_When_Membership_Changes()
    {
        var hashes = new Dictionary<string, string> { ["a"] = "1", ["b"] = "2" };

        Assert.NotEqual(
            TriggerPathClosure.Compose(new[] { "a" }, hashes),
            TriggerPathClosure.Compose(new[] { "a", "b" }, hashes));
    }

    [Fact]
    public void Compose_Treats_Missing_Path_As_Empty_Hash()
    {
        var withMissing = TriggerPathClosure.Compose(new[] { "a", "gone" }, new Dictionary<string, string> { ["a"] = "1" });
        var withSentinel = TriggerPathClosure.Compose(new[] { "a", "gone" }, new Dictionary<string, string> { ["a"] = "1", ["gone"] = "" });

        Assert.Equal(withMissing, withSentinel);
    }

    [Fact]
    public void Compose_Deduplicates_Unnormalized_Aliases()
    {
        var hashes = new Dictionary<string, string> { ["a"] = "1" };

        Assert.Equal(
            TriggerPathClosure.Compose(new[] { "a" }, hashes),
            TriggerPathClosure.Compose(new[] { "a", "a/", "/a" }, hashes));
    }

    [Fact]
    public void ExpandWithClosures_Widens_Declared_Paths_With_Their_Closures()
    {
        var closures = new Dictionary<string, List<string>>
        {
            ["modules/app-a"] = new() { "shared/network", "shared/naming" },
            ["modules/unrelated"] = new() { "shared/other" }
        };

        var expanded = TriggerPathClosure.ExpandWithClosures(new[] { "modules/app-a", "shared/scripts" }, closures);

        Assert.Equal(new[] { "modules/app-a", "shared/naming", "shared/network", "shared/scripts" }, expanded);
    }

    [Fact]
    public void ExpandWithClosures_Without_Discovery_Returns_Declared_Paths()
    {
        var declared = new[] { "modules/app-a", "shared/scripts" };

        Assert.Equal(declared, TriggerPathClosure.ExpandWithClosures(declared, null));
    }

    [Fact]
    public void WatchedPaths_Unions_Subdirectory_Module_And_Namespace_Paths()
    {
        var module = new Module
        {
            SourceSubdirectory = "modules/app-a/",
            AdditionalTriggerPaths =
            {
                new ModuleAdditionalTriggerPath { Path = "shared/scripts" },
                new ModuleAdditionalTriggerPath { Path = "modules/app-a" }
            },
            Namespace = new Namespace
            {
                AdditionalTriggerPaths = { new NamespaceAdditionalTriggerPath { Path = "shared/config" } }
            }
        };

        Assert.Equal(new[] { "modules/app-a", "shared/config", "shared/scripts" }, TriggerPathClosure.WatchedPaths(module));
    }

    [Theory]
    [InlineData(null, null, false)]
    [InlineData(null, true, true)]
    [InlineData(null, false, false)]
    [InlineData(true, null, true)]
    [InlineData(true, false, true)]
    [InlineData(false, true, false)]
    public void FilterEnabled_Resolves_Module_Then_Namespace_Then_False(bool? moduleFlag, bool? namespaceDefault, bool expected)
    {
        var module = new Module
        {
            TriggerPathFilterEnabled = moduleFlag,
            Namespace = new Namespace { DefaultTriggerPathFilterEnabled = namespaceDefault }
        };

        Assert.Equal(expected, TriggerPathClosure.FilterEnabled(module));
    }
}
