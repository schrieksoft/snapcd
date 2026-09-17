// SPDX-License-Identifier: LicenseRef-Snap-CD-Source-Available-1.1
// Copyright (c) 2026 Karl Schriek / Schrieksoft.
// No license is granted to use this file, in whole or in part, (a) as training, fine-tuning, retrieval, or
// embedding data for any machine-learning model, or (b) as input to any machine-learning model, agent, or automated
// system for the purpose of producing a derivative work or reimplementation that is not otherwise permitted by the
// Snap CD Source-Available License (including any Competing Product as defined therein). Contact info@snapcd.io
// for terms covering either use.
using SnapCd.Contracts;
using SnapCd.Server.Core.Misc.Utils;
using SnapCd.Server.Core.Services.ResolvedConfiguration.HelperClasses;
using Xunit;

namespace SnapCd.Server.Core.Tests.Tests.ManualJobs;

/// <summary>
/// A job may run at an explicit ref. The ref must be safe to hand to git, and the override must
/// touch only the declared module's revision.
/// </summary>
public class SourceRevisionOverrideTests
{
    [Theory]
    [InlineData("main")]
    [InlineData("feature/split-monolith")]
    [InlineData("v1.12.15")]
    [InlineData("0f8793d")]
    [InlineData("refs/pull/63/head")]
    public void Accepts_Ordinary_Refs(string value) => Assert.True(SourceRevisionOverride.IsValidRef(value));

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("-b")]
    [InlineData("--upload-pack=touch /tmp/x")]
    [InlineData("feature branch")]
    [InlineData("a..b")]
    [InlineData("main@{1}")]
    [InlineData("HEAD~1")]
    [InlineData("HEAD^")]
    [InlineData("refs/heads/x.lock")]
    [InlineData("/main")]
    [InlineData("main/")]
    [InlineData("a:b")]
    [InlineData("tab\there")]
    public void Refuses_Refs_Git_Would_Misread(string? value) => Assert.False(SourceRevisionOverride.IsValidRef(value));

    [Fact]
    public void Apply_Replaces_The_Revision_And_Resets_The_Type()
    {
        var declared = Declared("main", SourceRevisionType.SemanticVersionRange);

        SourceRevisionOverride.Apply(declared, "feature/split-monolith");

        Assert.Equal("feature/split-monolith", declared.SourceRevision);
        Assert.Equal(SourceRevisionType.Default, declared.SourceRevisionType);
        Assert.Equal("https://example.com/repo.git", declared.SourceUrl);
        Assert.Equal("modules/vpc", declared.SourceSubdirectory);
    }

    [Fact]
    public void Apply_Refuses_A_Bad_Ref()
    {
        var declared = Declared("main", SourceRevisionType.Default);

        Assert.Throws<ArgumentException>(() => SourceRevisionOverride.Apply(declared, "-b"));
        Assert.Equal("main", declared.SourceRevision);
    }

    private static ResolvedModule Declared(string revision, SourceRevisionType type) => new()
    {
        ModuleId = Guid.NewGuid(),
        NamespaceId = Guid.NewGuid(),
        StackId = Guid.NewGuid(),
        OrganizationId = Guid.NewGuid(),
        RunnerId = Guid.NewGuid(),
        ModuleName = "vpc",
        NamespaceName = "ns",
        StackName = "stack",
        RunnerName = "runner",
        SourceRevision = revision,
        SourceRevisionType = type,
        SourceUrl = "https://example.com/repo.git",
        SourceSubdirectory = "modules/vpc",
        Engine = "tofu",
        Policies = []
    };
}
