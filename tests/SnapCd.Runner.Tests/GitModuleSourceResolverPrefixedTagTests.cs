// SPDX-License-Identifier: LicenseRef-Snap-CD-Source-Available-1.1
// Copyright (c) 2026 Karl Schriek / Schrieksoft.
// No license is granted to use this file, in whole or in part, (a) as training, fine-tuning, retrieval, or
// embedding data for any machine-learning model, or (b) as input to any machine-learning model, agent, or automated
// system for the purpose of producing a derivative work or reimplementation that is not otherwise permitted by the
// Snap CD Source-Available License (including any Competing Product as defined therein). Contact info@snapcd.io
// for terms covering either use.

using Microsoft.Extensions.Logging.Abstractions;
using SnapCd.Runner.Services.ModuleSourceRefresher;

namespace SnapCd.Runner.Tests;

/// <summary>
/// Range resolution against the monorepo-testing fixture repo, whose tag corpus deliberately mixes unprefixed,
/// prefixed, suffixed, digit-ending-prefix and pre-release tags in one repository. The pinned expectations come
/// from the fixture's frozen refs.
/// </summary>
public class GitModuleSourceResolverPrefixedTagTests
{
    private readonly GitModuleSourceResolver _resolver;
    private readonly string _sourceUrl = "https://github.com/schrieksoft/monorepo-testing.git";

    public GitModuleSourceResolverPrefixedTagTests()
    {
        _resolver = new GitModuleSourceResolver(NullLogger<GitModuleSourceResolver>.Instance);
    }

    [Theory]
    [InlineData("1.0.*", "v1.0.1")]
    [InlineData("1.*", "v1.1.0")]
    [InlineData("v1.*", "v1.1.0")]
    [InlineData("2.*", "2.0.0")]
    [InlineData("ui-v1.*", "ui-v1.2.3")]
    [InlineData("ui-v1.2.*", "ui-v1.2.3")]
    [InlineData("ui-1.*", "ui-v1.2.3")]
    [InlineData("backend/v2.*", "backend/v2.1.0")]
    [InlineData("1.*-ui", "1.2.0-ui")]
    [InlineData("release2-1.*", "release2-1.1.0")]
    [InlineData("ui-v1.*-rc.1", "ui-v1.3.0-rc.1")]
    [InlineData("*", "2.0.0")]
    [InlineData("v*", "2.0.0")]
    [InlineData("ui-*", "ui-v1.2.3")]
    [InlineData("ui-v*", "ui-v1.2.3")]
    [InlineData("ui-v*-rc.1", "ui-v1.3.0-rc.1")]
    [InlineData("ui-*-rc.1", "ui-v1.3.0-rc.1")]
    [InlineData("*-ui", "1.2.0-ui")]
    [InlineData("backend/v*", "backend/v2.1.0")]
    [InlineData("release2-*", "release2-1.1.0")]
    [InlineData("ui-v1.2.3", "ui-v1.2.3")]
    [InlineData("backend/v2.0.0", "backend/v2.0.0")]
    public void Resolves_Correct_Tag(string semverRange, string expectedTag)
    {
        Assert.Equal(expectedTag, _resolver.GetRemoteSemverRangeResolvedTag(_sourceUrl, semverRange));
    }

    [Fact]
    public void Resolves_Lightweight_Tag_To_Pinned_Commit()
    {
        var sha = _resolver.GetRemoteSemverRangeDefinitiveRevision(_sourceUrl, "2.*");

        Assert.Equal("6051e6b0d42fbdb3141ef02990c6aa343d0e790c", sha);
    }

    [Theory]
    [InlineData("v2")]
    [InlineData("v1.*.*")]
    [InlineData("release-1")]
    [InlineData("invalid")]
    public void Throws_For_Invalid_Formats(string invalidRange)
    {
        Assert.Throws<ArgumentException>(() =>
            _resolver.GetRemoteSemverRangeDefinitiveRevision(_sourceUrl, invalidRange));
    }

    [Fact]
    public void Nonexistent_Literal_Tag_Fails_At_Resolution_Not_Parsing()
    {
        var ex = Assert.Throws<Exception>(() =>
            _resolver.GetRemoteSemverRangeDefinitiveRevision(_sourceUrl, "ui-v9.9.9"));

        Assert.Contains("Unable to determine latest remote sha", ex.Message);
    }

    [Theory]
    [InlineData("3.0.*")]
    [InlineData("ui-v3.*")]
    [InlineData("frontend-v1.*")]
    [InlineData("frontend-*")]
    [InlineData("*.1.2")]
    public void Throws_When_No_Matching_Tags(string notFoundRange)
    {
        var ex = Assert.Throws<Exception>(() =>
            _resolver.GetRemoteSemverRangeDefinitiveRevision(_sourceUrl, notFoundRange));

        Assert.Contains("No tags in remote repository", ex.Message);
    }
}
