// SPDX-License-Identifier: LicenseRef-Snap-CD-Source-Available-1.1
// Copyright (c) 2026 Karl Schriek / Schrieksoft.
// No license is granted to use this file, in whole or in part, (a) as training, fine-tuning, retrieval, or
// embedding data for any machine-learning model, or (b) as input to any machine-learning model, agent, or automated
// system for the purpose of producing a derivative work or reimplementation that is not otherwise permitted by the
// Snap CD Source-Available License (including any Competing Product as defined therein). Contact info@snapcd.io
// for terms covering either use.

using SnapCd.Server.Host.Telemetry;
using Xunit;

namespace SnapCd.Server.Core.Tests.Tests.Services;

public class VersionDeltaTests
{
    [Theory]
    [InlineData("1.12.11", "1.12.12", "patch")]
    [InlineData("1.12.11", "1.13.0", "minor")]
    [InlineData("1.12.11", "2.0.0", "major")]
    [InlineData("1.12.11+sha.abc", "1.12.12", "patch")]
    [InlineData("2.0.0-rc.1", "2.0.0", "patch")]
    public void Names_TheLargestChangedComponent(string running, string latest, string expected)
    {
        Assert.Equal(expected, VersionDelta.Describe(running, latest));
    }

    [Theory]
    [InlineData("1.12.11", "1.12.11")]
    [InlineData("1.13.0", "1.12.11")]
    [InlineData("1.12.11", null)]
    [InlineData("1.12.11", "")]
    [InlineData("not-a-version", "1.0.0")]
    public void Null_WhenNotBehind_OrUnknown(string running, string? latest)
    {
        Assert.Null(VersionDelta.Describe(running, latest));
    }
}
