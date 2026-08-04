// SPDX-License-Identifier: LicenseRef-Snap-CD-Source-Available-1.1
// Copyright (c) 2026 Karl Schriek / Schrieksoft.
// No license is granted to use this file, in whole or in part, (a) as training, fine-tuning, retrieval, or
// embedding data for any machine-learning model, or (b) as input to any machine-learning model, agent, or automated
// system for the purpose of producing a derivative work or reimplementation that is not otherwise permitted by the
// Snap CD Source-Available License (including any Competing Product as defined therein). Contact info@snapcd.io
// for terms covering either use.

using Moq;
using SnapCd.Server.Core.Licensing.Services;
using SnapCd.Server.Core.Services;
using SnapCd.Server.Core.Settings;

namespace SnapCd.Server.Core.Tests.Tests.Services;

/// <summary>
/// The three allowance cases must stay distinct. They replaced a nullable int where null meant
/// "unlimited", which made an unentitled organization — returning null — silently unmetered.
/// </summary>
public class QuotaAllowanceTests
{
    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(int.MaxValue)]
    public void Unlimited_IsNeverExceeded(int currentCount)
    {
        Assert.False(QuotaAllowance.Unlimited.IsExceededAt(currentCount));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(int.MaxValue)]
    public void Denied_IsAlwaysExceeded(int currentCount)
    {
        Assert.True(QuotaAllowance.Denied.IsExceededAt(currentCount));
    }

    [Theory]
    [InlineData(0, false)]
    [InlineData(9, false)]
    [InlineData(10, true)]
    [InlineData(11, true)]
    public void Limited_IsExceededAtOrAboveTheLimit(int currentCount, bool expected)
    {
        Assert.Equal(expected, QuotaAllowance.Limited(10).IsExceededAt(currentCount));
    }

    [Fact]
    public void LimitOrNull_ExposesTheNumberOnlyForLimited()
    {
        Assert.Equal(10, QuotaAllowance.Limited(10).LimitOrNull);
        Assert.Null(QuotaAllowance.Unlimited.LimitOrNull);
        Assert.Null(QuotaAllowance.Denied.LimitOrNull);
    }

    /// <summary>
    /// Denied must not be mistaken for unlimited by the shared exceeded-check helper.
    /// </summary>
    [Fact]
    public async Task QuotaService_DeniedAllowance_ReportsQuotaExceeded()
    {
        var gating = new Mock<IQuotaGatingService>();
        gating.Setup(g => g.GetAllowanceAsync(It.IsAny<Guid>(), It.IsAny<string>()))
            .ReturnsAsync(QuotaAllowance.Denied);

        var service = new QuotaService(gating.Object);

        Assert.True(await service.IsQuotaExceededAsync(Guid.NewGuid(), nameof(QuotaLimits.ModuleQuota), 0));
    }

    [Fact]
    public async Task QuotaService_UnlimitedAllowance_NeverReportsQuotaExceeded()
    {
        var gating = new Mock<IQuotaGatingService>();
        gating.Setup(g => g.GetAllowanceAsync(It.IsAny<Guid>(), It.IsAny<string>()))
            .ReturnsAsync(QuotaAllowance.Unlimited);

        var service = new QuotaService(gating.Object);

        Assert.False(await service.IsQuotaExceededAsync(Guid.NewGuid(), nameof(QuotaLimits.ModuleQuota), int.MaxValue));
    }
}
