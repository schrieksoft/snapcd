// SPDX-License-Identifier: LicenseRef-Snap-CD-Source-Available-1.1
// Copyright (c) 2026 Karl Schriek / Schrieksoft.
// No license is granted to use this file, in whole or in part, (a) as training, fine-tuning, retrieval, or
// embedding data for any machine-learning model, or (b) as input to any machine-learning model, agent, or automated
// system for the purpose of producing a derivative work or reimplementation that is not otherwise permitted by the
// Snap CD Source-Available License (including any Competing Product as defined therein). Contact info@snapcd.io
// for terms covering either use.

using MassTransit;
using SnapCd.Server.Core.Events.System;

namespace SnapCd.Server.Core.Startup;

/// <summary>
/// Publishes a few messages nothing acts on, once the bus is up.
///
/// On the SQL Server transport the first message through a newly created send transport is usually
/// never delivered: it is written, no consumer ever receives it, and nothing faults. Measured at
/// about eight times in ten. Spending that loss on a message that means nothing leaves the first
/// real one to arrive.
/// </summary>
public class TransportWarmupHostedService(
    IBusControl bus,
    ILogger<TransportWarmupHostedService> logger) : IHostedService
{
    /// <summary>Two more than the one send at risk, which is cheap and covers a slower start.</summary>
    private const int Sends = 3;

    private static readonly TimeSpan Interval = TimeSpan.FromMilliseconds(500);

    /// <summary>Nothing correlates to these, so an undelivered one must expire on its own.</summary>
    private static readonly TimeSpan TimeToLive = TimeSpan.FromSeconds(30);

    /// <summary>Configuring every endpoint against a fresh database takes a couple of minutes.</summary>
    private static readonly TimeSpan StartTimeout = TimeSpan.FromMinutes(3);

    public Task StartAsync(CancellationToken cancellationToken)
    {
        // Off the startup path: the host waits for every hosted service to return, and waiting for
        // the bus here would hold the server down for as long as the bus takes to come up.
        _ = Task.Run(() => Warm(cancellationToken), cancellationToken);
        return Task.CompletedTask;
    }

    private async Task Warm(CancellationToken cancellationToken)
    {
        try
        {
            // Hosted services start in registration order, so this one cannot assume the bus is
            // already listening; publishing before it is drops the message with no warning.
            var health = await bus.WaitForHealthStatus(BusHealthStatus.Healthy, StartTimeout);

            if (health != BusHealthStatus.Healthy)
            {
                logger.LogWarning("Bus was {Health} after {Timeout}; skipping the warm-up", health, StartTimeout);
                return;
            }

            for (var i = 1; i <= Sends; i++)
            {
                await bus.Publish(
                    new WarmupRequested { Sequence = i },
                    context => context.TimeToLive = TimeToLive,
                    cancellationToken);

                if (i < Sends) await Task.Delay(Interval, cancellationToken);
            }

            logger.LogDebug("Warmed the transport with {Count} messages", Sends);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            // The server is no worse off than without the warm-up, so starting still wins.
            logger.LogWarning(ex, "Could not warm the transport; the first job started may not run");
        }
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}
