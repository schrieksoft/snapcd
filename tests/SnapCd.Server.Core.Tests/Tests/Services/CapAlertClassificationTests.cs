// SPDX-License-Identifier: LicenseRef-Snap-CD-Source-Available-1.1
// Copyright (c) 2026 Karl Schriek / Schrieksoft.
// No license is granted to use this file, in whole or in part, (a) as training, fine-tuning, retrieval, or
// embedding data for any machine-learning model, or (b) as input to any machine-learning model, agent, or automated
// system for the purpose of producing a derivative work or reimplementation that is not otherwise permitted by the
// Snap CD Source-Available License (including any Competing Product as defined therein). Contact info@snapcd.io
// for terms covering either use.

using SnapCd.Server.Core.Telemetry;
using SnapCd.Server.Host.CapAlerts;
using Xunit;

namespace SnapCd.Server.Core.Tests.Tests.Services;

public class CapAlertClassificationTests
{
    [Theory]
    [InlineData(8, 10, null)]
    [InlineData(9, 10, nameof(WhatsNewKind.CapNear))]
    [InlineData(10, 10, nameof(WhatsNewKind.CapAt))]
    [InlineData(11, 10, nameof(WhatsNewKind.CapOver))]
    [InlineData(13, 15, null)]
    [InlineData(14, 15, nameof(WhatsNewKind.CapNear))]
    [InlineData(18, 20, nameof(WhatsNewKind.CapNear))]
    [InlineData(89, 100, null)]
    [InlineData(90, 100, nameof(WhatsNewKind.CapNear))]
    [InlineData(100, 100, nameof(WhatsNewKind.CapAt))]
    [InlineData(0, 10, null)]
    public void NearIsNinetyPercent_AtIsEqual_OverIsMore(int count, int cap, string? expected)
    {
        Assert.Equal(expected, CapAlertService.Classify(count, cap)?.ToString());
    }

    [Fact]
    public void Defaults_CoverAllThreeKinds_AndCarryTheCountPlaceholder()
    {
        foreach (var kind in new[] { WhatsNewKind.CapNear, WhatsNewKind.CapAt, WhatsNewKind.CapOver })
        {
            var text = CapAlertService.Defaults[kind];
            Assert.Equal(kind.ToString(), text.Kind);
            Assert.Contains("{cap}", text.Markdown);
        }
    }
}
