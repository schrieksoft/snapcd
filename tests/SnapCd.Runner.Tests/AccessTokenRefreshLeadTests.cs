// SPDX-License-Identifier: LicenseRef-Snap-CD-Source-Available-1.1
// Copyright (c) 2026 Karl Schriek / Schrieksoft.
// No license is granted to use this file, in whole or in part, (a) as training, fine-tuning, retrieval, or
// embedding data for any machine-learning model, or (b) as input to any machine-learning model, agent, or automated
// system for the purpose of producing a derivative work or reimplementation that is not otherwise permitted by the
// Snap CD Source-Available License (including any Competing Product as defined therein). Contact info@snapcd.io
// for terms covering either use.

using SnapCd.Runner.Services;

namespace SnapCd.Runner.Tests;

/// <summary>
/// The server sets the token lifetime and reports it as expires_in, so the runner's refresh lead
/// has to hold for any lifetime it is told.
/// </summary>
public class AccessTokenRefreshLeadTests
{
    [Theory]
    [InlineData(1)]
    [InlineData(2)]
    [InlineData(5)]
    [InlineData(10)]
    [InlineData(60)]
    [InlineData(480)]
    public void The_lead_leaves_the_next_refresh_in_the_future(int lifetimeMinutes)
    {
        var lifetime = TimeSpan.FromMinutes(lifetimeMinutes);

        var lead = AccessTokenCacheQuartzJob.RefreshLeadTime(lifetime);

        Assert.True(lead > TimeSpan.Zero, "a non-positive lead would refresh at or after expiry");
        Assert.True(lead < lifetime, "a lead of the whole lifetime would schedule every refresh in the past");
    }

    [Fact]
    public void An_hour_long_token_keeps_the_five_minute_lead()
    {
        Assert.Equal(TimeSpan.FromMinutes(5), AccessTokenCacheQuartzJob.RefreshLeadTime(TimeSpan.FromHours(1)));
    }

    [Fact]
    public void A_long_lived_token_is_not_fetched_needlessly_early()
    {
        Assert.Equal(TimeSpan.FromMinutes(5), AccessTokenCacheQuartzJob.RefreshLeadTime(TimeSpan.FromHours(8)));
    }

    [Fact]
    public void A_short_lived_token_scales_its_lead_down()
    {
        Assert.Equal(TimeSpan.FromSeconds(24), AccessTokenCacheQuartzJob.RefreshLeadTime(TimeSpan.FromMinutes(2)));
    }

    [Theory]
    [InlineData(1)]
    [InlineData(2)]
    [InlineData(5)]
    [InlineData(10)]
    [InlineData(60)]
    public void The_first_refresh_lands_before_the_initial_token_expires(int lifetimeMinutes)
    {
        // The server closes a hub connection when the token it was opened with expires, so a first
        // refresh placed after that point leaves the connection to be dropped instead of replaced.
        var lifetime = TimeSpan.FromMinutes(lifetimeMinutes);
        var expiry = DateTime.UtcNow + lifetime;

        var triggerTime = expiry - AccessTokenCacheQuartzJob.RefreshLeadTime(lifetime);

        Assert.True(triggerTime < expiry, "the refresh has to precede expiry");
        Assert.True(triggerTime > DateTime.UtcNow, "a trigger in the past fires immediately and spins");
    }
}
