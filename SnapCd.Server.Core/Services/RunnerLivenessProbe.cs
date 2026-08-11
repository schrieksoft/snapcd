// SPDX-License-Identifier: LicenseRef-Snap-CD-Source-Available-1.1
// Copyright (c) 2026 Karl Schriek / Schrieksoft.
// No license is granted to use this file, in whole or in part, (a) as training, fine-tuning, retrieval, or
// embedding data for any machine-learning model, or (b) as input to any machine-learning model, agent, or automated
// system for the purpose of producing a derivative work or reimplementation that is not otherwise permitted by the
// Snap CD Source-Available License (including any Competing Product as defined therein). Contact info@snapcd.io
// for terms covering either use.

using System.Collections.Concurrent;
using Microsoft.AspNetCore.SignalR;
using SnapCd.Contracts.Constants;
using SnapCd.Server.Core.Hubs;

namespace SnapCd.Server.Core.Services;

/// <summary>
/// Round-trips a ping over an established runner connection. A SignalR connection can remain open
/// while the runner behind it is unable to service calls, so a registered connection is not on its
/// own evidence that work dispatched to it will run.
/// </summary>
public class RunnerLivenessProbe
{
    private readonly IHubContext<RunnerHub> _hubContext;
    private readonly ILogger<RunnerLivenessProbe> _logger;

    private static readonly ConcurrentDictionary<Guid, TaskCompletionSource<bool>> Pending = new();

    private static readonly TimeSpan DefaultTimeout = TimeSpan.FromSeconds(5);

    public RunnerLivenessProbe(IHubContext<RunnerHub> hubContext, ILogger<RunnerLivenessProbe> logger)
    {
        _hubContext = hubContext;
        _logger = logger;
    }

    /// <summary>
    /// Returns true if the runner answered within the timeout.
    /// </summary>
    public async Task<bool> IsAlive(string signalRConnectionId, TimeSpan? timeout = null)
    {
        var pingId = Guid.NewGuid();
        var tcs = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
        Pending[pingId] = tcs;

        try
        {
            await _hubContext.Clients.Client(signalRConnectionId).SendAsync(RunnerEndpoints.Ping, pingId);

            var completed = await Task.WhenAny(tcs.Task, Task.Delay(timeout ?? DefaultTimeout));
            if (completed == tcs.Task)
                return true;

            _logger.LogWarning(
                "Runner on connection {ConnectionId} did not answer a liveness ping within {Timeout}",
                signalRConnectionId, timeout ?? DefaultTimeout);
            return false;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex,
                "Liveness ping to connection {ConnectionId} could not be sent", signalRConnectionId);
            return false;
        }
        finally
        {
            Pending.TryRemove(pingId, out _);
        }
    }

    /// <summary>Called from the hub when a runner answers a ping.</summary>
    public static void Acknowledge(Guid pingId)
    {
        if (Pending.TryRemove(pingId, out var tcs))
            tcs.TrySetResult(true);
    }
}
