// SPDX-License-Identifier: LicenseRef-Snap-CD-Source-Available-1.1
// Copyright (c) 2026 Karl Schriek / Schrieksoft.
// No license is granted to use this file, in whole or in part, (a) as training, fine-tuning, retrieval, or
// embedding data for any machine-learning model, or (b) as input to any machine-learning model, agent, or automated
// system for the purpose of producing a derivative work or reimplementation that is not otherwise permitted by the
// Snap CD Source-Available License (including any Competing Product as defined therein). Contact info@snapcd.io
// for terms covering either use.

using System.Net.WebSockets;
using Microsoft.AspNetCore.SignalR;
using SnapCd.Contracts.Constants;

namespace SnapCd.Contracts.Clients;

/// <summary>
/// The retry every caller of a hub method uses, so the runner and the job harness agree on which
/// rejections are transient and how long they wait. A harness that retried on different terms than
/// the runner would pass or stall for reasons the real runner never sees.
/// </summary>
public static class HubInvocationRetry
{
    public const int DefaultMaxRetries = 3;
    public static readonly TimeSpan DefaultInitialDelay = TimeSpan.FromSeconds(1);
    public static readonly TimeSpan MaxDelay = TimeSpan.FromSeconds(10);

    /// <summary>
    /// Whether a failed invocation is worth repeating. A rejection the server chose to send is
    /// retryable only if it carries a code saying so; everything else is a decision that will not
    /// change on a second attempt. The transport failures are exception types rather than codes,
    /// because the call never reached the server to be given one.
    /// </summary>
    public static bool IsRetryable(Exception ex) =>
        HubErrorCodes.IsRetryable(ex.Message) ||
        ex is TaskCanceledException or TimeoutException or OperationCanceledException ||
        ex is HttpRequestException or WebSocketException ||
        ex is HubException { Message: "" };

    /// <summary>Whether the rejection is specifically an expired authentication token.</summary>
    public static bool IsTokenExpired(Exception ex) =>
        HubErrorCodes.Is(HubErrorCodes.TokenExpired, ex.Message);

    /// <summary>
    /// Runs <paramref name="invocation"/>, repeating it on a retryable failure with an
    /// exponential backoff capped at <see cref="MaxDelay"/>. <paramref name="onAttemptFailed"/>
    /// is given the exception and the delay about to be waited, and returns the delay to actually
    /// wait, so a caller can handle a case such as token expiry on its own terms.
    /// </summary>
    public static async Task InvokeAsync(
        Func<Task> invocation,
        int maxRetries = DefaultMaxRetries,
        TimeSpan? initialDelay = null,
        Func<Exception, int, TimeSpan, Task<TimeSpan>>? onAttemptFailed = null,
        Action<int>? onSucceededAfterRetry = null)
    {
        var attempt = 0;
        var delay = initialDelay ?? DefaultInitialDelay;
        Exception? lastException = null;

        while (attempt < maxRetries)
        {
            try
            {
                await invocation();

                if (attempt > 0) onSucceededAfterRetry?.Invoke(attempt + 1);

                return;
            }
            catch (Exception ex) when (IsRetryable(ex))
            {
                lastException = ex;
                attempt++;

                if (attempt >= maxRetries) throw;

                var wait = delay;
                if (onAttemptFailed is not null) wait = await onAttemptFailed(ex, attempt, delay);

                if (wait > TimeSpan.Zero) await Task.Delay(wait);

                delay = TimeSpan.FromSeconds(Math.Min(delay.TotalSeconds * 2, MaxDelay.TotalSeconds));
            }
        }

        throw lastException ?? new Exception($"Hub invocation failed after {maxRetries} attempts");
    }
}
