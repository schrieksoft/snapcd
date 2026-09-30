// SPDX-License-Identifier: LicenseRef-Snap-CD-Source-Available-1.1
// Copyright (c) 2026 Karl Schriek / Schrieksoft.
// No license is granted to use this file, in whole or in part, (a) as training, fine-tuning, retrieval, or
// embedding data for any machine-learning model, or (b) as input to any machine-learning model, agent, or automated
// system for the purpose of producing a derivative work or reimplementation that is not otherwise permitted by the
// Snap CD Source-Available License (including any Competing Product as defined therein). Contact info@snapcd.io
// for terms covering either use.

using Microsoft.AspNetCore.SignalR;
using SnapCd.Contracts.Clients;
using SnapCd.Contracts.Constants;

namespace SnapCd.Runner.Tests;

public class HubInvocationRetryTests
{
    [Fact]
    public void An_expired_token_is_retryable()
    {
        var ex = new HubException($"{HubErrorCodes.TokenExpired}: The authentication token has expired.");

        Assert.True(HubInvocationRetry.IsRetryable(ex));
        Assert.True(HubInvocationRetry.IsTokenExpired(ex));
    }

    [Fact]
    public void A_rejection_without_a_code_is_not_retryable()
    {
        Assert.False(HubInvocationRetry.IsRetryable(new HubException("Unauthorized: This job requires a specific runner")));
    }

    /// <summary>
    /// Both carry the word "connection", which a substring match once read as transient, so both
    /// were retried three times before failing anyway.
    /// </summary>
    [Theory]
    [InlineData("Unauthorized: Runner connection not found")]
    [InlineData("Connection validation failed")]
    public void A_permanent_rejection_naming_the_connection_is_not_retryable(string message)
    {
        Assert.False(HubInvocationRetry.IsRetryable(new HubException(message)));
    }

    /// <summary>A call that never reached the server carries no code, so it is matched by type.</summary>
    [Fact]
    public void A_transport_failure_is_retryable()
    {
        Assert.True(HubInvocationRetry.IsRetryable(new TimeoutException()));
        Assert.True(HubInvocationRetry.IsRetryable(new TaskCanceledException()));
        Assert.True(HubInvocationRetry.IsRetryable(new HttpRequestException()));
    }

    [Fact]
    public async Task It_stops_once_the_invocation_succeeds()
    {
        var attempts = 0;

        await HubInvocationRetry.InvokeAsync(
            () =>
            {
                attempts++;
                if (attempts < 2) throw new HubException($"{HubErrorCodes.TokenExpired}: expired");
                return Task.CompletedTask;
            },
            initialDelay: TimeSpan.Zero,
            onAttemptFailed: (_, _, _) => Task.FromResult(TimeSpan.Zero));

        Assert.Equal(2, attempts);
    }

    [Fact]
    public async Task It_gives_up_after_the_last_attempt()
    {
        var attempts = 0;

        await Assert.ThrowsAsync<HubException>(() => HubInvocationRetry.InvokeAsync(
            () =>
            {
                attempts++;
                throw new HubException($"{HubErrorCodes.TokenExpired}: expired");
            },
            maxRetries: 3,
            initialDelay: TimeSpan.Zero,
            onAttemptFailed: (_, _, _) => Task.FromResult(TimeSpan.Zero)));

        Assert.Equal(3, attempts);
    }

    [Fact]
    public async Task It_does_not_repeat_a_rejection_that_will_not_change()
    {
        var attempts = 0;

        await Assert.ThrowsAsync<HubException>(() => HubInvocationRetry.InvokeAsync(
            () =>
            {
                attempts++;
                throw new HubException("Unauthorized: Runner connection not found");
            },
            initialDelay: TimeSpan.Zero));

        Assert.Equal(1, attempts);
    }
}
